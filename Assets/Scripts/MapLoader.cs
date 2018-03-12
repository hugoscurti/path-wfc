using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.Tilemaps;

public class MapLoader : MonoBehaviour
{
    public Tilemap Target;
    
    private Texture2D map;
    private const string ResourceDirectory = "MapData/Output";

    [MapIterator("Resources/" + ResourceDirectory)]
    public MapSelector outputmap = new MapSelector();

    

    /// <summary>
    /// Load map to target
    /// </summary>
    public void LoadMap()
    {
        FileInfo file = outputmap.GetFile();

        Target.ClearAllTiles();

        map = Map.LoadMap(file, ResourceDirectory);

        TileUtils.PaintTexture(map, Target);
    }

    public void ClearMap()
    {
        Target.ClearAllTiles();
    }
}