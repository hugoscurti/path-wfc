using System;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Utilities
{
    public static class Utils
    {
        /// <summary>
        /// Pick a random index in the array based on the random number r
        /// </summary>
        public static int Random(this double[] a, double r)
        {
            double sum = a.Sum();

            if (sum == 0)
            {
                for (int j = 0; j < a.Length; j++) a[j] = 1;
                sum = a.Sum();
            }

            for (int j = 0; j < a.Length; j++) a[j] /= sum;

            int i = 0;
            double x = 0;

            while (i < a.Length)
            {
                x += a[i];
                if (r <= x) return i;
                i++;
            }

            return 0;
        }

        public static void SafeInvoke<t>(this t isi, Action<t> call) where t : ISynchronizeInvoke
        {
            if (isi.InvokeRequired) isi.BeginInvoke(call, new object[] { isi });
            else
                call(isi);
        }
    }

    public static class TilemapUtils
    {
        public static RectInt GetBounds(this Tilemap tilemap)
        {
            return new RectInt(0, 0, tilemap.cellBounds.size.x, tilemap.cellBounds.size.y);
        }
    }

    public static class ArrayUtils
    {
        public static void ForEach<T>(this T[,] array, Action<T, int, int> action)
        {
            for (int x = 0; x < array.GetLength(0); ++x)
                for (int y = 0; y < array.GetLength(1); ++y)
                    action(array[x, y], x, y);
        }

        public static void ForEach<T>(this T[,] array, Action<int, int> action)
        {
            for (int x = 0; x < array.GetLength(0); ++x)
                for (int y = 0; y < array.GetLength(1); ++y)
                    action(x, y);
        }
    }
}
