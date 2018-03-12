using System;
using UnityEngine.Tilemaps;

[Serializable]
public class PathOverlapAttributes
{
    public bool PeriodicInput;
    public bool PeriodicOutput;

    public bool GenerateMasksFromOutput;
    public bool AddRotationsAndReflexions;

    public bool UseRandomWeights;
}
