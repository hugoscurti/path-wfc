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

    // Internal variables
    Thread _thread;

    State _runstate;
    public State RunState { get { return _runstate; } }

    bool? workDone;
    bool firstPropagateDone;

    // Inspector variables

    public PathOverlapAttributes ModelAttributes;

    [Range(0.1f, 4f)]
    public float secondsBetweenUpdates = 0.5f;

    IEnumerator progress;
    float lastUpdate = 0f;

    private PathOverlapModel model;

    int N = 3;

    public PathOverlapModel GetModel()
    {
        return model;
    }

    public void Cancel()
    {
        _runstate = State.Stopped;
        status = "Stopped";
        if (_thread != null) _thread.Abort();
        model.Init((int)Time.realtimeSinceStartup);
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
        GetComponent<MapController>().ResetOutput();
        model.Init((int)Time.realtimeSinceStartup);
        model.Print();
    }

    // Call this when the maps are loaded
    public void InstantiateModel()
    {
        // Prepare variables for thread
        MapController mapLoader = GetComponent<MapController>();

        model = new PathOverlapModel(mapLoader.inputTarget, mapLoader.outputTarget, N, ModelAttributes);
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

    private void OnDrawGizmos()
    {
        var output = GetComponent<MapController>().outputTarget;
        
        // Show status of algorithm
        Handles.Label(output.transform.position, $"Status : {status}");
    }

    private void ThreadExecute()
    {
        _runstate = State.Running;
        status = "Running";

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

            Thread.Sleep(10);
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
