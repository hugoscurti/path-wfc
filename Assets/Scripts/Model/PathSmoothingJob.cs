using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Utilities;

public struct PathSmoothingJob : IJob
{
    // Properties needed
    [ReadOnly]
    public int Iterations;
    [ReadOnly]
    public float Tolerance;

    public NativeArray<Vector3> Path;
    public NativeArray<int> PathLength;

    [ReadOnly]
    public NativeArray<int> Obstacles;
    [ReadOnly]
    public RectInt ImageSize;

    public void Execute()
    {
        // Store path in list for easier processing
        List<Vector3> tempPath = new List<Vector3>();
        for (int i = 0; i < PathLength[0]; ++i)
            tempPath.Add(Path[i]);


        SimplifyPath(tempPath, Tolerance);

        SmoothenPath(tempPath, Iterations);
        
        // Put result back in path
        PathLength[0] = tempPath.Count;
        for (int i = 0; i < tempPath.Count; ++i)
            Path[i] = tempPath[i];
    }


    private void SmoothenPath(List<Vector3> path, int iterations)
    {
        List<Vector3> lcurr = new List<Vector3>(), lprev = path;
        List<Vector3> temp;

        bool isLoop = Path[0] == Path[PathLength[0] - 1];
        if (iterations == 0) return;

        Vector3 qi, ri, rprev = Vector3.zero;

        while (iterations-- > 0)
        {
            lcurr.Clear();
            for (int i = 0; i < lprev.Count - 1; ++i)
            {
                qi = 0.75f * lprev[i] + 0.25f * lprev[i + 1];
                ri = 0.25f * lprev[i] + 0.75f * lprev[i + 1];


                // See if new lines intersect obstacles
                if (i > 0)
                {
                    if (CrossObstacle(new Vector2(rprev.x, rprev.z), new Vector2(qi.x, qi.z)))
                        // Insert old point
                        lcurr.Add(lprev[i]);
                }

                lcurr.Add(qi);
                lcurr.Add(ri);

                rprev = ri;
            }

            // Close loop if it's a loop
            if (isLoop)
            {
                if (CrossObstacle(new Vector2(rprev.x, rprev.z), new Vector2(lcurr[0].x, lcurr[0].z)))
                    // Insert old point
                    lcurr.Add(lprev[0]);

                lcurr.Add(lcurr[0]);
            }

            // Switch between the 2 lists to prevent instantiating new lists uselessly
            temp = lprev;
            lprev = lcurr;
            lcurr = temp;
        }

        // Put result in path
        if (lprev != path)
        {
            path.Clear();
            path.AddRange(lprev);
        }
    }

    public void SimplifyPath(List<Vector3> path, float tolerance)
    {
        //Ramer-Douglas-Peucker for path simplification
        HashSet<int> toRemove = new HashSet<int>();

        Queue<int> qbegin = new Queue<int>();
        Queue<int> qend = new Queue<int>();
        int lineBegin, lineEnd;

        Vector2 start, end, point = start = end = Vector2.zero;

        // Start with first and last point
        qbegin.Enqueue(0);
        qend.Enqueue(path.Count - 1);

        while (qbegin.Count > 0)
        {
            int maxPoint = 0;
            float dist, maxDist = -1;

            lineBegin = qbegin.Dequeue();
            lineEnd = qend.Dequeue();

            start.Set(path[lineBegin].x, path[lineBegin].z);
            end.Set(path[lineEnd].x, path[lineEnd].z);

            // Find furthest point from the line
            for (int i = lineBegin + 1; i < lineEnd; ++i)
            {
                point.Set(path[i].x, path[i].z);
                dist = PerpDist(start, end, point);

                if (dist > maxDist)
                {
                    maxDist = dist;
                    maxPoint = i;
                }
            }

            // If distance is smaller than epsilon and 
            if (maxDist <= tolerance && !CrossObstacle(start, end))
            {
                // Remove points between start and end
                for (int i = lineBegin + 1; i < lineEnd; ++i)
                    toRemove.Add(i);
            }
            else
            {
                // Split in 2
                qbegin.Enqueue(lineBegin);
                qend.Enqueue(maxPoint);

                qbegin.Enqueue(maxPoint);
                qend.Enqueue(lineEnd);
            }
        }

        // Remove all points from toRemove
        for (int i = path.Count - 1; i >= 0; --i)
            if (toRemove.Contains(i))
                path.RemoveAt(i);
    }


    private bool CrossObstacle(Vector2 p1, Vector2 p2)
    {
        // Add a small threshold to the Rect
        float threshold = 0.2f;

        // Ray's direction is normalized...
        Vector2 dist = p2 - p1;
        float maxDist = dist.magnitude;
        Ray2D ray = new Ray2D(p1, dist);
        Rect obst = new Rect();
        for (int i = 0; i < Obstacles.Length; ++i)
        {
            /// Set obstacle
            obst.x = (Obstacles[i] % ImageSize.width) - threshold;
            obst.y = (Obstacles[i] / ImageSize.width) - threshold;
            obst.size = Vector2.one * (1 + threshold * 2);

            // Set line
            if (ray.Intersect(obst, maxDist))
                return true;
        }

        return false;
    }

    public static float PerpDist(Vector2 start, Vector2 end, Vector2 point)
    {
        if (start == end)
            // Point-to-point distance
            return Vector2.Distance(start, point);

        float num = (end.y - start.y) * point.x - (end.x - start.x) * point.y + (end.x * start.y) - (end.y * start.x);
        num = Mathf.Abs(num);

        return num / Vector2.Distance(start, end);
    }
}
