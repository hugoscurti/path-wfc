﻿using UnityEngine;

public class MapController : MonoBehaviour {

    public const string INPUT_FOLDER = "MapData/input";
    public const string OUTPUT_FOLDER = "MapData/output";

    public SpriteRenderer inputTarget;
    private Texture2D inputTex;
    [MapIterator("Resources/" + INPUT_FOLDER)]
    public MapSelector inputSelector = new MapSelector();

    public SpriteRenderer outputTarget;
    private Texture2D outputTex;
    [MapIterator("Resources/" + OUTPUT_FOLDER)]
    public MapSelector outputSelector = new MapSelector();

    public SpriteRenderer Background;

    private Color[] outputPixels;

    public void LoadMaps()
    {
        ClearMaps();

        inputTex = Map.LoadMap(inputSelector.GetFile(), INPUT_FOLDER);
        LoadSprite(inputTarget, inputTex);

        outputTex = Map.LoadMap(outputSelector.GetFile(), OUTPUT_FOLDER);
        outputPixels = outputTex.GetPixels();  // Store initial output value
        LoadSprite(outputTarget, outputTex);

        // Set background alpha to be the same size as the output
        Vector2 mapsize = new Vector2(outputTex.width, outputTex.height);
        if (Background)
        {
            Background.size = mapsize;
            Background.transform.localPosition = new Vector3(0, 0, Background.transform.localPosition.z);
        }

        // Set Output collider size to fit output map's size
        var collider = outputTarget.GetComponent<BoxCollider2D>();
        if (collider)
        {
            collider.size = mapsize;
            collider.offset = outputTarget.sprite.bounds.extents;
            collider.enabled = true;
        }
    }

    public void LoadSprite(SpriteRenderer renderer, Texture2D texture)
    {
        renderer.sprite = Sprite.Create(texture, new Rect(.0f, .0f, texture.width, texture.height), Vector2.zero, 1f);
    }

    public void ClearMaps()
    {
        inputTarget.sprite = null;
        outputTarget.sprite = null;
        DestroyImmediate(inputTex);
        DestroyImmediate(outputTex);


        // Reset background and collider as well
        if (Background)
            Background.size = Vector2.zero;
        var collider = outputTarget.GetComponent<BoxCollider2D>();
        if (collider) collider.enabled = false;


        // Unload assets just in case
        Resources.UnloadUnusedAssets();
    }
    

    public void ResetOutput()
    {
        outputTex.SetPixels(outputPixels);
        outputTex.Apply();
    }
}
