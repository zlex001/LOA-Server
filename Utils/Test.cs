using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Utils
{
    public static class Test
    {
        public static List<int> GetTeleportTargets(int currentIndex, List<(float x, float y)> coords, int k = 2)
        {
            var distances = new List<(int index, float dist)>();
            var current = coords[currentIndex];

            for (int i = 0; i < coords.Count; i++)
            {
                if (i == currentIndex) continue;
                var dx = current.x - coords[i].x;
                var dy = current.y - coords[i].y;
                distances.Add((i, dx * dx + dy * dy));
            }

            return distances.OrderBy(d => d.dist).Take(k).Select(d => d.index).ToList();
        }



    }
    
}
