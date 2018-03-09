using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TileUtils
{

    /// <summary>
    /// Paints a texture2d image into a tilemap target
    /// </summary>
    public static void PaintTexture(Texture2D source, Tilemap target)
    {
        Tile tile,
            blank = Resources.Load<Tile>("Tiles/White");

        for (int x = 0; x < source.width; ++x)
            for (int y = 0; y < source.height; ++y)
            {
                tile = GameObject.Instantiate(blank);
                tile.color = source.GetPixel(x, y);

                target.SetTile(new Vector3Int(x, y, 0), tile);
            }
    }
}
