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
        target.ClearAllTiles();
        TileUtils.PaintTexture(source, target);
    }

    public void ClearMaps()
    {
        //Manually destroy tiles
        DestroyTiles(inputTarget);
        DestroyTiles(outputTarget);

        EditorUtility.UnloadUnusedAssetsImmediate();
    }

    public void DestroyTiles(Tilemap target)
    {
        TileBase[] tiles = target.GetTilesBlock(target.cellBounds);
        target.ClearAllTiles();

        for(int i = 0; i < tiles.Length; ++i)
        {
            DestroyImmediate(tiles[i]); // when in editor, we call destroyimmediate
            tiles[i] = null;
        }
    }
    

    public void ResetOutput()
    {
        DestroyTiles(outputTarget);
        TileUtils.PaintTexture(output, outputTarget);
    }
}
