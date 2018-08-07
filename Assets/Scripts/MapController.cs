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

    public SpriteRenderer Background;

    private Texture2D output;

    public void LoadMaps()
    {
        ClearMaps();

        Texture2D input_src = Map.LoadMap(inputSelector.GetFile(), INPUT_FOLDER);
        LoadMap(inputTarget, input_src);

        this.output = Map.LoadMap(outputSelector.GetFile(), OUTPUT_FOLDER);
        LoadMap(outputTarget, this.output);

        // Set background alpha to be the same size as the output
        Background.size = new Vector2(output.width, output.height);
        Background.transform.localPosition = new Vector3(output.width/2f, output.height/2f, Background.transform.localPosition.z);
    }

    public void LoadMap(Tilemap target, Texture2D source)
    {
        TileUtils.PaintTexture(source, target);
    }

    public void ClearMaps()
    {
        //Manually destroy tiles
        DestroyTiles(inputTarget);
        DestroyTiles(outputTarget);
        // Reset background as well
        Background.size = Vector2.zero;

        TileUtils.ResetTilePool();
        GC.Collect();
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
        TileUtils.PaintTexture(output, outputTarget);
    }
}
