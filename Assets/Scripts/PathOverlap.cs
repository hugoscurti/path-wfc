using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathOverlap : MonoBehaviour {

    Thread _thread;
    bool _threadRunning;
    bool? workDone;

    public bool periodicInput;
    public bool periodicOutput;

    Tile blankTile;

    IEnumerator progress;

    private PathOverlapModel model;

    private void Awake()
    {
    }


    private void OnGUI()
    {
    }

    [Range(2, 6)]
    public int N = 3;

    public void Cancel()
    {
        _threadRunning = false;
    }


	public void ExecuteAlgorithm()
    {
        // Prepare variables for thread
        MapLoader mapLoader = GetComponent<MapLoader>();
        blankTile = mapLoader.blankTile;

        model = new PathOverlapModel(mapLoader.inputTarget, mapLoader.outputTarget, this.N, this.periodicInput, this.periodicOutput);
        model.Init((int)Time.time);

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
        int i = 0;

        do
        {
            model.Print(blankTile);

            yield return new WaitForSeconds(1);

        } while (_threadRunning);

        // Final print after algorithm is done
        model.Print(blankTile);

        yield return null;
    }

    private void ThreadExecute()
    {
        _threadRunning = true;
        
        model.PropagateFixedWaves(true);

        workDone = null;
        while (_threadRunning && workDone == null)
        {
            model.Propagate();

            Thread.Sleep(10);
            workDone = model.Observe();
        }

        _threadRunning = false;

        if (!workDone.HasValue)
            Debug.Log("Cancelled");
        else if (workDone.Value)
            Debug.Log("Successful");
        else
            Debug.Log("Failed");
    }

    private void OnDisable()
    {
        if (_threadRunning)
        {
            _threadRunning = false;
            _thread.Join();
        }
    }

}
