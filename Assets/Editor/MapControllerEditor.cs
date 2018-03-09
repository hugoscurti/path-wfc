using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

[CustomEditor(typeof(MapController))]
public class MapControllerEditor : Editor {

    SerializedProperty selectedInput,
        selectedOutput;

    private List<FileInfo> inputmaps,
        outputmaps;

    private string[] inputFilenames,
        outputFilenames;

    public void OnEnable()
    {
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Load Maps"))
        {
            var src = target as MapController;
            src.LoadMaps();

            // Finally, init the model
            src.InitModel();
        }

        if (GUILayout.Button("Clear"))
        {
            (target as MapController).ClearMaps();
        }
    }

    private string GetBaseMapDataDirectory(string lastFolder)
    {
        return Path.Combine(Application.dataPath, "Resources/MapData", lastFolder);
    }
}
