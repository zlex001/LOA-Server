using Data;
using Utils;

namespace Logic.Exchange
{
    public static class Sell
    {
        public static void Init()
        {
        }

        public static List<Item> GetItemRange(Life life) => new[] { Agent.GetHandleItem(life) }.Where(i => i != null).Concat(Agent.GetItemsInBag(life).Where(i => !i.Content.Has<Item>())).ToList();

        private static bool Can(Life sub, Life obj, Item item, int count)
        {
            if (sub == null) return false;
            if (obj == null) return false;
            if (!Infrastructure.Agent.Guild.IsOwner(obj)) return false;
            return GetItemRange(sub).Contains(item);
        }

        private static long ComputePrice(Item item, int count)
        {
            long price = 0;
            if (item != null && item.Config != null)
            {
                if (count > 0)
                {
                    long unit = item.Config.value;
                    if (unit < 0) unit = 0;
                    long p = unit * (long)count;
                    if (p >= 0) price = p; else price = long.MaxValue;
                }
                else
                {
                    price = 0;
                }
            }
            else
            {
                price = 0;
            }
            return price;
        }

        public static void Do(Life sub, Life obj, Item item, int count)
        {
            if (Can(sub, obj, item, count))
            {
                Broadcast.Instance.Local(obj, [Text.Agent.Instance.Id(global::Data.Text.Labels.Sell)], ("sub", sub), ("item", item), ("obj", obj), ("count", count.ToString()));
                Infrastructure.Agent.Guild.Pay(obj, sub, Utils.Mathematics.AsInt(ComputePrice(item, count)));
                Receive.Do(obj, item, count);
            }
            else
            {
                Broadcast.Instance.Local(obj,[Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.ReceiveFail)],("sub", obj),("item", item),("count", count.ToString()));
            }
        }
    }
}
