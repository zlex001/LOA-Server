using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Basic
{
    public class Graph : Manager
    {
        public class Node : Element
        {

            public enum Direction
            {
                East,
                South,
                West,
                North,
                Northeast,
                Southeast,
                Southwest,
                Northwest,
            }
            public int id;
            public int[] pos;
            public bool visited;
            public List<int> links = new List<int>();
            public Dictionary<int, int> path = new Dictionary<int, int>();
            public Dictionary<int, double> distance = new Dictionary<int, double>();
            public Dictionary<int, List<int>> shortest = new Dictionary<int, List<int>>();

            public int[] DirectionPos(Direction direction, int range = 1)
            {
                switch (direction)
                {
                    case Direction.East:
                        return new int[] { pos[0] + range, pos[1], pos[2] };
                    case Direction.South:
                        return new int[] { pos[0], pos[1] - range, pos[2] };
                    case Direction.West:
                        return new int[] { pos[0] - range, pos[1], pos[2] };
                    case Direction.North:
                        return new int[] { pos[0], pos[1] + range, pos[2] };
                    case Direction.Northeast:
                        return new int[] { pos[0] + range, pos[1] + range, pos[2] };
                    case Direction.Southeast:
                        return new int[] { pos[0] + range, pos[1] - range, pos[2] };
                    case Direction.Southwest:
                        return new int[] { pos[0] - range, pos[1] - range, pos[2] };
                    case Direction.Northwest:
                        return new int[] { pos[0] - range, pos[1] + range, pos[2] };
                    default:
                        return null;
                }
            }
        }
        public Graph() { }
        public Graph(IEnumerable<(int x, int y, int z)> position)
        {
            int id = 0;
            foreach (var (x, y, z) in position)
            {
                Add(new Node { id = id++, pos = [x, y, z] });
            }
        }

        public void Link()
        {
            foreach (Node node in Content.Gets<Node>())
            {
                node.links.Clear();

                foreach (int[] pos in Scale(node.pos, 1))
                {
                    var neighbors = Content.Gets<Node>(
                        n => n != node && n.pos[0] == pos[0] && n.pos[1] == pos[1]
                    );

                    node.links.AddRange(neighbors.Select(n => n.id));
                }
            }
        }

        public List<int[]> Scale(int[] pos, int range)
        {
            List<int[]> finals = new List<int[]>();

            for (int dx = -range; dx <= range; dx++)
            {
                for (int dy = -range; dy <= range; dy++)
                {
                    for (int dz = -range; dz <= range; dz++)
                    {
                        int[] newPos = { pos[0] + dx, pos[1] + dy, pos[2] + dz };
                        if (Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz) <= range)
                        {
                            finals.Add(newPos);
                        }
                    }
                }
            }
            return finals;
        }

        public void BreadthFirstSearch()
        {
            int total = Content.Count<Node>();
            int count = 0;

            foreach (Node node in Content.Gets<Node>())
            {
                count++;

                node.distance[node.id] = 0;
                List<Node> nodes = new List<Node> { node };

                while (nodes.Count > 0)
                {
                    Node point = nodes[0];
                    nodes.RemoveAt(0);

                    foreach (int area in point.links)
                    {
                        if (!node.distance.ContainsKey(area))
                        {
                            node.distance[area] = node.distance[point.id] + 1;
                            node.path[area] = point.id;
                            Node newNode = Content.Get<Node>(n => n.id == area);
                            nodes.Add(newNode);
                        }
                    }
                }

            }
        }


        public void Shortest()
        {
            int total = Content.Count<Node>();
            int count = 0;

            foreach (Node node in Content.Gets<Node>())
            {
                count++;

                foreach (Node start in Content.Gets<Node>())
                {
                    if (node.distance.ContainsKey(start.id) && node.distance[start.id] > 0)
                    {
                        node.shortest[start.id] = new List<int> { start.id };
                        Node path = Content.Get<Node>(n => n.id == node.path[start.id]);
                        while (node.path.ContainsKey(path.id))
                        {
                            node.shortest[start.id].Insert(0, path.id);
                            path = Content.Get<Node>(n => n.id == node.path[path.id]);
                        }
                    }
                }
            }
        }


        public void WeightPath()
        {
            foreach (Node node in Content.Gets<Node>())
            {
                node.distance[node.id] = 0;
                foreach (int a in node.links)
                {
                    Node approach = Content.Get<Node>(n => n.id == a);
                    double x = Math.Pow(node.pos[0] - approach.pos[0], 2);
                    double y = Math.Pow(node.pos[1] - approach.pos[1], 2);
                    double z = Math.Pow(node.pos[2] - approach.pos[2], 2);
                    double distance = Math.Sqrt(x + y + z);
                    node.distance[a] = distance;
                    node.path[a] = node.id;
                }
                Dijkstra(node);

            }
        }
        public void Dijkstra(Node node)
        {
            foreach (Node n in Content.Gets<Node>())
            {
                n.visited = false;
            }
            node.visited = true;
            int count = 0;
            while (true)
            {
                count++;
                double min = double.MaxValue;
                Node v = null;
                foreach (var d in node.distance)
                {
                    if (d.Value < min && Content.Has<Node>(n => n.id == d.Key && !n.visited))
                    {
                        min = d.Value;
                        v = Content.Get<Node>(n => n.id == d.Key && !n.visited);
                    }
                }
                if (v == null) { break; }
                v.visited = true;
                foreach (int a in v.links)
                {
                    Node w = Content.Get<Node>(n => n.id == a);
                    if (!w.visited)
                    {
                        double x = Math.Pow(v.pos[0] - w.pos[0], 2);
                        double y = Math.Pow(v.pos[1] - w.pos[1], 2);
                        double z = Math.Pow(v.pos[2] - w.pos[2], 2);
                        double distance = Math.Sqrt(x + y + z);

                        if (!node.distance.ContainsKey(a) || node.distance[a] > distance + node.distance[v.id])
                        {
                            node.distance[a] = distance + node.distance[v.id];
                            node.path[a] = v.id;
                        }
                    }
                }
            }

        }

    }
}
