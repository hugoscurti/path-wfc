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

    public void Init(Tilemap target, PathOverlapModel model, Vector2Int tilepos)
    {
        //this.target = target;
        this.model = model;
        this.tilepos = tilepos;

        if (model != null)
            this.patterns = this.model.GetPatternsForWave(this.tilepos);
    }

    private void OnGUI()
    {
        if (patterns == null)
            EditorGUI.LabelField(new Rect(10, 10, 100, 15), new GUIContent("No model loaded..."));

        else if (patterns.Count == 0)
            EditorGUI.LabelField(new Rect(10, 10, 100, 15), new GUIContent("No patterns available :("));

        else
        {
            int x = 20, y = 20;

            foreach (KeyValuePair<int, Texture2D> p in patterns)
            {
                if (GUI.Button(new Rect(x, y, 100, 15), new GUIContent("Select")))
                {
                    // Fix selected wave
                    model.FixWave(tilepos, p.Key);
                    model.Print();
                    updateDict = true;
                    break;
                }
                EditorGUI.DrawPreviewTexture(new Rect(x, y + 15, 100, 100), p.Value, null, ScaleMode.ScaleToFit);

                x += 110;
                if (x + 110 > position.width )
                {
                    y += 120;
                    x = 20;
                }
            }

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
