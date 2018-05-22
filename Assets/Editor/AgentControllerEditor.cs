using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(AgentController))]
public class AgentControllerEditor : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var ctrl = target as AgentController;


        if (ctrl.PathIndices != null && ctrl.PathIndices.Length > 0)
        {
            EditorGUILayout.PrefixLabel("Select path");
            ctrl.selectedPath = EditorGUILayout.Popup(ctrl.selectedPath, ctrl.PathIndices);

            // Button to set agent on path
            if (GUILayout.Button("Set agent"))
            {
                ctrl.EnableAgent();
            }
        } else
        {
            var centered = new GUIStyle(GUI.skin.GetStyle("Label"));
            centered.alignment = TextAnchor.UpperCenter;
            centered.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField("No path found", centered);
        }
    }

}
