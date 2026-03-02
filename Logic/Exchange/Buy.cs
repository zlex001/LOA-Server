using Data;

namespace Logic.Exchange
{
    public class Buy
    {

        private static bool Can(Life sub, Life obj, Item target, int count)
        {
            if (sub == null) return false;
            if (obj == null) return false;
            if (target == null) return false;
            if (count <= 0) return false;
            if (target.Count < count) return false;
            return true;
        }
        private static bool Afford(Life sub, int price, out Item money)
        {
            money = null;
            if (sub == null) return false;
            if (price <= 0) return false;
            if (sub is not Player) return true;
            foreach (Character character in Give.Availability(sub))
            {
                if (character is Item item && item.Config.Id == global::Data.Constant.Money)
                {
                    money = item;
                    break;
                }
            }
            if (money == null) return false;
            if (money.Count < price) return false;
            return true;
        }

        public static void Do(Life sub, Life obj, Item target, int count)
        {

            if (Can(sub, obj, target, count))
            {
                Broadcast.Instance.Local(obj, [Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.Buy, random: true)], ("sub", sub), ("item", target), ("count", count.ToString()));
                int price = Math.Max(1, target.Config.value * count);
                if (Afford(sub, price, out Item money))
                {
                    Give.Do(sub, obj, money, price);
                    Pick.Do(obj, target, count);
                    Item actualItem = Give.Availability(obj)
                        .OfType<Item>()
                        .FirstOrDefault(i => i.Config.Id == target.Config.Id);
                    if (actualItem != null)
                    {
                        Give.Do(obj, sub, actualItem, Math.Min(count, actualItem.Count));
                    }
                }
            }
        }

    }
}
