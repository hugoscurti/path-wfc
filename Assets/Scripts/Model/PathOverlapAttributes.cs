using System;

[Serializable]
public class PathOverlapAttributes
{
    public bool PeriodicInput;
    public bool PeriodicOutput;

    public bool forbidBufferSpaceOnBoundaries;
    public bool enforceBufferSpaceOnObstacles;

    public bool GenerateMasksFromOutput;
    public bool AddRotationsAndReflexions;

    public bool UseRandomWeights;
}
