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
    }
}
