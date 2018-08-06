using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathOverlapController))]
public class PathOverlapEditor : Editor
{

    public override void OnInspectorGUI()
    {
        var src = target as PathOverlapController;

        base.OnInspectorGUI();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Instantiate"))
        {
            src.InstantiateModel();
        }

        if (GUILayout.Button("First Propagate"))
        {
            src.FirstPropagate(true);
        }
        GUILayout.EndHorizontal();

        // Change button based on running state
        if (src.RunState == State.Running)
        {
            if (GUILayout.Button("Pause"))
                src.Pause();
        } else
        {
            if (GUILayout.Button("Play"))
                src.ExecuteAlgorithm(src.RunState == State.Stopped);
        }

        // Step by step button
        if (GUILayout.Button("Step"))
            src.ExecuteAlgorithm(src.RunState == State.Stopped, true);


        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Stop"))
            src.Cancel();

        if (GUILayout.Button("Reset"))
            src.ResetOutput();
        GUILayout.EndHorizontal();
    }

}
