using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public enum TileType
{
    Freespace,
    Obstacle,
    Path
}

public class Map
{
    public int Width { get; set; }
    public int Height { get; set; }

    public TileType[,] grid;

    public Texture2D ToTexture()
    {
        Texture2D res = new Texture2D(Width, Height);
        Color c;

        for (int x = 0; x < Width; ++x)
            for(int y = 0; y < Height; ++y)
            {
                switch (grid[x, y])
                {
                    case TileType.Freespace:
                        c = PathOverlapModel.freespace;
                        break;
                    case TileType.Obstacle:
                        c = PathOverlapModel.obstacle;
                        break;
                    case TileType.Path:
                        c = PathOverlapModel.path;
                        break;
                    default:
                        throw new NotSupportedException("Tile type not supported");
                }

                res.SetPixel(x, y, c);
            }
        return res;
    }

    public static Map ReadMap(FileInfo file)
    {
        Map map = new Map();

        using (FileStream fs = file.OpenRead())
        using (StreamReader sr = new StreamReader(fs))
        {

            //Line 1 : type octile
            ReadLine(sr, "type octile");

            //Line 2 : height
            map.Height = ReadIntegerValue(sr, "height");

            //Line 3 : width
            map.Width = ReadIntegerValue(sr, "width");

            //Line 4 to end : map
            ReadLine(sr, "map");

            map.grid = new TileType[map.Width, map.Height];

            //Read tiles section
            ReadTiles(sr, map);

            return map;
        }
    }

    /// <summary>
    /// Read a line and expect the line to be the value passed in arguments
    /// </summary>
    private static void ReadLine(StreamReader sr, string value)
    {
        string line = sr.ReadLine();
        if (line != value) throw new Exception(
                string.Format("Invalid format. Expected: {0}, Actual: {1}", value, line));
    }

    /// <summary>
    /// Returns an integer value from the streamreader that comes
    /// right after a key separated by a space.
    /// I.E. width 5
    /// </summary>
    private static int ReadIntegerValue(StreamReader sr, string key)
    {
        string[] block = sr.ReadLine().Split(null);
        if (block[0] != key) throw new Exception(
                    string.Format("Invalid format. Expected: {0}, Actual: {1}", key, block[0]));

        return int.Parse(block[1]);
    }

    /// <summary>
    /// Read tiles from the map file, adding tiles and filling obstacles in the array
    /// </summary>
    private static void ReadTiles(StreamReader sr, Map map)
    {
        char c;
        string line;

        for (int y = 0; y < map.Height; ++y)
        {
            line = sr.ReadLine();

            for (int x = 0; x < map.Width; ++x)
            {
                c = line[x];

                switch (c)
                {
                    case '@':
                    case 'O':
                    case 'T':
                        map.grid[x, y] = TileType.Obstacle;
                        break;
                    case '.':
                    case 'G':
                        map.grid[x, y] = TileType.Freespace;
                        break;
                    default:
                        throw new Exception("Character not recognized");
                }
            }
        }
    }
}
