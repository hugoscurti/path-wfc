using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathOverlap))]
public class PathOverlapEditor : Editor
{

    public override void OnInspectorGUI()
    {
        var src = target as PathOverlap;

        base.OnInspectorGUI();

        // Change button based on running state
        if (src.RunState == PathOverlap.State.Running)
        {
            if (GUILayout.Button("Pause"))
                src.Pause();
        } else
        {
            if (GUILayout.Button("Play"))
                src.ExecuteAlgorithm(src.RunState == PathOverlap.State.Stopped);
        }
        

        if (GUILayout.Button("Stop"))
            src.Cancel();

        if (GUILayout.Button("Reset"))
            src.ResetOutput();

    }

}
