using Logic;
using System.Collections.Generic;

namespace Domain.Move
{
    public static class Agent
    {
        private static readonly Dictionary<string, Map> _teleportCache = new Dictionary<string, Map>();

        public static void Init()
        {
            Follow.Init();
            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Map), OnAddMap);
            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Player), OnAddPlayer);
            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Item), OnAddItem);
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
            Logic.Map map = (Logic.Map)args[1];
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
            Logic.Player player = (Logic.Player)args[1];
            player.data.before.Register(Character.Data.Map, OnBeforePlayerMapChanged);
            player.data.after.Register(Character.Data.Map, OnAfterPlayerMapChanged);
        }

        private static bool Cross(Logic.Map start, Logic.Map destination)
        {
            return start != null && start.Scene != destination.Scene;
        }

        private static void OnBeforePlayerMapChanged(params object[] args)
        {
            Logic.Map o = (Logic.Map)args[0];
            Logic.Map v = (Logic.Map)args[1];
            Logic.Player player = (Logic.Player)args[2];
            if (Cross(o, v))
            {
                // 内联 CreateScene 函数
                var pos = v.Database.pos;
                var maps = new List<Net.Protocol.Map>();
                string sceneName = "";

                var scene = v?.Scene;
                if (scene != null)
                {
                    sceneName = Domain.Text.Agent.Instance.Get(scene.Config.Name, player);
                    
                    foreach (Logic.Map m in scene.Content.Gets<Logic.Map>(m => !(m.Copy != null)))
                    {
                        // 内联 CreateMap 函数
                        if (m != null)
                        {
                            var name = Domain.Text.Name.Map(m, player);
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
                            string name = (x == v.Database.pos[0] && y == v.Database.pos[1]) ? (v.Scene != null ? Domain.Text.Agent.Instance.Get(v.Scene.Config.Name, player) : "") : " ";
                            var baseColor = Net.Protocol.MapColorHelper.GetMapTypeColor(Logic.Map.Types.Default);
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
            Logic.Map map = (Logic.Map)args[0];
            Logic.Player player = (Logic.Player)args[1];
            Net.Tcp.Instance.Send(player, new Net.Protocol.Pos(map.Database.pos, Walk.Area(player)));
        }

        private static void UpdateCharacters(Logic.Map map)
        {
            // Update players in the same map
            foreach (Logic.Player player in map.Content.Gets<Logic.Player>())
            {
                var data = Display.Agent.GetCharactersForDisplay(player);
                Net.Tcp.Instance.Send(player, new Net.Protocol.Characters(data));
            }
            
            // Update players who can see this map (within their view range)
            foreach (Logic.Player player in Logic.Agent.Instance.Content.Gets<Logic.Player>())
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
        public static Logic.Text.Labels GetDirectionLabel(Logic.Map fromMap, Logic.Map toMap)
        {
            if (fromMap?.Database.pos == null || toMap?.Database.pos == null || fromMap.Database.pos.Length < 2 || toMap.Database.pos.Length < 2)
                return Logic.Text.Labels.DirectionEast; // 默认返回东

            int dx = toMap.Database.pos[0] - fromMap.Database.pos[0];
            int dy = toMap.Database.pos[1] - fromMap.Database.pos[1];

            // 处理同一位置的情况
            if (dx == 0 && dy == 0)
                return Logic.Text.Labels.DirectionEast; // 同一位置，默认返回东

            // 计算角度（以东为0度，逆时针为正）
            double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
            if (angle < 0) angle += 360;

            // 根据角度范围确定方向（南北就是上下）
            if (angle >= 337.5 || angle < 22.5)
                return Logic.Text.Labels.DirectionEast;      // 东 (0°)
            else if (angle >= 22.5 && angle < 67.5)
                return Logic.Text.Labels.DirectionNortheast; // 东北 (45°)
            else if (angle >= 67.5 && angle < 112.5)
                return Logic.Text.Labels.DirectionNorth;     // 北 (90°) - 对应上
            else if (angle >= 112.5 && angle < 157.5)
                return Logic.Text.Labels.DirectionNorthwest; // 西北 (135°)
            else if (angle >= 157.5 && angle < 202.5)
                return Logic.Text.Labels.DirectionWest;      // 西 (180°)
            else if (angle >= 202.5 && angle < 247.5)
                return Logic.Text.Labels.DirectionSouthwest; // 西南 (225°)
            else if (angle >= 247.5 && angle < 292.5)
                return Logic.Text.Labels.DirectionSouth;     // 南 (270°) - 对应下
            else // angle >= 292.5 && angle < 337.5
                return Logic.Text.Labels.DirectionSoutheast; // 东南 (315°)
        }

        private static void OnMapAddCharacter(params object[] args)
        {
            Logic.Map map = (Logic.Map)args[0];
            Character character = (Character)args[1];
            character.Map = map;
            UpdateCharacters(map);
            map.monitor.Fire(Map.Event.Arrived, character);
        }

        private static void OnMapRemoveCharacter(params object[] args)
        {
            Logic.Map map = (Logic.Map)args[0];
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
            Logic.Map map = (Logic.Map)args[0];
            Item item = (Item)args[1];
            item.monitor.Register(Item.Event.CountChangeComplete, OnMapItemCountChanged);
            UpdateCharacters(map);
        }

        private static void OnMapRemoveItem(params object[] args)
        {
            Logic.Map map = (Logic.Map)args[0];
            UpdateCharacters(map);
        }

        private static void OnMapItemCountChanged(params object[] args)
        {
            Item item = (Item)args[0];
            if (item.Parent is Logic.Map map)
            {
                UpdateCharacters(map);
            }
        }

        private static void OnItemAddCharacter(params object[] args)
        {
            Item item = (Item)args[0];
            Character character = (Character)args[1];

            if (character is Logic.Player player)
            {
                var data = GetCharactersInContainer(player, item);
                Net.Tcp.Instance.Send(player, new Net.Protocol.Characters(data));
            }
        }

        private static void OnItemRemoveCharacter(params object[] args)
        {
            Item item = (Item)args[0];
            Character character = (Character)args[1];

            if (character is Logic.Player player && player.Map != null)
            {
                var data = Display.Agent.GetCharactersForDisplay(player);
                Net.Tcp.Instance.Send(player, new Net.Protocol.Characters(data));
            }
        }

        private static List<Net.Protocol.Characters.CharacterData> GetCharactersInContainer(Logic.Player player, Item container)
        {
            var content = new List<Net.Protocol.Characters.CharacterData>();
            foreach (var c in container.Content.Gets<Logic.Character>())
            {
                string name;
                if (c is Logic.Life life)
                {
                    name = Domain.Text.Decorate.Life(life, player);
                }
                else if (c is Logic.Item item)
                {
                    name = Domain.Text.Decorate.Item(item, player);
                }
                else
                {
                    name = c.GetType().Name;
                }
                content.Add(new Net.Protocol.Characters.CharacterData(name, 0, c.GetHashCode()));
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
            return Logic.Agent.Instance.Content.Get<Map>(m => m.Database.pos != null && pos != null && m.Database.pos.AsSpan().SequenceEqual(pos));
        }
        public static Map Teleportation(Character character, int[] pos)
        {
            if (character.Map.Scene.Content.Has<Map>(m => m.Copy == character.Map.Copy && Enumerable.SequenceEqual(m.Database.pos, pos)))
            {
                return character.Map.Scene.Content.Get<Map>(m => Enumerable.SequenceEqual(m.Database.pos, pos));
            }
            else
            {
                return Teleportation(pos);
            }
        }
    }
}

