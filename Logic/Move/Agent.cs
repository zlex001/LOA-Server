using Data;
using System.Collections.Generic;

namespace Logic.Move
{
    public static class Agent
    {
        private static readonly Dictionary<string, Map> _teleportCache = new Dictionary<string, Map>();

        public static void Init()
        {
            Follow.Init();
            global::Data.Agent.Instance.Content.Add.Register(typeof(global::Data.Map), OnAddMap);
            global::Data.Agent.Instance.Content.Add.Register(typeof(global::Data.Player), OnAddPlayer);
            global::Data.Agent.Instance.Content.Add.Register(typeof(global::Data.Item), OnAddItem);
        }

        private static string PosKey(int[] pos)
        {
            if (pos == null || pos.Length < 3) return null;
            return $"{pos[0]}_{pos[1]}_{pos[2]}";
        }

        public static void Do(Character character, Map destination)
        {
            if (destination.Database.teleport == null)
            {
                destination.AddAsParent(character);
            }
            else
            {
                var teleportTarget = Teleportation(destination.Database.teleport);
                teleportTarget.AddAsParent(character);
            }
        }



        private static void OnAddMap(params object[] args)
        {
            global::Data.Map map = (global::Data.Map)args[1];
            map.Content.Add.Register(typeof(Character), OnMapAddCharacter);
            map.Content.Remove.Register(typeof(Character), OnMapRemoveCharacter);
            map.Content.Add.Register(typeof(Item), OnMapAddItem);
            map.Content.Remove.Register(typeof(Item), OnMapRemoveItem);

            string key = PosKey(map.Database.pos);
            if (key != null)
            {
                _teleportCache[key] = map;
            }
        }
        private static void OnAddPlayer(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[1];
            player.data.before.Register(Character.Data.Map, OnBeforePlayerMapChanged);
            player.data.after.Register(Character.Data.Map, OnAfterPlayerMapChanged);
        }

        private static bool Cross(global::Data.Map start, global::Data.Map destination)
        {
            return start != null && start.Scene != destination.Scene;
        }

        private static void OnBeforePlayerMapChanged(params object[] args)
        {
            global::Data.Map o = (global::Data.Map)args[0];
            global::Data.Map v = (global::Data.Map)args[1];
            global::Data.Player player = (global::Data.Player)args[2];
            if (Cross(o, v))
            {
                // 内联 CreateScene 函数
                var pos = v.Database.pos;
                var maps = new List<Net.Protocol.Map>();
                string sceneName = "";

                var scene = v?.Scene;
                if (scene != null)
                {
                    sceneName = Logic.Text.Agent.Instance.Get(scene.Config.Name, player);
                    
                    foreach (global::Data.Map m in scene.Content.Gets<global::Data.Map>(m => !(m.Copy != null)))
                    {
                        // 内联 CreateMap 函数
                        if (m != null)
                        {
                            var name = Logic.Text.Name.Map(m, player);
                            var mapPos = m.Database.pos;
                            var baseColor = Net.Protocol.MapColorHelper.GetMapTypeColor(m.Type);
                            var color = Lighting.Instance.ApplyWorldLighting(baseColor);
                            maps.Add(new Net.Protocol.Map(name, mapPos, color));
                        }
                    }
                }
                else
                {
                    int startX = v.Database.pos[0] - 1;
                    int endX = v.Database.pos[0] + 1;
                    int startY = v.Database.pos[1] - 1;
                    int endY = v.Database.pos[1] + 1;

                    for (int x = startX; x <= endX; x++)
                    {
                        for (int y = startY; y <= endY; y++)
                        {
                            string name = (x == v.Database.pos[0] && y == v.Database.pos[1]) ? (v.Scene != null ? Logic.Text.Agent.Instance.Get(v.Scene.Config.Name, player) : "") : " ";
                            var baseColor = Net.Protocol.MapColorHelper.GetMapTypeColor(global::Data.Map.Types.Default);
                            var color = Lighting.Instance.ApplyWorldLighting(baseColor);
                            maps.Add(new Net.Protocol.Map(name, new int[] { x, y, v.Database.pos[2] }, color));
                        }
                    }
                }

                Net.Tcp.Instance.Send(player, new Net.Protocol.Scene(pos, maps, sceneName));
            }
        }
        private static void OnAfterPlayerMapChanged(params object[] args)
        {
            global::Data.Map map = (global::Data.Map)args[0];
            global::Data.Player player = (global::Data.Player)args[1];
            Net.Tcp.Instance.Send(player, new Net.Protocol.Pos(map.Database.pos, Walk.Area(player)));
        }

        private static void UpdateCharacters(global::Data.Map map)
        {
            // Update players in the same map
            foreach (global::Data.Player player in map.Content.Gets<global::Data.Player>())
            {
                var data = Display.Agent.GetCharactersForDisplay(player);
                Net.Tcp.Instance.Send(player, new Net.Protocol.Characters(data));
            }
            
            // Update players who can see this map (within their view range)
            foreach (global::Data.Player player in global::Data.Agent.Instance.Content.Gets<global::Data.Player>())
            {
                if (player.Map == map) continue; // Already updated above
                if (player.Map == null) continue;
                if (Perception.Agent.Instance.IsVisible(player, map))
                {
                    var data = Display.Agent.GetCharactersForDisplay(player);
                    Net.Tcp.Instance.Send(player, new Net.Protocol.Characters(data));
                }
            }
        }
        /// <summary>
        /// 根据两个Map的坐标计算方向
        /// </summary>
        /// <param name="fromMap">起始地图</param>
        /// <param name="toMap">目标地图</param>
        /// <returns>方向枚举对应的文本标签</returns>
        public static global::Data.Text.Labels GetDirectionLabel(global::Data.Map fromMap, global::Data.Map toMap)
        {
            if (fromMap?.Database.pos == null || toMap?.Database.pos == null || fromMap.Database.pos.Length < 2 || toMap.Database.pos.Length < 2)
                return global::Data.Text.Labels.DirectionEast; // 默认返回东

            int dx = toMap.Database.pos[0] - fromMap.Database.pos[0];
            int dy = toMap.Database.pos[1] - fromMap.Database.pos[1];

            // 处理同一位置的情况
            if (dx == 0 && dy == 0)
                return global::Data.Text.Labels.DirectionEast; // 同一位置，默认返回东

            // 计算角度（以东为0度，逆时针为正）
            double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
            if (angle < 0) angle += 360;

            // 根据角度范围确定方向（南北就是上下）
            if (angle >= 337.5 || angle < 22.5)
                return global::Data.Text.Labels.DirectionEast;      // 东 (0°)
            else if (angle >= 22.5 && angle < 67.5)
                return global::Data.Text.Labels.DirectionNortheast; // 东北 (45°)
            else if (angle >= 67.5 && angle < 112.5)
                return global::Data.Text.Labels.DirectionNorth;     // 北 (90°) - 对应上
            else if (angle >= 112.5 && angle < 157.5)
                return global::Data.Text.Labels.DirectionNorthwest; // 西北 (135°)
            else if (angle >= 157.5 && angle < 202.5)
                return global::Data.Text.Labels.DirectionWest;      // 西 (180°)
            else if (angle >= 202.5 && angle < 247.5)
                return global::Data.Text.Labels.DirectionSouthwest; // 西南 (225°)
            else if (angle >= 247.5 && angle < 292.5)
                return global::Data.Text.Labels.DirectionSouth;     // 南 (270°) - 对应下
            else // angle >= 292.5 && angle < 337.5
                return global::Data.Text.Labels.DirectionSoutheast; // 东南 (315°)
        }

        private static void OnMapAddCharacter(params object[] args)
        {
            global::Data.Map map = (global::Data.Map)args[0];
            Character character = (Character)args[1];
            character.Map = map;
            UpdateCharacters(map);
            map.monitor.Fire(Map.Event.Arrived, character);
        }

        private static void OnMapRemoveCharacter(params object[] args)
        {
            global::Data.Map map = (global::Data.Map)args[0];
            Character character = (Character)args[1];
            UpdateCharacters(map);
        }


        private static void OnAddItem(params object[] args)
        {
            Item item = (Item)args[1];
            item.Content.Add.Register(typeof(Character), OnItemAddCharacter);
            item.Content.Remove.Register(typeof(Character), OnItemRemoveCharacter);
        }

        private static void OnMapAddItem(params object[] args)
        {
            global::Data.Map map = (global::Data.Map)args[0];
            Item item = (Item)args[1];
            item.monitor.Register(Item.Event.CountChangeComplete, OnMapItemCountChanged);
            UpdateCharacters(map);
        }

        private static void OnMapRemoveItem(params object[] args)
        {
            global::Data.Map map = (global::Data.Map)args[0];
            UpdateCharacters(map);
        }

        private static void OnMapItemCountChanged(params object[] args)
        {
            Item item = (Item)args[0];
            if (item.Parent is global::Data.Map map)
            {
                UpdateCharacters(map);
            }
        }

        private static void OnItemAddCharacter(params object[] args)
        {
            Item item = (Item)args[0];
            Character character = (Character)args[1];

            if (character is global::Data.Player player)
            {
                var data = GetCharactersInContainer(player, item);
                Net.Tcp.Instance.Send(player, new Net.Protocol.Characters(data));
            }
        }

        private static void OnItemRemoveCharacter(params object[] args)
        {
            Item item = (Item)args[0];
            Character character = (Character)args[1];

            if (character is global::Data.Player player && player.Map != null)
            {
                var data = Display.Agent.GetCharactersForDisplay(player);
                Net.Tcp.Instance.Send(player, new Net.Protocol.Characters(data));
            }
        }

        private static List<Net.Protocol.Characters.CharacterData> GetCharactersInContainer(global::Data.Player player, Item container)
        {
            var content = new List<Net.Protocol.Characters.CharacterData>();
            foreach (var c in container.Content.Gets<global::Data.Character>())
            {
                string name;
                int configId = 0;
                
                if (c is global::Data.Life life)
                {
                    name = Logic.Text.Decorate.Life(life, player);
                    configId = life.Config?.Id ?? 0;
                }
                else if (c is global::Data.Item item)
                {
                    name = Logic.Text.Decorate.Item(item, player);
                    configId = item.Config?.Id ?? 0;
                }
                else
                {
                    name = c.GetType().Name;
                }
                content.Add(new Net.Protocol.Characters.CharacterData(name, 0, c.GetHashCode(), configId));
            }
            return content;
        }
        public static Map Teleportation(int[] pos)
        {
            string key = PosKey(pos);
            if (key != null && _teleportCache.TryGetValue(key, out Map cached))
            {
                return cached;
            }
            return global::Data.Agent.Instance.Content.Get<Map>(m => m.Database.pos != null && pos != null && m.Database.pos.AsSpan().SequenceEqual(pos));
        }
        public static Map Teleportation(Character character, int[] pos)
        {
            // If character is in a Copy, search within the Copy first
            if (character.Map?.Copy != null)
            {
                var copyMap = character.Map.Copy.Content.Get<Map>(m => 
                    m.Database.pos != null && 
                    pos != null && 
                    m.Database.pos.AsSpan().SequenceEqual(pos));
                if (copyMap != null)
                {
                    return copyMap;
                }
            }
            
            // Search in Scene if character has a valid Scene
            var scene = character.Map?.Parent as Scene;
            if (scene != null)
            {
                var sceneMap = scene.Content.Get<Map>(m => 
                    m.Copy == null && 
                    m.Database.pos != null && 
                    pos != null && 
                    Enumerable.SequenceEqual(m.Database.pos, pos));
                if (sceneMap != null)
                {
                    return sceneMap;
                }
            }
            
            // Fallback to global search
            return Teleportation(pos);
        }
    }
}

