using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Parameter
{
    public bool FixedLength = false;
    [Range(1,100)]
    public int Distance;
}

public class PathGenerator : MonoBehaviour {

    public Parameter TopSide;
    public Parameter RightSide;
    public Parameter BottomSide;
    public Parameter LeftSide;


    public void ExecuteAlgorithm()
    {
        // 1. Find obstacle
        // Any closed polygon?

        // 2. Fulfill constraints

        // 3. Add random variables for missing sides

        // 4. Connect the sides?
    }

    private void FindObstacle()
    {

    }
}
