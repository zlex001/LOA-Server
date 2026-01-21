using Logic;
using Utils;
using System.Linq;
using Newtonsoft.Json;

namespace Domain.Exchange
{
    public static class Drop
    {
        public static List<Item> GetItemRange(Life life)
        {
            List<Item> result = new List<Item>();
            if (Agent.GetHandleItem(life) != null)
            {
                result.Add(Agent.GetHandleItem(life));
            }
            foreach (Item equipment in Agent.GetEquipments(life))
            {
                result.Add(equipment);
            }
            foreach (Item bag in Agent.GetBags(life))
            {
                if (!result.Contains(bag))
                {
                    result.Add(bag);
                }
            }
            foreach (Item item in Agent.GetItemsInBag(life))
            {
                result.Add(item);
            }
            return result;
        }

        public static bool Can(Life life, Ability target)
        {
            if (target is Item item)
            {
                return Agent.GetItems(life).Contains(item);
            }
            
            if (target is Life targetLife)
            {
                return targetLife.Bearer == life;
            }
            
            return false;
        }
        public static bool Can(Life life, Item item, int count)
        {
            return GetItemRange(life).Contains(item) && item.Count >= count && count > 0 && life.Map != null;
        }

        public static void Do(Life sub, Item obj, int count)
        {
            Exchange.Receive.Do(sub.Map, obj, count);
            Broadcast.Instance.Local(sub, [Domain.Text.Agent.Instance.Id(Logic.Text.Labels.Drop)], ("sub", sub), ("obj", obj), ("count", count.ToString()));
        }
    }
}
