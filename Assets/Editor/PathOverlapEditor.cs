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
        base.OnInspectorGUI();

        // Add button to execute algorithm

        if (GUILayout.Button("Execute"))
        {
            // Execute the thing
            (target as PathOverlap).ExecuteAlgorithm();
        }

    }

}
