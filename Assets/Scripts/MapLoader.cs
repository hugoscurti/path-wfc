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

    [MapIterator("Resources/MapData/output")]
    public MapSelector outputmap = new MapSelector();

    private string ResourceDirectory = "MapData/Output";

    /// <summary>
    /// Load map to target
    /// </summary>
    public void LoadMap()
    {
        FileInfo file = outputmap.GetFile();

        Target.ClearAllTiles();

        Texture2D source = Map.LoadMap(file, ResourceDirectory);

        TileUtils.PaintTexture(source, Target);
    }

    public void ClearMap()
    {
        Target.ClearAllTiles();
    }
}