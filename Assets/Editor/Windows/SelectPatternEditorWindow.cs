using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SelectPatternEditorWindow : EditorWindow
{
    //private Tilemap target;
    private PathOverlapModel model;
    private Vector2Int tilepos;

    Dictionary<int, Texture2D> patterns;
    bool updateDict = false;

    // Pattern info
    int patternSize = 50;
    int margin = 15;
    int buttonHeight = 15;

    // Scrollable
    Vector2 scrollPos;

    [MenuItem("Window/Select Pattern")]
    static void ShowWindow()
    {
        var window = GetWindow<SelectPatternEditorWindow>();

        //window.position = new Rect(50, 50, 50, 50);
        //window.minSize = new Vector2(100, 100);
        window.Show();
    }

    public void Init(Tilemap target, PathOverlapModel model, Vector2Int tilepos)
    {
        //this.target = target;
        this.model = model;
        this.tilepos = tilepos;

        if (model != null)
            patterns = this.model.GetPatternsForWave(this.tilepos);
    }

    private void OnGUI()
    {
        if (patterns == null)
            EditorGUI.LabelField(new Rect(10, 10, 100, 15), new GUIContent("No model loaded..."));

        else if (patterns.Count == 0)
            EditorGUI.LabelField(new Rect(10, 10, 100, 15), new GUIContent("No patterns available :("));

        else
        {
            float patternPerLine = (int)(position.width / (margin + patternSize));
            int actualHeight = margin + (int)Math.Floor(patterns.Count / patternPerLine) * (buttonHeight + patternSize + margin);

            scrollPos = GUI.BeginScrollView(new Rect(0, 0, position.width, position.height), scrollPos, new Rect(0, 0, position.width - 15, actualHeight));
            int x = margin, y = margin;

            foreach (KeyValuePair<int, Texture2D> p in patterns)
            {
                if (GUI.Button(new Rect(x, y, patternSize, buttonHeight), new GUIContent("Select")))
                {
                    // Fix selected wave
                    model.FixWave(tilepos, p.Key);
                    model.Print();
                    updateDict = true;
                    break;
                }
                EditorGUI.DrawPreviewTexture(new Rect(x, y + buttonHeight, patternSize, patternSize), p.Value, null, ScaleMode.ScaleToFit);

                x += patternSize + margin;
                if (x + patternSize + margin > position.width )
                {
                    y += buttonHeight + patternSize + margin;
                    x = margin;
                }
            }
            GUI.EndScrollView();

            if (updateDict)
            {
                // Reset dict of patterns (Since it most likely have changed
                patterns = this.model.GetPatternsForWave(this.tilepos);
                updateDict = false;
            }
        }
    }

    private Texture2D Scale(Texture2D img, int factor)
    {
        Texture2D res = new Texture2D(img.width * factor, img.height * factor);

        for (int x = 0; x < res.width; ++x)
            for (int y = 0; y < res.height; ++y)
            {
                Color c = img.GetPixel(x / factor, y / factor);
                res.SetPixel(x, y, c);
            }

        return res;
    }
}
