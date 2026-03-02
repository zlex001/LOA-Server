using Aop.Api.Domain;
using Data;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.X509;

namespace Logic.Exchange
{
    public static class Receive
    {
        private static void OnRemoveLife(params object[] args)
        {
            Life life = (Life)args[1];
        }
        public static void Do(Life sub, Life obj)
        {
            if (sub == null) return;
            if (obj == null) return;
            obj.Bearer = sub;
        }
        public static void Do(Life sub, Item obj, int count)
        {
            if (sub == null) return;
            if (obj == null) return;
            if (obj.Count <= 0) return;
            if (count <= 0) return;
            if (count > obj.Count) return;
            if (sub.Hand == null) return;
            
            if (count == obj.Count)
            {
                sub.Hand.AddAsParent(obj);
            }
            else
            {
                sub.Hand.Create<Item>(obj.Config, count);
                obj.Count -= count;
            }
            
            if (sub.Hand.Content.Count<Item>() > 1)
            {
                var items = sub.Hand.Content.Gets<Item>();
                var bags = Agent.GetBags(sub);
                for (int i = items.Count - 1; i > 0; i--)
                {
                    Item item = items[i];
                    bool putInBag = false;
                    foreach (var bag in bags)
                    {
                        if (Agent.IsContainer(bag))
                        {
                            Do(bag, item, item.Count);
                            if (item.Count == 0 || item.Parent == bag)
                            {
                                putInBag = true;
                                break;
                            }
                        }
                    }
                    if (!putInBag && item.Parent == sub.Hand)
                    {
                        Broadcast.Instance.Local(sub, [Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.Overhandle)], ("sub", sub), ("obj", item));
                        Drop.Do(sub, item, item.Count);
                    }
                }
            }
        }
        public static void Do(Item sub, Item obj, int count)
        {
            if (sub == null) return;
            if (obj == null) return;
            if (obj.Count <= 0) return;
            if (count <= 0) return;
            if (count > obj.Count) return;
            if (!Agent.IsContainer(sub)) return;
            if (obj.Content.Has<Item>())
            {
                sub.AddAsParent(obj);
            }
            else if (sub.Content.Has(i => i.Config.Id == obj.Config.Id, out Item exsit))
            {
                exsit.Count += count;
                obj.Count -= count;

            }
            else if (count == obj.Count)
            {
                sub.AddAsParent(obj);
            }
            else
            {
                sub.Create<Item>(obj.Config, count);
                obj.Count -= count;
            }
        }
        public static void Do(Item sub, Life obj)
        {
            if (sub == null) return;
            if (obj == null) return;
            sub.AddAsParent(obj);

        }

        public static void Do(Map sub, Item obj, int count)
        {
            if (sub == null) return;
            if (obj == null) return;
            if (obj.Count <= 0) return;
            if (count <= 0) return;
            if (count > obj.Count) return;
            if (obj.Content.Has<Item>())
            {
                sub.AddAsParent(obj);
            }
            else if (sub.Content.Has(i => i.Config.Id == obj.Config.Id, out Item exsit))
            {
                exsit.Count += count;
                obj.Count -= count;

            }
            else if (count == obj.Count)
            {
                sub.AddAsParent(obj);
            }
            else
            {
                sub.Create<Item>(obj.Config, count);
                obj.Count -= count;
            }
        }




    }
}
