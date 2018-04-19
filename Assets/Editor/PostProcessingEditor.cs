using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

[CustomEditor(typeof(PostProcessingController))]
public class PostProcessingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var ctrl = target as PostProcessingController;

        if (GUILayout.Button("Generate Paths"))
        {
            ctrl.Clear();
            ctrl.GenerateMap();
        }

        if (GUILayout.Button("Clear"))
        {
            ctrl.Clear();
        }

        //if (GUILayout.Button("Combine meshes"))
        //{
        //    ctrl.CombineMeshes();
        //}
    }
}
