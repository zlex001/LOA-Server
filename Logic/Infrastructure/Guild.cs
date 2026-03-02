using Data;

namespace Logic.Infrastructure
{

    public class Guild : Agent
    {
        public Guild(string owner, Map.Types type) : base(owner, type)
        {

        }
        private Item GetMoney(global::Data.Map map)
        {
            if (map == null) return null;
            foreach (var box in GetBoxes(map))
            {
                foreach (Item item in box.Content.Gets<Item>())
                {
                    if (item.Config.Id == global::Data.Constant.Money)
                    {
                        return item;
                    }
                }
            }
            return null;
        }
        private bool Deficit(global::Data.Map map, int amount)
        {
            Item money = GetMoney(map);
            if (money == null) return true;
            return money.Count < amount;
        }
        public void Pay(Life sub, Life obj, int amount)
        {
            if (sub == null) return;
            if (obj == null) return;
            if (amount <= 0) return;
            if (sub.Map == null) return;
            if (Deficit(sub.Map, amount) && !Fund(sub.Map, amount)) return;
            Item money = GetMoney(sub.Map);
            Exchange.Pick.Do(sub, money, amount);
            Exchange.Give.Do(sub, obj, money, amount);
        }
        public bool Fund(global::Data.Map map, int amount)
        {
            if (map == null) return false;
            if (amount <= 0) return false;
            var container = GetBox(map, ContainerType.Miscellaneous);
            if (container == null) return false;
            if (container.Content.Has<global::Data.Item>(i => i.Config.Id == global::Data.Constant.Money, out Item money))
            {
                money.Count += amount;
            }
            else
            {
                container.Load<global::Data.Config.Item, global::Data.Item>(global::Data.Constant.Money, amount);
            }
            return true;
        }
    }
}


























































