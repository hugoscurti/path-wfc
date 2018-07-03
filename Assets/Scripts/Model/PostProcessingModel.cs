using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class PostProcessingModel
{

    private PathOverlapModel model;
    private RectInt outsize;
    private bool[] isPath;

    // First 4 values are non diags offset. Last 4 values are diags offset
    private readonly static int[] DX = { -1,  0,  1,  0, -1, -1,  1,  1 };
    private readonly static int[] DY = {  0, -1,  0,  1, -1,  1, -1,  1 };


    public void Init(PathOverlapModel model)
    {
        this.model = model;
        this.outsize = model.GetOutputRect();
        this.isPath = model.IsPath();
    }

    #region Static functions


    /// <summary>
    /// Determine where this new neighbour will go. 
    /// If it already is a neighbour, returns none.
    /// </summary>
    public static bool IsNextNeighbour(LinkedListNode<int> node, int nextval, bool goforward)
    {
        if (node.Previous != null && node.Previous.Value == nextval) return false;
        if (node.Next != null && node.Next.Value == nextval) return false;

        if (goforward) return node.Next == null;
        else return node.Previous == null;
    }

    #endregion

    /// <summary>
    /// Return the list of paths
    /// </summary>
    public List<LinkedList<int>> GetPaths()
    {
        List<LinkedList<int>> paths = new List<LinkedList<int>>();

        // At this point, we should have one color index for each position.
        // We can then look only for paths and filter out the smaller ones (or the ones that don't go around an obstacle?)
        for (int i = 0; i < isPath.Length; ++i)
        {
            // Tile is not a path
            if (!isPath[i]) continue;

            // Path has already been processed
            if (paths.Any(p => p.Contains(i))) continue;

            paths.Add(GetPath(i));
        }

        return paths;
    }

    private LinkedList<int> GetPath(int i)
    {
        int y = i / outsize.width;
        int x = i % outsize.width;

        // Process path
        LinkedList<int> path = new LinkedList<int>();
        LinkedListNode<int> first = path.AddFirst(i);

        LinkedListNode<int> current = first;

        // Look for previous nodes
        ProcessPath(first, current, x, y, false);


        // Look for next nodes (if not cyclic)
        if (path.First.Value != path.Last.Value)
        {
            current = first;
            x = i % outsize.width;
            y = i / outsize.width;
            ProcessPath(first, current, x, y, true);
        }

        return path;
    }


    private void ProcessPath(LinkedListNode<int> first, LinkedListNode<int> current, int x, int y, bool goforward)
    {
        // TODO: Handle crossing path
        bool neighbourset;
        int nx, ny, ni;

        do
        {
            neighbourset = false;
            for (int d = 0; d < 8; ++d)
            {
                // 1. Get offset position
                nx = x + DX[d];
                ny = y + DY[d];
                ni = ny * outsize.width + nx;

                if (model.attributes.PeriodicOutput)
                {
                    if (nx < 0) nx = outsize.width - 1;
                    if (ny < 0) ny = outsize.height - 1;
                    if (nx >= outsize.width) nx = 0;
                    if (ny >= outsize.height) ny = 0;
                }
                else
                {
                    if (nx < 0 || ny < 0 || nx >= outsize.width || ny >= outsize.height)
                        continue;
                }


                // 2. Determine if it's the next segment in the current path
                if (isPath[ni])
                {
                    if (IsNextNeighbour(current, ni, goforward))
                    {
                        neighbourset = true;

                        current = goforward ? 
                            current.List.AddAfter(current, ni) : 
                            current.List.AddBefore(current, ni);
                        x = ni % outsize.width;
                        y = ni / outsize.width;
                        break;
                    }
                }
            }
        } while (neighbourset && current.Value != first.Value);
    }

}

