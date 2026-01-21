using Logic;

namespace Domain.Infrastructure
{

    public class Shop : Agent
    {
        public Shop(string owner, Map.Types type, params Item.Types[] products) : base(owner, type)
        {
            this.products = products;
        }
        private readonly Item.Types[] products;
        public bool IsValidOwner(Life life, System.Func<Life, bool> predicate = null)
        {
            if (!IsOwner(life, predicate)) return false;
            if (GetGoods(life.Map).Count == 0) return false;
            return true;
        }

        public List<Item> GetGoods(Map map)
        {
            var results = new List<Item>();
            if (map == null) return results;
            foreach (var box in GetBoxes(map))
            {
                foreach (Item item in box.Content.Gets<Item>())
                {
                    if (products.Contains(item.Type))
                    {
                        results.Add(item);
                    }
                }
            }
            return results;
        }





    }
}
