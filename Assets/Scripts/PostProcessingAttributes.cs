﻿using System;
using UnityEngine;

[Serializable]
public class PostProcessingAttributes
{
    public bool ApplyPostProcessing;
    // If min path length > 0, remove paths that are smaller than value
    [Range(0, 50)]
    public int MinPathLengh;

    [Range(0, 2)]
    public float Tolerance;

    [Range(0, 5)]
    public int Iterations;

}
