using Logic;

namespace Domain.Exchange
{
    public static class Agent
    {
        public static void Init()
        {
            Pick.Init();
            Equip.Instance.Init();
            Sell.Init();
            Load.Instance.Init();
            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Item), OnAddItem);
            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Player), OnAddPlayer);
        }
        private static void OnPlayerAddWarehouse(params object[] args)
        {
            Logic.Warehouse warehouse = (Logic.Warehouse)args[1];
            warehouse.monitor.Register(Logic.Warehouse.Event.PlayerObtained, OnWarehousePlayerObtained);
        }
        private static void OnWarehousePlayerObtained(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[0];
            Logic.Warehouse warehouse = (Logic.Warehouse)args[1];
            Broadcast.Instance.Local(player, new object[] { Domain.Text.Agent.Instance.Id(Logic.Text.Labels.Acquire) }, ("warehouse", warehouse));
        }
        public static Item GetHandleItem(Life life, Func<Item, bool> predicate = null)
        {
            if (life == null) return null;
            if (life.Hand == null) return null;
            return life.Hand.Content.Get(predicate);
        }
        public static List<Item> GetItems(Life life, Func<Item, bool> predicate = null)
        {
            List<Item> items = new List<Item>();
            if (life == null) return items;
            Item handle = GetHandleItem(life, predicate);
            if (handle != null) items.Add(handle);
            List<Item> inBag = GetItemsInBag(life, predicate);
            items.AddRange(inBag);
            return items;
        }
        public static bool IsContainer(Item item) => item != null && item.Container.ContainsKey("Capacity") && item.Container.ContainsKey("Carry");
        public static List<Item> GetBags(Life target) => target == null ? new List<Item>() : GetEquipments(target).SelectMany(e => IsContainer(e) ? new[] { e } : e.Container.Count > 0 ? e.Content.Gets<Item>(i => IsContainer(i)) : Enumerable.Empty<Item>()).ToList();
        public static List<Item> GetItemsInBag(Life life, Func<Item, bool> predicate = null)
        {
            List<Item> items = new List<Item>();
            if (life == null) return items;
            foreach (Item bag in GetBags(life))
            {
                foreach (Item item in bag.Content.Gets<Item>(i => predicate == null || predicate(i)))
                {
                    items.Add(item);
                }
            }
            return items;
        }

        private static void OnAddItem(params object[] args)
        {
            Item item = (Item)args[1];
            if (IsContainer(item))
            {
                item.data.raw[Logic.Item.Data.Lock] = true;
            }
        }
        private static void OnAddPlayer(params object[] args)
        {
            Player player = (Player)args[1];
            player.Content.Add.Register(typeof(Logic.Warehouse), OnPlayerAddWarehouse);
        }
        public static Life Cargor(Life life)
        {
            if (life == null)
                return null;
            if (life.Map == null)
                return null;
            return life.Map.Content.Get<Life>(l => l.Bearer == life);
        }


        public static List<Item> GetEquipments(Life life)
        {
            if (life == null) return null;

            List<Item> results = new List<Item>();
            foreach (Part part in life.Content.Gets<Part>())
            {
                if (part.Content.Has<Item>())
                {
                    results.Add(part.Content.Get<Item>());
                }
            }
            return results;
        }


    }
}
