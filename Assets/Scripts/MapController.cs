using System;
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

    public Tilemap inputTarget;
    [MapIterator("Resources/" + INPUT_FOLDER)]
    public MapSelector inputSelector = new MapSelector();

    public Tilemap outputTarget;
    [MapIterator("Resources/" + OUTPUT_FOLDER)]
    public MapSelector outputSelector = new MapSelector();

    public PathOverlapAttributes ModelAttributes;

    private Texture2D output;

    public void LoadMaps()
    {
        Texture2D input_src = Map.LoadMap(inputSelector.GetFile(), INPUT_FOLDER);
        LoadMap(inputTarget, input_src);

        this.output = Map.LoadMap(outputSelector.GetFile(), OUTPUT_FOLDER);
        LoadMap(outputTarget, this.output);
    }

    public void LoadMap(Tilemap target, Texture2D source)
    {
        target.ClearAllTiles();
        TileUtils.PaintTexture(source, target);
    }

    public void InitModel()
    {
        var modelComponent = GetComponent<PathOverlap>();
        modelComponent.InstantiateModel(inputTarget, outputTarget, ModelAttributes);
    }

    public void ClearMaps()
    {
        inputTarget.ClearAllTiles();
        outputTarget.ClearAllTiles();
    }
    

    public void ResetOutput()
    {
        outputTarget.ClearAllTiles();
        TileUtils.PaintTexture(output, outputTarget);
    }
}
