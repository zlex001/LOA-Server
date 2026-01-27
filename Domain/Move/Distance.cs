using Logic;
using System.Collections.Generic;

namespace Domain.Move
{
    public static class Distance
    {
        private static readonly Dictionary<string, int> _cache = new Dictionary<string, int>();

        private static string CacheKey(int startGid, int destGid)
        {
            return $"{startGid}_{destGid}";
        }
        public static T Nearest<T>(Character character, Func<T, bool> predicate) where T : Character
        {
            List<T> candidates = Logic.Agent.Instance.Content.Gets<T>(predicate);

            T result = null;
            int minDistance = int.MaxValue;

            foreach (var candidate in candidates)
            {
                if (candidate?.Map == null) continue;

                int distance = Get(character.Map, candidate.Map);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    result = candidate;
                }
            }

            return result;
        }
        public static Map Nearest(Character character, Func<Map, bool> predicate)
        {
            return Nearest(character.Map, predicate);
        }
        public static Map Nearest(Map source, Func<Map, bool> predicate)
        {
            List<Map> maps = Logic.Agent.Instance.Content.Gets<Map>(predicate);
            if (maps.Count == 0) return null;

            Map result = null;
            int minDistance = int.MaxValue;

            for (int i = 0; i < maps.Count; i++)
            {
                int distance = Get(source, maps[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    result = maps[i];
                }
            }

            return result;
        }

        public static int Get(Map start, Map destination)
        {
            if (start == null) return int.MaxValue;
            if (destination == null) return int.MaxValue;
            if (start == destination) return 0;
            
            // Handle Copy maps - use shortest path within Copy
            if (start.Copy != null && start.Copy == destination.Copy)
            {
                // Both maps in same Copy - use shortest path data
                if (start.Database.shortest == null) return int.MaxValue;
                if (!start.Database.shortest.TryGetValue(destination.Database.gid, out var paths)) return int.MaxValue;
                return paths.Count;
            }
            
            // Get scenes safely (Copy.Map.Parent is Copy, not Scene)
            var startScene = start.Parent as Scene;
            var destScene = destination.Parent as Scene;
            
            if (startScene == null) return int.MaxValue;
            if (destScene == null) return int.MaxValue;

            string key = CacheKey(start.Database.gid, destination.Database.gid);
            if (_cache.TryGetValue(key, out int cached))
            {
                return cached;
            }

            int distance;
            if (startScene == destScene)
            {
                if (start.Database.shortest == null)
                {
                    distance = int.MaxValue;
                }
                else if (!start.Database.shortest.TryGetValue(destination.Database.gid, out var paths))
                {
                    distance = int.MaxValue;
                }
                else
                {
                    distance = paths.Count;
                }
            }
            else
            {
                if (startScene.Database.shortest == null)
                {
                    distance = int.MaxValue;
                }
                else if (!startScene.Database.shortest.TryGetValue(destScene.Database.id, out var paths))
                {
                    distance = int.MaxValue;
                }
                else
                {
                    int result = 0;
                    Scene scene = startScene;
                    Map map = start;

                    foreach (var path in paths)
                    {
                        if (!Logic.Agent.Instance.Content.Has(s => s.Database.id == path, out Scene next))
                        {
                            distance = int.MaxValue;
                            goto CacheAndReturn;
                        }
                        if (!scene.Content.Has(m => m.Database.teleport != null && Agent.Teleportation(m.Database.teleport)?.Scene == next, out Map exit))
                        {
                            distance = int.MaxValue;
                            goto CacheAndReturn;
                        }
                        if (map.Database.shortest == null)
                        {
                            distance = int.MaxValue;
                            goto CacheAndReturn;
                        }
                        if (!map.Database.shortest.TryGetValue(exit.Database.gid, out var distanceToExit))
                        {
                            distance = int.MaxValue;
                            goto CacheAndReturn;
                        }
                        result += distanceToExit.Count;
                        Map teleportation = Agent.Teleportation(exit.Database.teleport);
                        if (teleportation == null)
                        {
                            distance = int.MaxValue;
                            goto CacheAndReturn;
                        }
                        result += 1;
                        scene = next;
                        map = teleportation;
                    }

                    if (map.Database.shortest == null)
                    {
                        distance = int.MaxValue;
                    }
                    else if (!map.Database.shortest.TryGetValue(destination.Database.gid, out var lastPath))
                    {
                        distance = int.MaxValue;
                    }
                    else
                    {
                        result += lastPath.Count;
                        distance = result;
                    }
                }
            }

            CacheAndReturn:
            _cache[key] = distance;
            return distance;
        }






    }
}
