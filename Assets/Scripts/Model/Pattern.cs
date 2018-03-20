using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pattern
{
    private int N;
    private byte[] indices;

    public static byte mask_idx = 0;

    #region Constructor

    public Pattern(int N, int x, int y, byte[] colorIndices, RectInt size, byte? maskColor = null)
    {
        this.N = N;

        Func<int, int, byte> f = (dx, dy) => {
            int sx = (x + dx) % size.width,
                sy = (y + dy) % size.height;

            var col_idx = colorIndices[sy * size.width + sx];

            if (maskColor.HasValue && col_idx == maskColor.Value)
                return mask_idx;
            else
                return col_idx;
        };
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
    /// 
    /// TODO: Handle case with mask pattern
    /// </summary>
    public bool Agrees(Pattern p2, int dx, int dy)
    {
        int xmin = dx < 0 ? 0 : dx,
            xmax = dx < 0 ? dx + N : N,
            ymin = dy < 0 ? 0 : dy,
            ymax = dy < 0 ? dy + N : N;

        byte c1, c2;

        for (int y = ymin; y < ymax; ++y)
        {
            for (int x = xmin; x < xmax; ++x)
            {
                c1 = Get(x, y);
                c2 = p2.Get(x - dx, y - dy);

                // Mask color should fit with everything but a path
                if (c1 == mask_idx && c2 == PathOverlapModel.path_idx) return false;
                if (c2 == mask_idx && c1 == PathOverlapModel.path_idx) return false;

                // As soon as one pair of colors disagree, return false
                if (c1 != mask_idx && c2 != mask_idx && c1 != c2)
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
        Texture2D res = new Texture2D(N, N, TextureFormat.RGBA32, false) {
            filterMode = FilterMode.Point,
            alphaIsTransparency = true
        };

        byte col;

        for (int x = 0; x < N; ++x)
            for(int y = 0; y < N; ++y)
            {
                col = Get(x, y);
                if (col == mask_idx)
                    res.SetPixel(x, y, Color.clear);
                else
                    res.SetPixel(x, y, colors[Get(x, y)]);
            }

        res.Apply();

        return res;
    }

    #endregion

}
