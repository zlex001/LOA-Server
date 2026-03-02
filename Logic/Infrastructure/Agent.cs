using Logic.Cast;
using Data;

namespace Logic.Infrastructure
{
    public enum ContainerType
    {
        Material,
        Product,
        Miscellaneous
    }
    public class Agent
    {
        public Agent(string owner, Map.Types type)
        {
            this.owner = owner;
            this.type = type;
        }
        private readonly string owner;
        private readonly Map.Types type;
        private static readonly Dictionary<Map.Types, List<Map>> maps = new Dictionary<Map.Types, List<Map>>();
        private static readonly Dictionary<Map.Types, Agent> agents = new Dictionary<Map.Types, Agent>();
        public static void Init()
        {
            global::Data.Agent.Instance.Content.Add.Register(typeof(Map), OnAddMap);
            agents[Map.Types.Restaurant] = new Shop("Shopkeeper", Map.Types.Restaurant, Item.Types.Food);
            agents[Map.Types.LightGearShop] = new Shop("Shopkeeper", Map.Types.LightGearShop, Item.Types.Armor, Item.Types.Bag);
            agents[Map.Types.HeavyGearShop] = new Shop("Shopkeeper", Map.Types.HeavyGearShop, Item.Types.Weapon);
            agents[Map.Types.Guild] = new Guild("Merchant", Map.Types.Guild);
        }
        private static void OnAddMap(params object[] args)
        {
            Map map = (Map)args[1];
            if (map == null) return;
            if (!agents.ContainsKey(map.Type)) return;
            if (!maps.ContainsKey(map.Type))
            {
                maps[map.Type] = new List<Map>();
            }
            maps[map.Type].Add(map);
        }
        public static List<Map> GetMaps(Map.Types type)
        {
            return maps.TryGetValue(type, out List<Map> list) ? list : new List<Map>();
        }
        public static Map GetMap(Map.Types mapType)
        {
            var list = GetMaps(mapType);
            return list.Count > 0 ? list[0] : null;
        }
        public static Shop Cook => agents.TryGetValue(Map.Types.Restaurant, out Agent agent) ? agent as Shop : null;
        public static Shop Sew => agents.TryGetValue(Map.Types.LightGearShop, out Agent agent) ? agent as Shop : null;
        public static Shop Forge => agents.TryGetValue(Map.Types.HeavyGearShop, out Agent agent) ? agent as Shop : null;
        public static Guild Guild => agents.TryGetValue(Map.Types.Guild, out Agent agent) ? agent as Guild : null;
        public bool IsOwner(Life life, System.Func<Life, bool> predicate = null)
        {
            if (life == null) return false;
            if (life is Player) return false;
            if (life.Map == null) return false;
            if (life.Map.Type != type) return false;
            if (life.State.Is(global::Data.Life.States.Unconscious)) return false;
            if (!life.Config.Tags.Contains(owner)) return false;
            if (predicate != null && !predicate(life)) return false;
            return true;
        }

        public static List<Item> GetBoxes(Map map)
        {
            var boxes = new List<Item>();
            if (map == null) return boxes;
            foreach (Item item in map.Content.Gets<Item>())
            {
                if (item.Container.Count > 0)
                {
                    boxes.Add(item);
                }
            }
            return boxes;
        }
        public static Shop GetShop(Character character)
        {
            if (character == null) return null;
            if (character.Map == null) return null;
            return agents.TryGetValue(character.Map.Type, out Agent agent) ? agent as Shop : null;
        }
        public static Item GetBox(Map map, ContainerType containerType)
        {
            if (map == null) return null;
            switch (containerType)
            {
                case ContainerType.Material:
                    foreach (Item item in map.Content.Gets<Item>())
                    {
                        if (Cast.Agent.Instance.cook.IsPoint(item)) return item;
                        if (Cast.Agent.Instance.compound.IsPoint(item)) return item;
                        if (Cast.Agent.Instance.alchemize.IsPoint(item)) return item;
                        if (Cast.Agent.Instance.sew.IsPoint(item)) return item;
                        if (Cast.Agent.Instance.smith.IsPoint(item)) return item;
                    }
                    return null;
                case ContainerType.Product:
                    return map.Content.Get<Item>(i => i.Config.Id == Constant.ProductContainer);

                case ContainerType.Miscellaneous:
                    return map.Content.Get<Item>(i => i.Config.Id == Constant.MiscellaneousContainer);

                default:
                    return null;
            }
        }

    }
}
