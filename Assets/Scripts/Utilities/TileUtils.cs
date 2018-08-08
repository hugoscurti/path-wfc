using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TileUtils
{
    /// <summary>
    /// Convert color into indices.
    /// Since we know exactly the expected color, we can hard-code the color indices
    /// and look for them.
    /// </summary>
    public static byte[] IndexColours(Color32[] source, List<Color32> colors)
    {
        Color32 c;
        int idx;

        byte[] indices = new byte[source.Length];

        for (int i = 0; i < source.Length; ++i) {
            c = source[i];
            idx = colors.IndexOf(c);
            if (idx == -1)
            {
                idx = colors.Count;
                colors.Add(c);
            }

            indices[i] = (byte)idx;
        }

        return indices;
    }
}
