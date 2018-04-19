using System;
using UnityEngine.Tilemaps;

[Serializable]
public class PostProcessingAttributes
{
    public bool SmoothPath;
    public float SmoothTolerance;
    public int ChaikinIterations;

    // If min path length > 0, remove paths that are smaller than value
    public int MinPathLengh;

}
