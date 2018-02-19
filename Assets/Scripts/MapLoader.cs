using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapLoader : MonoBehaviour {

    public const string INPUT = "input";
    public const string OUTPUT = "output";

    // Field that will be used by the MapLoaderEditor
    [SerializeField, HideInInspector]
    private int SelectedInputMap = 0;
    [SerializeField, HideInInspector]
    private int SelectedOutputMap = 0;

    public Tilemap inputTarget;
    public Tilemap outputTarget;

    private Texture2D output;

    public Tile blankTile;


    public void LoadMap(FileInfo file, bool input)
    {
        Tilemap target = input ? inputTarget : outputTarget;
        Texture2D source;

        target.ClearAllTiles();

        // Handle png and map types
        if (file.Extension == ".png")
        {
            string resourcePath = Path.Combine(GetResourceMapDataDirectory(input), 
                file.Name.Remove(file.Name.LastIndexOf(file.Extension), file.Extension.Length));

            source = Resources.Load<Texture2D>(resourcePath);

        } else if (file.Extension == ".map")
        {
            Map map = Map.ReadMap(file);
            source = map.ToTexture();
        }
        else
        {
            throw new NotSupportedException("File type not supported");
        }

        if (!input)
            this.output = source;

        PaintTexture(source, target);
    }

    private void PaintTexture(Texture2D source, Tilemap target)
    {
        Tile tile;

        for (int x = 0; x < source.width; ++x)
            for (int y = 0; y < source.height; ++y)
            {
                tile = Instantiate(blankTile);
                tile.color = source.GetPixel(x, y);

                target.SetTile(new Vector3Int(x, y, 0), tile);
            }
    }

    public void ClearMaps()
    {
        inputTarget.ClearAllTiles();
        outputTarget.ClearAllTiles();
    }
    

    public void ResetOutput()
    {
        outputTarget.ClearAllTiles();
        PaintTexture(output, outputTarget);
    }

    private string GetResourceMapDataDirectory(bool input)
    {
        var fold = input ? INPUT : OUTPUT;
        return $"MapData/{fold}";
    }   
}
