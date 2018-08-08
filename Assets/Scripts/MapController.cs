﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapController : MonoBehaviour {

    public const string INPUT_FOLDER = "MapData/input";
    public const string OUTPUT_FOLDER = "MapData/output";

    public SpriteRenderer inputTarget;
    private Texture2D inputTex;
    [MapIterator("Resources/" + INPUT_FOLDER)]
    public MapSelector inputSelector = new MapSelector();

    public SpriteRenderer outputTarget;
    private Texture2D outputTex;
    [MapIterator("Resources/" + OUTPUT_FOLDER)]
    public MapSelector outputSelector = new MapSelector();

    public SpriteRenderer Background;

    private Color[] outputPixels;

    public void LoadMaps()
    {
        ClearMaps();

        inputTex = Map.LoadMap(inputSelector.GetFile(), INPUT_FOLDER);
        LoadSprite(inputTarget, inputTex);

        outputTex = Map.LoadMap(outputSelector.GetFile(), OUTPUT_FOLDER);
        outputPixels = outputTex.GetPixels();  // Store initial output value
        LoadSprite(outputTarget, outputTex);

        // Set background alpha to be the same size as the output
        Background.size = new Vector2(outputTex.width, outputTex.height);
        Background.transform.localPosition = new Vector3(0, 0, Background.transform.localPosition.z);
    }

    public void LoadSprite(SpriteRenderer renderer, Texture2D texture)
    {
        renderer.sprite = Sprite.Create(texture, new Rect(.0f, .0f, texture.width, texture.height), new Vector2(.5f, .5f), 1f);
    }

    public void ClearMaps()
    {
        inputTarget.sprite = null;
        outputTarget.sprite = null;
        DestroyImmediate(inputTex);
        DestroyImmediate(outputTex);

        // Reset background as well
        Background.size = Vector2.zero;

        // Unload assets just in case
        Resources.UnloadUnusedAssets();
    }

    public void DestroyTiles(Tilemap target)
    {
        Vector3Int pos = Vector3Int.zero;
        var bounds = target.cellBounds;

        for (int x = 0; x < bounds.xMax; ++x)
            for (int y = 0; y < bounds.yMax; ++y)
            {
                pos.Set(x, y, 0);
                target.SetTile(pos, null);
            }

        // Clear all tiles at the end
        target.ClearAllTiles();
    }
    

    public void ResetOutput()
    {
        outputTex.SetPixels(outputPixels);
        outputTex.Apply();
    }
}
