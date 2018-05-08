using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Utilities
{
    public static class EngineUtils
    {
        public static void Clear(this Transform transform)
        {
            var tempChildren = transform.Cast<Transform>().ToList();
            foreach (Transform child in tempChildren)
                GameObject.DestroyImmediate(child.gameObject);

        }

        public static bool Intersect(this Ray2D ray, Rect box, float maxDistance)
        {
            Vector2 tmin = new Vector2(
                (box.min.x - ray.origin.x) / ray.direction.x,
                (box.min.y - ray.origin.y) / ray.direction.y);

            Vector2 tmax = new Vector2(
                (box.max.x - ray.origin.x) / ray.direction.x,
                (box.max.y - ray.origin.y) / ray.direction.y);

            // TEST: Check for divisions by 0
            if (float.IsNaN(tmin.x) || float.IsNaN(tmin.y) || float.IsNaN(tmax.x) || float.IsNaN(tmax.y))
            {
                Debug.Log("Ok cool");
            }

            // Swap min and max
            float temp;
            if (tmin.x > tmax.x)
            {
                temp = tmin.x;
                tmin.x = tmax.x;
                tmax.x = temp;
            }

            if (tmin.y > tmax.y)
            {
                temp = tmin.y;
                tmin.y = tmax.y;
                tmax.y = temp;
            }


            // Get final min max intersect
            float tmin_f = Mathf.Max(tmin.x, tmin.y);
            float tmax_f = Mathf.Min(tmax.x, tmax.y);

            if (tmin_f > tmax_f) return false;

            // Check for intersection inside the line segment
            else if (tmax_f < 0 || tmin_f > maxDistance) return false;

            else return true;
        }

        // Not sure about this function...
        private static void SwapMinMax(ref float min, ref float max)
        {
            if (min > max)
            {
                float temp = min;
                min = max;
                max = temp;
            }
        }
    }
}
