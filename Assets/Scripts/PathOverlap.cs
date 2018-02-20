using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathOverlap : MonoBehaviour {

    public enum State
    {
        Stopped,
        Running,
        Paused
    }

    // Public variables
    public bool periodicInput;
    public bool periodicOutput;

    public bool generatePatternsFromOutput;

    // Internal variables

    Thread _thread;

    State _runstate;
    public State RunState { get { return _runstate; } }

    bool? workDone;

    bool firstPropagateDone;

    Tile blankTile;

    IEnumerator progress;

    // TODO: If we want to fix tiles, we should instantiate Model when we display the map instead of instantiating it before pressing play
    private PathOverlapModel model;

    int N = 3;

    private void Awake()
    {
    }


    private void OnGUI()
    {
    }


    public void Cancel()
    {
        _runstate = State.Stopped;
    }

    public void Pause()
    {
        _runstate = State.Paused;
    }

    public void ResetOutput()
    {
        _runstate = State.Stopped;
        GetComponent<MapLoader>().ResetOutput();
    }

    public void ExecuteAlgorithm(bool reset = true)
    {
        if (reset)
        {
            // Prepare variables for thread
            MapLoader mapLoader = GetComponent<MapLoader>();
            blankTile = mapLoader.blankTile;

            model = new PathOverlapModel(mapLoader.inputTarget, mapLoader.outputTarget, this.N, this.periodicInput, this.periodicOutput, this.generatePatternsFromOutput);
            model.Init((int)Time.time);
            firstPropagateDone = false;
        }

        _thread = new Thread(ThreadExecute);
        _thread.Start();

        progress = ShowProgress();

        EditorApplication.update += EditorUpdate;
    }

    void EditorUpdate()
    {
        var hasNext = progress.MoveNext();

        if (!hasNext)
            // Unregister event
            EditorApplication.update -= EditorUpdate;
    }

    private IEnumerator ShowProgress()
    {
        do
        {
            model.Print(blankTile);

            yield return new WaitForSeconds(1);

        } while (_runstate == State.Running);

        // Final print after algorithm is done
        model.Print(blankTile);

        yield return null;
    }

    private void ThreadExecute()
    {
        _runstate = State.Running;

        if (!firstPropagateDone)
        {
            model.PropagateFixedWaves(true);
            firstPropagateDone = true;
        }

        workDone = null;
        while (_runstate == State.Running && workDone == null)
        {
            model.Propagate();

            Thread.Sleep(10);
            workDone = model.Observe();
        }

        if (!workDone.HasValue)
        {
            Debug.Log(_runstate == State.Stopped ? "Cancelled" : "Paused");
        } else
        {
            // Algorithm has finished, we put in stopped state
            _runstate = State.Stopped;

            if (workDone.Value)
                Debug.Log("Successful");
            else
                Debug.Log("Failed");
        }
    }

    private void OnDisable()
    {
        if (_runstate == State.Running)
        {
            _runstate = State.Stopped;
            _thread.Join();
        }
    }

}
