using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ExecutionAttributes
{
    public bool ShowProgress;

    [Range(0.1f, 4f)]
    public float SecondsBetweenUpdate = 0.5f;

    public bool UseFixedSeed;
    public int Seed = 123;
}

