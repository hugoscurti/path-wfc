﻿using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class PostProcessingAttributes
{
    public bool SmoothPaths;
    public bool LookForObstacles;
    [Range(0, 2)]
    public float Tolerance;
    [Range(0, 5)]
    public int Iterations;

    // If min path length > 0, remove paths that are smaller than value
    [Range(0, 50)]
    public int MinPathLengh;

}
