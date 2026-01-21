using Basic;
using System.IO;
using System.Collections.Generic;

namespace Logic.Design
{
    public class World
    {
        private static readonly (int, int)[] Directions = { (1, 0), (-1, 0), (0, 1), (0, -1) };
        private static readonly string Path = System.IO.Path.Combine(Utils.Paths.DesignData, "地图坐标.csv");
        private static readonly string SceneMapPath = System.IO.Path.Combine(Utils.Paths.DesignData, "场景坐标.csv");
        private static readonly string OutputPath = System.IO.Path.Combine(Utils.Paths.Config, "Shortest.bin");
        private static bool IsChanged => Math.Abs(Utils.FileManager.CompareModificationTime(Path, OutputPath).TotalSeconds) > 1;
        
        public static List<(string sceneCid, int[] pos)> SceneCoordinates { get; private set; } = new List<(string, int[])>();
        private static List<List<object>> Load()
        {
            var cells = Utils.Csv.LoadAsCells(Path);
            for (int y = 0; y < cells.Count; y++)
            {
                for (int x = 0; x < cells[y].Count; x++)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(cells[y][x])))
                    {
                        string cellValue = Convert.ToString(cells[y][x]);
                        var map = Agent.Instance.Content.Get<Map>(s => s.cid == cellValue);
                        if (map != null)
                        {
                            cells[y][x] = map.id;
                        }
                        else
                        {
                            cells[y][x] = 0;
                        }
                    }
                }
            }
            return cells;
        }
        public static void Init()
        {
            LoadSceneCoordinates();
            
            if (IsChanged)
            {
                Utils.Debug.Log.Info("DESIGN", "World table changed, recalculating graph data...");
                try
                {
                    var cells = Load();
                    var (mapMatrix, height, width) = MapMatrix(cells);
                    var (sceneMatrix, sceneDictionary) = SceneMatrix(mapMatrix, height, width);
                    var sceneExit = SceneExit(mapMatrix, sceneMatrix, height, width);
                    var scenes = Build(mapMatrix, sceneDictionary, sceneExit);
                    SetMapTeleport(mapMatrix, sceneMatrix, sceneDictionary, scenes, sceneExit, height, width);
                    Utils.Binary.Serialize(scenes, OutputPath, Utils.SerializeFormat.Binary);
                    
                    Utils.FileManager.Instance.SyncFilesLastModifiedTime(Path, OutputPath);
                    Utils.Debug.Log.Info("DESIGN", "World data generation completed");
                }
                catch (Exception ex)
                {
                    if (System.IO.File.Exists(OutputPath))
                    {
                        System.IO.File.Delete(OutputPath);
                        Utils.Debug.Log.Warning("DESIGN", "World data generation failed, corrupted Shortest.bin deleted");
                    }
                    Utils.Debug.Log.Error("DESIGN", $"World data generation error: {ex.Message}");
                    throw;
                }
            }
            else
            {
                Utils.Debug.Log.Info("DESIGN", "World table unchanged, loading from cache file");
            }
        }

        private static void LoadSceneCoordinates()
        {
            SceneCoordinates.Clear();
            
            try
            {
                if (!System.IO.File.Exists(SceneMapPath))
                {
                    Utils.Debug.Log.Warning("DESIGN", "SceneMap CSV file not found");
                    return;
                }

                var cells = Utils.Csv.LoadAsCells(SceneMapPath);
                if (cells == null || cells.Count == 0)
                {
                    Utils.Debug.Log.Warning("DESIGN", "SceneMap CSV file is empty");
                    return;
                }

                int height = cells.Count;
                for (int y = 0; y < height; y++)
                {
                    int flip = height - 1 - y;
                    if (cells[y] == null) continue;
                    
                    for (int x = 0; x < cells[y].Count; x++)
                    {
                        var cellValue = Convert.ToString(cells[y][x]);
                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            SceneCoordinates.Add((cellValue, new int[] { x, flip, 0 }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Warning("DESIGN", $"Failed to load SceneMap CSV: {ex.Message}");
            }
        }


        private static (int?[,] cotent, int height, int width) MapMatrix(List<List<object>> cells)
        {
            int height = cells.Count;
            int width = cells.Max(r => r.Count);
            var cotent = new int?[height, width];

            for (int y = 0; y < height; y++)
            {
                int flip = height - 1 - y;
                for (int x = 0; x < cells[y].Count; x++)
                {
                    var cell = cells[y][x];
                    if (cell != null && int.TryParse(cell.ToString(), out int id))
                    {
                        cotent[flip, x] = id;
                    }
                }
            }

            return (cotent, height, width);
        }

        private static (int?[,] scenes, Dictionary<int, List<(int, int)>> dic) SceneMatrix(int?[,] mapMatrix, int height, int width)
        {
            var scenes = new int?[height, width];
            var dictionary = new Dictionary<int, List<(int, int)>>();

            void Fill(int x, int y, int id)
            {
                var queue = new Queue<(int, int)>();
                queue.Enqueue((x, y));
                while (queue.Count > 0)
                {
                    var (cx, cy) = queue.Dequeue();
                    if (cx < 0 || cy < 0 || cx >= width || cy >= height) continue;
                    if (mapMatrix[cy, cx] == null || scenes[cy, cx] != null) continue;

                    scenes[cy, cx] = id;
                    dictionary[id].Add((cx, cy));
                    foreach (var (dx, dy) in Directions)
                    {
                        queue.Enqueue((cx + dx, cy + dy));

                    }
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (mapMatrix[y, x] is int mapId && scenes[y, x] == null)
                    {
                        var map = Agent.Instance.Content.Get<Logic.Design.Map>(m => m.id == mapId);
                        var config = Design.Agent.Instance.Content.Get<Logic.Design.Scene>(m => m.cid == map.cid.Split('-')[0]);
                        int id = config.id;
                        if (!dictionary.ContainsKey(id)) dictionary[id] = [];
                        Fill(x, y, id);
                    }
                }
            }


            return (scenes, dictionary);
        }

        private static Dictionary<int, List<int>> SceneExit(int?[,] mapMatrix, int?[,] scene, int height, int width)
        {
            var exits = new Dictionary<int, List<int>>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (mapMatrix[y, x] == null)
                    {
                        var neighbors = new HashSet<int>();
                        foreach (var (dx, dy) in Directions)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                            if (scene[ny, nx] is int id) neighbors.Add(id);
                        }

                        if (neighbors.Count == 2)
                        {
                            var ids = neighbors.ToArray();
                            void Link(int a, int b)
                            {
                                if (!exits.ContainsKey(a)) exits[a] = new();
                                if (!exits[a].Contains(b)) exits[a].Add(b);
                            }
                            Link(ids[0], ids[1]);
                            Link(ids[1], ids[0]);
                        }
                    }
                }
            }
            return exits;
        }

        private static List<Database.Scene> Build(int?[,] mapMatrix, Dictionary<int, List<(int, int)>> dictionary, Dictionary<int, List<int>> exit)
        {
            var result = new List<Database.Scene>();
            var graphFprScene = new Basic.Graph();
            int globalGid = 0;
            foreach (var (id, content) in dictionary)
            {
                var graphForMaps = new Graph(content.Select(p => (p.Item1, p.Item2, 0)));
                graphForMaps.Link();
                graphForMaps.BreadthFirstSearch();
                graphForMaps.Shortest();
                var scene = new Database.Scene(id, exit.TryGetValue(id, out var exitList) ? exitList : new List<int>());
                var localToGlobalGid = new Dictionary<int, int>();
                foreach (var (x, y) in content)
                {
                    var nodeForMap = graphForMaps.Content.Get<Graph.Node>(n => n.pos[0] == x && n.pos[1] == y);
                    localToGlobalGid[nodeForMap.id] = globalGid;
                    var convertedShortest = new Dictionary<int, List<int>>();
                    scene.maps.Add(new Database.Map(mapMatrix[y, x].Value, globalGid++, [x, y, 0], convertedShortest));
                }
                foreach (var map in scene.maps)
                {
                    var nodeForMap = graphForMaps.Content.Get<Graph.Node>(n => n.pos[0] == map.pos[0] && n.pos[1] == map.pos[1]);
                    foreach (var (targetLocalId, path) in nodeForMap.shortest)
                    {
                        if (localToGlobalGid.TryGetValue(targetLocalId, out int targetGlobalGid))
                        {
                            var convertedPath = path.Select(localId => localToGlobalGid.TryGetValue(localId, out int gid) ? gid : localId).ToList();
                            map.shortest[targetGlobalGid] = convertedPath;
                        }
                    }
                }
                var nodeForScene = new Basic.Graph.Node { id = scene.id };
                nodeForScene.links = exit.TryGetValue(id, out var sceneExitList) ? sceneExitList : new List<int>();
                graphFprScene.Add(nodeForScene);
                result.Add(scene);
            }
            graphFprScene.BreadthFirstSearch();
            graphFprScene.Shortest();
            foreach (var scene in result)
            {
                var node = graphFprScene.Content.Get<Basic.Graph.Node>(n => n.id == scene.id);
                scene.shortest = node.shortest;
            }
            return result;
        }

        private static void SetMapTeleport(int?[,] mapMatrix, int?[,] sceneMatrix, Dictionary<int, List<(int, int)>> sceneDictionary, List<Database.Scene> scenes, Dictionary<int, List<int>> sceneExit, int height, int width)
        {
            foreach (var (id, exit) in sceneExit)
            {
                foreach (var e in exit)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            if (mapMatrix[y, x] != null) continue;

                            var neighbor = new List<(int x, int y, int id)>();
                            foreach (var (dx, dy) in Directions)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                                if (sceneMatrix[ny, nx] is int nid && (nid == id || nid == e))
                                {
                                    neighbor.Add((nx, ny, nid));
                                }
                            }

                            if (neighbor.Any(n => n.id == id) && neighbor.Any(n => n.id == e))
                            {
                                var a = neighbor.First(n => n.id == id);
                                var b = neighbor.First(n => n.id == e);

                                static int[]? NeighborPos((int x, int y) pos, List<(int x, int y)> set)
                                {
                                    foreach (var (dx, dy) in Directions)
                                    {
                                        int tx = pos.x + dx, ty = pos.y + dy;
                                        if (set.Contains((tx, ty))) return new[] { tx, ty, 0 };
                                    }
                                    return null;
                                }

                                var sceneA = scenes.First(s => s.id == id);
                                var sceneB = scenes.First(s => s.id == e);

                                var mapA = sceneA.maps.FirstOrDefault(m => m.pos[0] == a.x && m.pos[1] == a.y);
                                var mapB = sceneB.maps.FirstOrDefault(m => m.pos[0] == b.x && m.pos[1] == b.y);

                                var toB = NeighborPos((b.x, b.y), sceneDictionary[e]);
                                var toA = NeighborPos((a.x, a.y), sceneDictionary[id]);

                                if (mapA != null && toB != null) mapA.teleport = toB;
                                if (mapB != null && toA != null) mapB.teleport = toA;
                            }
                        }
                    }

                }

            }

        }

    }
}