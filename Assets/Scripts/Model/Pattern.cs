using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pattern
{
    private int N;
    private byte[] indices;

    public static byte mask_idx_l1 = 0;
    public static byte mask_idx_l2 = 0;

    public static Mask layer1;
    public static Mask layer2;

    private static List<int> overlaps = new List<int>(256);

    #region Constructor

    public Pattern(int N, int x, int y, byte[] colorIndices, RectInt size)
    {
        this.N = N;

        Func<int, int, byte> f = (dx, dy) => {
            int sx = (x + dx) % size.width,
                sy = (y + dy) % size.height;

            var col_idx = colorIndices[sy * size.width + sx];

            return col_idx;
        };
        indices = ApplyPattern(f);
    }

    public Pattern(int N, int x, int y, byte[] colorIndices, RectInt size, byte maskColor)
    {
        this.N = N;

        Func<int, int, byte> f = (dx, dy) => {
            int sx = (x + dx) % size.width,
                sy = (y + dy) % size.height;

            var col_idx = colorIndices[sy * size.width + sx];

            if (col_idx == maskColor)
            {
                // First step, put everything to layer 2
                return mask_idx_l2;
            }
            else return col_idx;
        };
        indices = ApplyPattern(f);

        // Second step, change layer masks
        for(int ny = 0; ny < N; ++ny)
            for(int nx = 0; nx < N; ++nx)
            {
                if (indices[ny * N + nx] == mask_idx_l2)
                {
                    indices[ny * N + nx] = DetermineMask(nx, ny);
                }
            }

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

    public bool ContainsMasks()
    {
        return indices.Any(i => i == mask_idx_l1 || i == mask_idx_l2);
    }

    public bool ContainsOnly(byte colorIdx)
    {
        return indices.All(i => i == colorIdx);
    }

    public bool ContainsOnlyMasks()
    {
        return indices.All(i => i == mask_idx_l1 || i == mask_idx_l2);
    }

    public byte DetermineMask(int x, int y)
    {
        // If we are one unit away from a boundary, then it's layer 1.
        // Else it's layer 2.
        int nx, ny;
        for(int dy = -1; dy <= 1; dy++)
            for(int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                nx = x + dx;
                ny = y + dy;
                if (nx < 0 || nx >= N || ny < 0 || ny >= N) continue;

                if (indices[ny * N + nx] == PathOverlapModel.obstacle_idx)
                    return mask_idx_l1;
            }

        return mask_idx_l2;
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
    /// the current pattern. Store result in propagator array
    /// </summary>
    public void Overlap(Pattern[] ps, int dx, int dy, out int[] propagator)
    {
        overlaps.Clear();

        for (int t = 0; t < ps.Length; ++t)
        {
            if (Agrees(ps[t], dx, dy))
                overlaps.Add(t);
        }

        propagator = new int[overlaps.Count];
        for (int i = 0; i < overlaps.Count; ++i)
            propagator[i] = overlaps[i];
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
        Mask tempmask1, tempmask2;

        for (int y = ymin; y < ymax; ++y)
        {
            for (int x = xmin; x < xmax; ++x)
            {
                c1 = Get(x, y);
                c2 = p2.Get(x - dx, y - dy);

                tempmask1 = GetMask(c1);
                tempmask2 = GetMask(c2);
                if (tempmask1 != null)
                {
                    if (!tempmask1.Agrees(c2)) return false;
                }
                else if (tempmask2 != null)
                {
                    if (!tempmask2.Agrees(c1)) return false;
                }
                // No mask
                else if (c1 != c2)
                    return false;
            }
        }

        //Else return true
        return true;
    }

    public Mask GetMask(int idx)
    {
        if (idx == mask_idx_l1) return layer1;
        else if (idx == mask_idx_l2) return layer2;
        else return null;
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
