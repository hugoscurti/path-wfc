using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathOverlapController : MonoBehaviour {

    int seed;
    string status = "Stopped";

    float timeRun = -1;

    // Internal variables
    Thread _thread;
    public State RunState { get; private set; }

    float lastUpdate = 0f;
    bool? workDone;
    bool firstPropagateDone;
    bool stepByStep;

    private PathOverlapModel model;
    private readonly int N = 3;

    // Inspector variables
    public MapController mapController;

    public PathOverlapAttributes ModelAttributes;

    public ExecutionAttributes execution;


    public PathOverlapModel GetModel()
    {
        return model;
    }

    public void Cancel()
    {
        RunState = State.Stopped;
        status = "Stopped";
        if (_thread != null) _thread.Abort();

        InitModel(false);
    }

    public void Pause()
    {
        RunState = State.Paused;
    }

    public void ResetOutput()
    {
        RunState = State.Stopped;
        status = "Stopped";
        if (_thread != null) _thread.Abort();

        InitModel(true);
    }

    // Call this when the maps are loaded
    public void InstantiateModel()
    {
        // Prepare variables for thread
        mapController.ResetOutput();

        model = new PathOverlapModel(mapController.inputTarget, mapController.outputTarget, N, ModelAttributes);

        InitModel(true);
    }

    private void firstPropagate()
    {
        model.Propagate();
        firstPropagateDone = true;
    }

    public void FirstPropagate()
    {
        firstPropagate();
        model.Print();
    }

    private void InitModel(bool print)
    {
        seed = execution.UseFixedSeed ? execution.Seed : (int)Time.realtimeSinceStartup;
        model.Init(seed);

        if (print)
            model.Print();
    }

    public void ExecuteAlgorithm(bool reset = true, bool step = false)
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

        stepByStep = step;

        // Execute in a separate thread.
        _thread = new Thread(Execute);

        _thread.Start();

        // Use the EditorApplication.update event to poll for updates
        EditorApplication.update += EditorUpdate;
    }


    void EditorUpdate()
    {
        if (execution.ShowProgress)
        {
            if (Time.realtimeSinceStartup - lastUpdate > execution.SecondsBetweenUpdate)
            {
                model.Print();

                lastUpdate = Time.realtimeSinceStartup;

                if (RunState != State.Running)
                    // Unregister event
                    EditorApplication.update -= EditorUpdate;
            }
        } else
        {
            if (RunState != State.Running)
            {
                // Wait for thread to finish?
                _thread.Join();
                model.Print();

                // Unregister event
                EditorApplication.update -= EditorUpdate;
            }
        }
    }

    private void OnDrawGizmos()
    {
        var output = GetComponent<MapController>().outputTarget;
        var text = $"Seed: {seed};\nStatus : {status};\nTime : {timeRun} sec.";
        

        // Show status of algorithm
        Handles.Label(output.transform.position, text);
    }

    private void Execute()
    {
        RunState = State.Running;
        status = "Running";

        // Begin timing
        long begin = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();

        if (!firstPropagateDone)
            firstPropagate();

        workDone = null;
        while (RunState == State.Running && workDone == null)
        {
            model.Propagate();
            workDone = model.Observe();

            if (stepByStep)
                RunState = State.Paused;
        }

        if (!workDone.HasValue)
        {
            status = RunState == State.Stopped ? "Cancelled" : "Paused";
        } else
        {
            // Algorithm has finished, we put in stopped state
            RunState = State.Stopped;

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
        if (RunState == State.Running)
        {
            RunState = State.Stopped;
            _thread.Join();
        }
    }

}
