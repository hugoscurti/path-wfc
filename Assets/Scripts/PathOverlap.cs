using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathOverlap : MonoBehaviour {

    string status = "Stopped";
    public enum State
    {
        Stopped,
        Running,
        Paused
    }

    float timeRun = -1;

    // Internal variables
    Thread _thread;

    State _runstate;
    public State RunState { get { return _runstate; } }

    float lastUpdate = 0f;
    bool? workDone;
    bool firstPropagateDone;

    IEnumerator progress;
    private PathOverlapModel model;
    int N = 3;

    // Inspector variables

    public PathOverlapAttributes ModelAttributes;

    public ExecutionAttributes execution;


    public PathOverlapModel GetModel()
    {
        return model;
    }

    public void Cancel()
    {
        _runstate = State.Stopped;
        status = "Stopped";
        if (_thread != null) _thread.Abort();

        InitModel(false);
    }

    public void Pause()
    {
        _runstate = State.Paused;
    }

    public void ResetOutput()
    {
        _runstate = State.Stopped;
        status = "Stopped";
        if (_thread != null) _thread.Abort();

        InitModel(true);
    }

    // Call this when the maps are loaded
    public void InstantiateModel()
    {
        // Prepare variables for thread
        MapController mapLoader = GetComponent<MapController>();
        mapLoader.ResetOutput();

        model = new PathOverlapModel(mapLoader.inputTarget, mapLoader.outputTarget, N, ModelAttributes);

        InitModel(true);
    }

    private void firstPropagate()
    {
        model.PropagateFixedWaves(true);
        model.PropagateMasks(true);
        firstPropagateDone = true;
    }

    public void FirstPropagate()
    {
        firstPropagate();
        model.Print();
    }

    private void InitModel(bool print)
    {
        model.Init(execution.UseFixedSeed ? execution.Seed : (int)Time.realtimeSinceStartup);

        if (print)
            model.Print();
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

        if (execution.ShowProgress)
        {
            // Show progress in a separate thread. 
            // Use the EditorApplication.update event to poll for algorithm updates
            _thread = new Thread(Execute);
            _thread.Start();

            progress = ShowProgress();

            EditorApplication.update += EditorUpdate;
        } else
        {
            Execute();
            model.Print();
        }
    }

    void EditorUpdate()
    {
        if (Time.realtimeSinceStartup - lastUpdate > execution.SecondsBetweenUpdate)
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

    private void OnDrawGizmos()
    {
        var output = GetComponent<MapController>().outputTarget;
        var text = $"Status : {status};\nTime : {timeRun} sec.";
        

        // Show status of algorithm
        Handles.Label(output.transform.position, text);
    }

    private void Execute()
    {
        _runstate = State.Running;
        status = "Running";

        // Begin timing
        long begin = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();

        if (!firstPropagateDone)
        {
            model.PropagateFixedWaves(true);
            model.PropagateMasks(true);
            firstPropagateDone = true;
        }

        workDone = null;
        while (_runstate == State.Running && workDone == null)
        {
            model.Propagate();
            workDone = model.Observe();
        }

        if (!workDone.HasValue)
        {
            status = _runstate == State.Stopped ? "Cancelled" : "Paused";
        } else
        {
            // Algorithm has finished, we put in stopped state
            _runstate = State.Stopped;

            if (workDone.Value)
                status = "Sucessful";
            else
                status = "Failed";
        }

        // End timing
        long end = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
        timeRun = (end - begin) / 1000f;
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
