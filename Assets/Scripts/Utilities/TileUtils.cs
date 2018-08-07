using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TileUtils
{
    private static Tile tile;
    private static readonly Tile blank = Resources.Load<Tile>("Tiles/White");

    private static List<Tile> tilePool = new List<Tile>(1000);
    private static int nextFreeTile = 0;

    /// <summary>
    /// Paints a texture2d image into a tilemap target
    /// </summary>
    public static void PaintTexture(Texture2D source, Tilemap target)
    {
        Vector3Int pos = Vector3Int.zero;
        for (int x = 0; x < source.width; ++x)
            for (int y = 0; y < source.height; ++y)
            {
                pos.x = x;
                pos.y = y;
                tile = (Tile)target.GetTile(pos);
                if (tile)
                {
                    target.SetColor(pos, source.GetPixel(x, y));
                }
                else
                {
                    // by instantiating many tiles for no reason
                    tile = GetNextTile();
                    tile.color = source.GetPixel(x, y);
                    tile.flags = TileFlags.None;        // Important if we want to modify tile colors afterwards
                    target.SetTile(pos, tile);
                }
            }
        tile = null;
    }

    public static RectInt GetBounds(this Tilemap tilemap)
    {
        return new RectInt(0, 0, tilemap.cellBounds.size.x, tilemap.cellBounds.size.y);
    }

    private static Tile GetNextTile()
    {
        if (nextFreeTile == tilePool.Count)
        {
            var tile = Object.Instantiate(blank);
            tilePool.Add(tile);
            nextFreeTile++;
            return tile;
        } else
        {
            return tilePool[nextFreeTile++];
        }
    }

    public static void ResetTilePool()
    {
        nextFreeTile = 0;
    }
}
