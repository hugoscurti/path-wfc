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
    public bool useOutputMaskPatterns;

    // Internal variables
    Thread _thread;

    State _runstate;
    public State RunState { get { return _runstate; } }

    bool? workDone;
    bool firstPropagateDone;

    IEnumerator progress;
    // TODO: Incorporate this in the update loop
    float secondsBetweenUpdates = 0.5f;
    float lastUpdate = 0f;

    // TODO: If we want to fix tiles, we should instantiate Model when we display the map instead of instantiating it before pressing play
    private PathOverlapModel model;

    int N = 3;

    public PathOverlapModel GetModel()
    {
        return model;
    }

    public void Cancel()
    {
        _runstate = State.Stopped;
        _thread.Abort();
        model.Init((int)Time.realtimeSinceStartup);
    }

    public void Pause()
    {
        _runstate = State.Paused;
    }

    public void ResetOutput()
    {
        _runstate = State.Stopped;
        _thread.Abort();
        GetComponent<MapLoader>().ResetOutput();
        model.Init((int)Time.realtimeSinceStartup);
    }

    // Call this when the maps are loaded
    public void InstantiateModel()
    {
        // Prepare variables for thread
        MapLoader mapLoader = GetComponent<MapLoader>();

        model = new PathOverlapModel(mapLoader.inputTarget, mapLoader.outputTarget, this.N, this.periodicInput, this.periodicOutput, this.generatePatternsFromOutput);
        model.Init((int)Time.realtimeSinceStartup);
        model.Print(); // Initial print
    }

    public void ExecuteAlgorithm(bool reset = true)
    {
        if (model == null)
        {
            Debug.LogError("You must load a map before executing the algortihm");
            return;
        }

        // This enables having pause and stop buttons 
        if (reset)
        {
            firstPropagateDone = false;
        }

        _thread = new Thread(ThreadExecute);
        _thread.Start();

        progress = ShowProgress();

        EditorApplication.update += EditorUpdate;
    }

    void EditorUpdate()
    {
        if (Time.realtimeSinceStartup - lastUpdate > secondsBetweenUpdates)
        {
            var hasNext = progress.MoveNext();
            lastUpdate = Time.realtimeSinceStartup;
            if (!hasNext)
                // Unregister event
                EditorApplication.update -= EditorUpdate;
        }

        
    }

    private IEnumerator ShowProgress()
    {
        do
        {
            model.Print();
            yield return null;

        } while (_runstate == State.Running);

        // Final print after algorithm is done
        model.Print();

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
