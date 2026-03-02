using System;
using System.Collections.Generic;
using System.Linq;
using Data.Config;
using Newtonsoft.Json;
using Utils;

namespace Data
{
    public class Maze : Scene
    {
        public Map Last { get; private set; }
        public Map Next { get; private set; }
        


        public override void Init(params object[] args)
        {
            var config = (Config.Maze)args[0];
            int[] exitPos = args.Length > 1 ? (int[])args[1] : null;
            int width = config.width;
            int height = config.height;
            float fillRate = config.fillRate;
            int iterations = config.iterations;

            int[,] grid = GenerateMaze(width, height, fillRate, iterations);
            var entrance = FindRandomEntrance(grid);
            var exit = FindFarthestPoint(grid, entrance);
            EnsureConnectivity(grid, entrance);

            var emptyTiles = new List<(int x, int y)>();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (grid[x, y] == 0) emptyTiles.Add((x, y));

            var fixedRoomsCfg = ParseFixedRoomsJson(config.fixedRooms ?? "");
            var roomPoolCfg = ParseRoomPoolJson(config.roomPool ?? "");

            bool canProceed = true;
            if ((roomPoolCfg == null || roomPoolCfg.Count == 0) &&
                (fixedRoomsCfg == null || SumValues(fixedRoomsCfg) < emptyTiles.Count))
                canProceed = false;

            var usedTiles = new HashSet<(int x, int y)>();
            var roomAssignments = new Dictionary<(int x, int y), int>();

            if (canProceed && fixedRoomsCfg != null && fixedRoomsCfg.Count > 0)
            {
                foreach (var kv in fixedRoomsCfg)
                {
                    int roomId = int.Parse(kv.Key);
                    int count = kv.Value;
                    for (int i = 0; i < count && usedTiles.Count < emptyTiles.Count; i++)
                    {
                        var tile = RandomUnusedTile(emptyTiles, usedTiles);
                        if (tile.Item1 < 0) i = count;
                        else
                        {
                            usedTiles.Add(tile);
                            roomAssignments[tile] = roomId;
                        }
                    }
                }
            }

            if (canProceed && roomPoolCfg != null && roomPoolCfg.Count > 0)
            {
                for (int i = 0; i < emptyTiles.Count; i++)
                {
                    var t = emptyTiles[i];
                    if (!usedTiles.Contains(t))
                        roomAssignments[t] = SelectRoomIdByWeight(roomPoolCfg);
                }
            }

            if (canProceed)
            {
                var (ex, ey) = entrance;
                var (ox, oy) = exit;

                foreach (var kv in roomAssignments)
                {
                    var tile = kv.Key;
                    int roomId = kv.Value;
                    int[] pos = new[] { tile.Item1, tile.Item2, 0 };
                    var map = Create<Map>(roomId, pos);

                    if (tile.Item1 == ex && tile.Item2 == ey) 
                    {
                        Last = map;
                        Last.Type = Map.Types.MazeEntrance;
                        if (exitPos != null)
                        {
                            Last.Database.teleport = exitPos;
                        }
                    }
                    else if (tile.Item1 == ox && tile.Item2 == oy) Next = map;
                }
            }
        }

        private static int SumValues(Dictionary<string, int> dict)
        {
            int sum = 0;
            if (dict != null)
            {
                foreach (var v in dict.Values) sum += v;
            }
            return sum;
        }

        private (int x, int y) RandomUnusedTile(List<(int x, int y)> pool, HashSet<(int x, int y)> used)
        {
            (int x, int y) result = (-1, -1);
            int remaining = pool.Count - used.Count;
            if (remaining > 0)
            {
                int attempts = 8;
                while (attempts-- > 0 && result.x < 0)
                {
                    var c = pool[Utils.Random.Instance.Next(pool.Count)];
                    if (!used.Contains(c)) result = c;
                }
                if (result.x < 0)
                {
                    for (int i = 0; i < pool.Count && result.x < 0; i++)
                        if (!used.Contains(pool[i])) result = pool[i];
                }
            }
            return result;
        }

        private Dictionary<string, int> ParseFixedRoomsJson(string json)
        {
            var result = new Dictionary<string, int>();
            if (!string.IsNullOrWhiteSpace(json))
            {
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
                if (parsed != null) result = parsed;
            }
            return result;
        }

        private Dictionary<string, float> ParseRoomPoolJson(string json)
        {
            var result = new Dictionary<string, float>();
            if (!string.IsNullOrWhiteSpace(json))
            {
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, float>>(json);
                if (parsed != null) result = parsed;
            }
            return result;
        }

        private int SelectRoomIdByWeight(Dictionary<string, float> roomPool)
        {
            if (roomPool == null || roomPool.Count == 0) throw new InvalidOperationException("房间池为空，无法选择房型");

            float total = 0f;
            foreach (var v in roomPool.Values) total += v;

            int selectedId = 0;
            bool assigned = false;

            if (total <= 0f)
            {
                foreach (var k in roomPool.Keys) { int.TryParse(k, out selectedId); assigned = true; break; }
            }
            else
            {
                float roll = (float)Utils.Random.Instance.NextDouble() * total;
                float acc = 0f;
                foreach (var kv in roomPool)
                {
                    acc += kv.Value;
                    if (roll <= acc && !assigned)
                    {
                        int.TryParse(kv.Key, out selectedId);
                        assigned = true;
                    }
                }
                if (!assigned)
                {
                    foreach (var k in roomPool.Keys) { int.TryParse(k, out selectedId); assigned = true; break; }
                }
            }
            return selectedId;
        }

        private int[,] GenerateMaze(int width, int height, float fillRate, int iterations)
        {
            if (width % 2 == 0) width++;
            if (height % 2 == 0) height++;

            var grid = new int[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    grid[x, y] = 1;

            int startX = Utils.Random.Instance.Next(0, width / 2) * 2 + 1;
            int startY = Utils.Random.Instance.Next(0, height / 2) * 2 + 1;

            CarvePassagesFrom(startX, startY, grid);

            for (int y = 0; y < height; y++)
            {
                grid[0, y] = 1;
                grid[width - 1, y] = 1;
            }
            for (int x = 0; x < width; x++)
            {
                grid[x, 0] = 1;
                grid[x, height - 1] = 1;
            }

            int openSpaces = (int)(width * height * (1 - fillRate) * 0.1);
            for (int i = 0; i < openSpaces; i++)
            {
                int x = Utils.Random.Instance.Next(1, width - 1);
                int y = Utils.Random.Instance.Next(1, height - 1);
                if (grid[x, y] == 1 && CountAdjacentWalls(x, y, grid) <= 5) grid[x, y] = 0;
            }

            return grid;
        }

        private void CarvePassagesFrom(int x, int y, int[,] grid)
        {
            grid[x, y] = 0;

            int[][] dirs = new int[][]
            {
                new[] {0, -2},
                new[] {2, 0},
                new[] {0, 2},
                new[] {-2, 0}
            };
            ShuffleArray(dirs);

            int w = grid.GetLength(0);
            int h = grid.GetLength(1);

            for (int i = 0; i < dirs.Length; i++)
            {
                int nx = x + dirs[i][0];
                int ny = y + dirs[i][1];
                if (nx > 0 && nx < w - 1 && ny > 0 && ny < h - 1 && grid[nx, ny] == 1)
                {
                    grid[x + dirs[i][0] / 2, y + dirs[i][1] / 2] = 0;
                    CarvePassagesFrom(nx, ny, grid);
                }
            }
        }

        private void ShuffleArray<T>(T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Utils.Random.Instance.Next(i + 1);
                T tmp = array[i];
                array[i] = array[j];
                array[j] = tmp;
            }
        }

        private int CountAdjacentWalls(int x, int y, int[,] grid)
        {
            int count = 0;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && nx < grid.GetLength(0) && ny >= 0 && ny < grid.GetLength(1) && grid[nx, ny] == 1)
                        count++;
                }
            return count;
        }

        private (int x, int y) FindRandomEntrance(int[,] grid)
        {
            var candidates = new List<(int x, int y)>();
            int w = grid.GetLength(0);
            int h = grid.GetLength(1);

            for (int x = 1; x < w - 1; x++)
            {
                if (grid[x, 1] == 0 && grid[x, 0] == 1) candidates.Add((x, 1));
                if (grid[x, h - 2] == 0 && grid[x, h - 1] == 1) candidates.Add((x, h - 2));
            }
            for (int y = 1; y < h - 1; y++)
            {
                if (grid[1, y] == 0 && grid[0, y] == 1) candidates.Add((1, y));
                if (grid[w - 2, y] == 0 && grid[w - 1, y] == 1) candidates.Add((w - 2, y));
            }

            if (candidates.Count == 0)
            {
                for (int y = 1; y < h - 1; y++)
                    for (int x = 1; x < w - 1; x++)
                        if (grid[x, y] == 0) candidates.Add((x, y));
                if (candidates.Count == 0)
                {
                    int cx = w / 2;
                    int cy = h / 2;
                    grid[cx, cy] = 0;
                    candidates.Add((cx, cy));
                }
            }

            return candidates[Utils.Random.Instance.Next(candidates.Count)];
        }

        private (int x, int y) FindFarthestPoint(int[,] grid, (int x, int y) entrance)
        {
            int w = grid.GetLength(0);
            int h = grid.GetLength(1);
            var distances = new int[w, h];
            var visited = new bool[w, h];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    distances[x, y] = int.MaxValue;

            var q = new Queue<(int x, int y, int d)>();
            q.Enqueue((entrance.x, entrance.y, 0));
            visited[entrance.x, entrance.y] = true;
            distances[entrance.x, entrance.y] = 0;

            while (q.Count > 0)
            {
                var t = q.Dequeue();
                int x = t.Item1, y = t.Item2, d = t.Item3;
                int nx, ny;

                nx = x; ny = y + 1;
                if (ny < h && grid[nx, ny] == 0 && !visited[nx, ny]) { visited[nx, ny] = true; distances[nx, ny] = d + 1; q.Enqueue((nx, ny, d + 1)); }
                nx = x + 1; ny = y;
                if (nx < w && grid[nx, ny] == 0 && !visited[nx, ny]) { visited[nx, ny] = true; distances[nx, ny] = d + 1; q.Enqueue((nx, ny, d + 1)); }
                nx = x; ny = y - 1;
                if (ny >= 0 && grid[nx, ny] == 0 && !visited[nx, ny]) { visited[nx, ny] = true; distances[nx, ny] = d + 1; q.Enqueue((nx, ny, d + 1)); }
                nx = x - 1; ny = y;
                if (nx >= 0 && grid[nx, ny] == 0 && !visited[nx, ny]) { visited[nx, ny] = true; distances[nx, ny] = d + 1; q.Enqueue((nx, ny, d + 1)); }
            }

            int best = -1;
            var far = entrance;
            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    if (grid[x, y] != 0) continue;
                    int d = distances[x, y];
                    if (d == int.MaxValue) continue;
                    bool nearEdge = (x == 1 || x == w - 2 || y == 1 || y == h - 2);
                    int score = d + (nearEdge ? 5 : 0);
                    if (score > best) { best = score; far = (x, y); }
                }
            return far;
        }

        private void EnsureConnectivity(int[,] grid, (int x, int y) entrance)
        {
            int w = grid.GetLength(0);
            int h = grid.GetLength(1);
            var visited = new bool[w, h];
            var q = new Queue<(int x, int y)>();

            grid[entrance.x, entrance.y] = 0;
            q.Enqueue(entrance);
            visited[entrance.x, entrance.y] = true;

            while (q.Count > 0)
            {
                var t = q.Dequeue();
                int x = t.Item1, y = t.Item2;

                int nx = x; int ny = y + 1;
                if (ny < h && grid[nx, ny] == 0 && !visited[nx, ny]) { visited[nx, ny] = true; q.Enqueue((nx, ny)); }
                nx = x + 1; ny = y;
                if (nx < w && grid[nx, ny] == 0 && !visited[nx, ny]) { visited[nx, ny] = true; q.Enqueue((nx, ny)); }
                nx = x; ny = y - 1;
                if (ny >= 0 && grid[nx, ny] == 0 && !visited[nx, ny]) { visited[nx, ny] = true; q.Enqueue((nx, ny)); }
                nx = x - 1; ny = y;
                if (nx >= 0 && grid[nx, ny] == 0 && !visited[nx, ny]) { visited[nx, ny] = true; q.Enqueue((nx, ny)); }
            }

            int isolated = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (grid[x, y] == 0 && !visited[x, y]) { grid[x, y] = 1; isolated++; }

            bool needConnect = isolated > w * h * 0.05;
            if (needConnect) ConnectIsolatedRegions(grid, visited);
        }

        private void ConnectIsolatedRegions(int[,] grid, bool[,] connected)
        {
            int w = grid.GetLength(0);
            int h = grid.GetLength(1);

            for (int attempt = 0; attempt < 10; attempt++)
            {
                var pool = new List<(int x, int y)>();
                for (int y = 1; y < h - 1; y++)
                    for (int x = 1; x < w - 1; x++)
                        if (grid[x, y] == 0 && connected[x, y]) pool.Add((x, y));

                if (pool.Count == 0) attempt = 10;
                else
                {
                    var start = pool[Utils.Random.Instance.Next(pool.Count)];
                    var potentials = new List<(int x, int y)>();

                    for (int dy = -2; dy <= 2; dy += 2)
                        for (int dx = -2; dx <= 2; dx += 2)
                        {
                            if (Math.Abs(dx) + Math.Abs(dy) != 2) continue;
                            int tx = start.x + dx;
                            int ty = start.y + dy;
                            if (tx >= 0 && tx < w && ty >= 0 && ty < h && grid[tx, ty] == 0 && !connected[tx, ty])
                            {
                                int wx = start.x + dx / 2;
                                int wy = start.y + dy / 2;
                                if (grid[wx, wy] == 1) potentials.Add((wx, wy));
                            }
                        }

                    if (potentials.Count > 0)
                    {
                        var c = potentials[Utils.Random.Instance.Next(potentials.Count)];
                        grid[c.x, c.y] = 0;
                        RecomputeConnectivity(grid, connected);
                    }
                }
            }
        }

        private void RecomputeConnectivity(int[,] grid, bool[,] connected)
        {
            int w = grid.GetLength(0);
            int h = grid.GetLength(1);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    connected[x, y] = false;

            (int x, int y) start = (-1, -1);
            bool found = false;
            for (int y = 0; y < h && !found; y++)
                for (int x = 0; x < w && !found; x++)
                    if (grid[x, y] == 0) { start = (x, y); found = true; }

            if (found)
            {
                var q = new Queue<(int x, int y)>();
                q.Enqueue(start);
                connected[start.x, start.y] = true;

                while (q.Count > 0)
                {
                    var t = q.Dequeue();
                    int x = t.Item1, y = t.Item2;

                    int nx = x; int ny = y + 1;
                    if (ny < h && grid[nx, ny] == 0 && !connected[nx, ny]) { connected[nx, ny] = true; q.Enqueue((nx, ny)); }
                    nx = x + 1; ny = y;
                    if (nx < w && grid[nx, ny] == 0 && !connected[nx, ny]) { connected[nx, ny] = true; q.Enqueue((nx, ny)); }
                    nx = x; ny = y - 1;
                    if (ny >= 0 && grid[nx, ny] == 0 && !connected[nx, ny]) { connected[nx, ny] = true; q.Enqueue((nx, ny)); }
                    nx = x - 1; ny = y;
                    if (nx >= 0 && grid[nx, ny] == 0 && !connected[nx, ny]) { connected[nx, ny] = true; q.Enqueue((nx, ny)); }
                }
            }
        }

        public override void Release()
        {
            base.Release();
        }
    }
}
