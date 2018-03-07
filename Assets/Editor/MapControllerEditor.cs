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

    private string GetNameWithoutExtension(FileInfo fi)
    {
        return fi.Name.Remove(fi.Name.IndexOf(fi.Extension), fi.Extension.Length);
    }

    public void OnEnable()
    {
        inputmaps = GetData(true);
        outputmaps = GetData(false);

        inputFilenames = inputmaps.Select(GetNameWithoutExtension).ToArray();
        outputFilenames = outputmaps.Select(GetNameWithoutExtension).ToArray();

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

    private string GetBaseMapDataDirectory(bool input)
    {
        return Path.Combine(Application.dataPath, "Resources/MapData", input ? MapController.INPUT : MapController.OUTPUT);
    }

    /// <summary>
    /// Get data from one of the input/output folders
    /// </summary>
    public List<FileInfo> GetData(bool input)
    {
        var folder = GetBaseMapDataDirectory(input);
        DirectoryInfo folderinfo = new DirectoryInfo(folder);

        var fileinfos = folderinfo.GetFiles("*.png").Union(folderinfo.GetFiles("*.map"));

        return fileinfos.ToList();
    }

}
