using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

[CustomEditor(typeof(MapController))]
public class MapControllerEditor : Editor {

    public void OnEnable()
    {
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var src = target as MapController;

        if (GUILayout.Button("Load Maps"))
            src.LoadMaps();

        if (GUILayout.Button("Clear"))
            src.ClearMaps();
    }

    private string GetBaseMapDataDirectory(string lastFolder)
    {
        return Path.Combine(Application.dataPath, "Resources/MapData", lastFolder);
    }
}
