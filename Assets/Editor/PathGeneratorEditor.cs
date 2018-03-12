using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PathGenerator))]
public class PathGeneratorEditor : Editor {

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Reset"))
        {
            
        }

        base.OnInspectorGUI();
    }
}
