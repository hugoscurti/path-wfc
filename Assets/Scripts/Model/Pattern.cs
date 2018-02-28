using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pattern
{
    private int N;
    private byte[] indices;

    #region Constructor

    public Pattern(int N, int x, int y, byte[,] colorIndices)
    {
        this.N = N;

        int width = colorIndices.GetLength(0),
            height = colorIndices.GetLength(1);

        Func<int, int, byte> f = (dx, dy) => colorIndices[(x + dx) % width, (y + dy) % height];
        indices = ApplyPattern(f);
    }

    /// <summary>
    /// Constructor that creates a pattern containing the same color for each position
    /// </summary>
    public Pattern(int N, byte colorIndex)
    {
        this.N = N;
        indices = ApplyPattern((x, y) => colorIndex);
    }

    private Pattern(Pattern p, Func<int, int, byte> transform)
    {
        this.N = p.N;
        indices = ApplyPattern(transform);
    }

    #endregion

    #region Functions

    /// <summary>
    /// Apply a function for each (x, y) of a pattern.
    /// Pattern are store in a one dimensional array
    /// </summary>
    private byte[] ApplyPattern(Func<int, int, byte> f)
    {
        byte[] result = new byte[N * N];

        for (int y = 0; y < N; ++y)
            for (int x = 0; x < N; ++x)
                result[x + y * N] = f(x, y);

        return result;
    }

    // Apply rotation from pattern and return new rotated pattern
    public Pattern Rotate()
    {
        return new Pattern(this, (x, y) => this.indices[N - 1 - y + x * N]);
    }

    public Pattern Reflect()
    {
        return new Pattern(this, (x, y) => this.indices[N - 1 - x + y * N]);
    }

    public bool ContainsColor(byte colorIdx)
    {
        return indices.Any(i => i == colorIdx);

    }

    public bool ContainsOnly(byte colorIdx)
    {
        return indices.All(i => i == colorIdx);
    }

    /// <summary>
    /// Generate a unique index that represents the pattern
    /// </summary>
    /// <param name="C">Total number of unique colors</param>
    public long GetIndex(int C)
    {
        long result = 0,
            power = 1;

        for (int i = indices.Length - 1; i >= 0; --i)
        {
            result += indices[i] * power;
            power *= C;
        }

        return result;
    }

    /// <summary>
    /// Returns the list of indices that overlaps with
    /// the current pattern
    /// </summary>
    /// <param name="ps"></param>
    /// <returns></returns>
    public List<int> Overlap(Pattern[] ps, int dx, int dy)
    {
        List<int> overlaps = new List<int>();

        for (int t = 0; t < ps.Length; ++t)
        {
            if (Agrees(ps[t], dx, dy))
                overlaps.Add(t);
        }

        return overlaps;
    }

    /// <summary>
    /// Verify if overlapping patterns p1 and p2 over their difference in dx and dy
    /// aggrees in color for all overlapping pixels.
    /// dy and dx = 0 means patterns overlap completely.
    /// </summary>
    public bool Agrees(Pattern p2, int dx, int dy)
    {
        int xmin = dx < 0 ? 0 : dx,
            xmax = dx < 0 ? dx + N : N,
            ymin = dy < 0 ? 0 : dy,
            ymax = dy < 0 ? dy + N : N;

        for (int y = ymin; y < ymax; ++y)
        {
            for (int x = xmin; x < xmax; ++x)
            {
                // As soon as one pair of colors disagree, return false
                if (this.Get(x, y) != p2.Get(x - dx, y - dy))
                    return false;
            }
        }

        //Else return true
        return true;
    }

    public byte Get(int x, int y)
    {
        return indices[x + y * N];
    }

    public Texture2D Print(List<Color> colors)
    {
        Texture2D res = new Texture2D(N, N, TextureFormat.RGBAFloat, false);
        res.filterMode = FilterMode.Point;
        res.alphaIsTransparency = true;

        for (int x = 0; x < N; ++x)
            for(int y = 0; y < N; ++y)
            {
                res.SetPixel(x, y, colors[Get(x, y)]);
            }

        res.Apply();

        return res;
    }

    #endregion

}
