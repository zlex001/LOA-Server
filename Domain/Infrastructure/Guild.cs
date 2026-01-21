using Logic;

namespace Domain.Infrastructure
{

    public class Guild : Agent
    {
        public Guild(string owner, Map.Types type) : base(owner, type)
        {

        }
        private Item GetMoney(Logic.Map map)
        {
            if (map == null) return null;
            foreach (var box in GetBoxes(map))
            {
                foreach (Item item in box.Content.Gets<Item>())
                {
                    if (item.Config.Id == Logic.Constant.Money)
                    {
                        return item;
                    }
                }
            }
            return null;
        }
        private bool Deficit(Logic.Map map, int amount)
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
        public bool Fund(Logic.Map map, int amount)
        {
            if (map == null) return false;
            if (amount <= 0) return false;
            var container = GetBox(map, ContainerType.Miscellaneous);
            if (container == null) return false;
            if (container.Content.Has<Logic.Item>(i => i.Config.Id == Logic.Constant.Money, out Item money))
            {
                money.Count += amount;
            }
            else
            {
                container.Load<Logic.Config.Item, Logic.Item>(Logic.Constant.Money, amount);
            }
            return true;
        }
    }
}


























































