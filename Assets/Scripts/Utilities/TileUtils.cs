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
                // This should be the only time we have to instantiate tiles. We will use set color in further calls, so that we don't leak memory 
                // by instantiating many tiles for no reason
                tile = GameObject.Instantiate(blank);
                tile.color = source.GetPixel(x, y);
                tile.flags = TileFlags.None;        // Important if we want to modify tile colors afterwards

                target.SetTile(new Vector3Int(x, y, 0), tile);
            }
    }

    public static RectInt GetBounds(this Tilemap tilemap)
    {
        return new RectInt(0, 0, tilemap.cellBounds.size.x, tilemap.cellBounds.size.y);
    }
}
