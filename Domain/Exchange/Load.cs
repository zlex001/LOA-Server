using Logic;

namespace Domain.Exchange
{
    public class Load : Domain.Agent<Load>
    {

        public void Init()
        {
            Logic.Agent.Instance.Content.Add.Register(typeof(Item), OnAddItem);
        }

        public static int GetContentWeight(Item item)
        {
            int result = 0;

            if (Agent.IsContainer(item))
            {
                foreach (var sub in item.Content.Gets<Item>())
                {
                    result += sub.Config.weight * sub.Count;

                    if (Agent.IsContainer(sub))
                    {
                        result += GetContentWeight(sub);
                    }
                }
            }
            return result;
        }

        public static int GetContentVolume(Item item)
        {
            int result = 0;

            if (Agent.IsContainer(item))
            {
                foreach (var sub in item.Content.Gets<Item>())
                {
                    result += sub.Config.volume * sub.Count;

                    if (Agent.IsContainer(sub))
                    {
                        result += GetContentVolume(sub);
                    }
                }
            }

            return result;
        }


        public static bool CheckOver(Item item)
        {
            bool overloaded = false;
            if (Agent.IsContainer(item))
            {
                if (item.Container.TryGetValue("Carry", out int carry) && carry > 0 && GetContentWeight(item) > carry)
                {
                    overloaded = true;
                }
                if (item.Container.TryGetValue("Capacity", out int capacity) && capacity > 0 && GetContentVolume(item) > capacity)
                {
                    overloaded = true;
                }
            }
            return overloaded;
        }



        private void Do(Item container)
        {

        }

        public void OnAddItem(params object[] args)
        {
            Item item = (Item)args[1];
            item.data.before.Register(Item.Data.Count, BeforeItemCountChanged);
            item.Content.Add.Register(typeof(Item), OnItemAddItem);
            item.Content.Remove.Register(typeof(Item), OnItemRemoveItem);
        }

        private void OnItemAddItem(params object[] args)
        {
            var container = (Item)args[0];
        }

        private void OnItemRemoveItem(params object[] args)
        {
            var container = (Item)args[0];

        }
        private void BeforeItemCountChanged(params object[] args)
        {
            int o = (int)args[0];
            int v = (int)args[1];
            if (v > o)
            {
                int d = v - o;
                Item item = (Item)args[2];
                var container = item.Parent as Item;
            }
        }

    }
}
