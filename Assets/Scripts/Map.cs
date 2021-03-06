﻿using System;
using System.IO;
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
        Texture2D res = new Texture2D(Width, Height)
        {
            filterMode = FilterMode.Point,
            anisoLevel = 1,
            alphaIsTransparency = true
        };

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

        res.Apply();
        return res;
    }
    

    /// <summary>
    /// Loads a .png or .map file and returns a Texture2d representation
    /// </summary>
    public static Texture2D LoadMap(FileInfo file, string ResourceDirectory)
    {
        // Handle png and map types
        if (file.Extension == ".png")
        {
            string resourcePath = Path.Combine(ResourceDirectory,
                file.Name.Remove(file.Name.LastIndexOf(file.Extension), file.Extension.Length));

            var tex = Resources.Load<Texture2D>(resourcePath);
            var returnedVal = GameObject.Instantiate(tex);
            Resources.UnloadAsset(tex);
            return returnedVal;
        }
        else if (file.Extension == ".map")
        {
            Map map = Map.ReadMap(file);
            return map.ToTexture();
        }
        else
        {
            throw new NotSupportedException("File type not supported");
        }
    }


    /// <summary>
    /// Reads a .map file and returns a Map object
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
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

        //Values are stored from bottom to top, so we iterate backwards
        for (int y = map.Height - 1; y >= 0; --y)
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
                    case 'W':
                        map.grid[x, y] = TileType.Obstacle;
                        break;
                    case '.':
                    case 'G':
                    case 'S':
                        map.grid[x, y] = TileType.Freespace;
                        break;
                    default:
                        throw new Exception("Character not recognized");
                }
            }
        }
    }
}
