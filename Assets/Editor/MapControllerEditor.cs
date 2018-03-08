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
        inputmaps = FileUtils.GetData(GetBaseMapDataDirectory(MapController.INPUT));
        outputmaps = FileUtils.GetData(GetBaseMapDataDirectory(MapController.OUTPUT));

        inputFilenames = inputmaps.Select(FileUtils.GetNameWithoutExtension).ToArray();
        outputFilenames = outputmaps.Select(FileUtils.GetNameWithoutExtension).ToArray();

        selectedInput = serializedObject.FindProperty("SelectedInputMap");
        selectedOutput = serializedObject.FindProperty("SelectedOutputMap");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        
        // Extend layout
        selectedInput.intValue = EditorGUILayout.Popup("Select input: ", selectedInput.intValue, inputFilenames);
        selectedOutput.intValue = EditorGUILayout.Popup("Select output: ", selectedOutput.intValue, outputFilenames);

        if (GUILayout.Button("Load Maps"))
        {
            var src = target as MapController;
            var map = inputmaps[selectedInput.intValue];
            src.LoadMap(map, true);

            map = outputmaps[selectedOutput.intValue];
            src.LoadMap(map, false);

            // Finally, init the model
            src.InitModel();
        }

        if (GUILayout.Button("Clear"))
        {
            (target as MapController).ClearMaps();
        }

        // Apply changes to the serialized properties
        serializedObject.ApplyModifiedProperties();
    }

    private string GetBaseMapDataDirectory(string lastFolder)
    {
        return Path.Combine(Application.dataPath, "Resources/MapData", lastFolder);
    }
}
