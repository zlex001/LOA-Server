using Data;


namespace Logic.Exchange
{
    public class Equip
    {
        private static Equip instance;
        public static Equip Instance { get { if (instance == null) { instance = new Equip(); } return instance; } }
        public void Init()
        {
            global::Data.Agent.Instance.Content.Add.Register(typeof(Life), OnAddLife);
        }
        public bool Can(Life sub, Item obj)
        {
            if (sub == null) return false;
            if (obj == null) return false;
            if (obj.Count <= 0) return false;
            if (obj.EquipPart == null) return false;
            if (!sub.Content.Has<Part>(p => p.Type == obj.EquipPart)) return false;
            if (!Agent.GetItems(sub).Contains(obj)) return false;
            // 已经装备在非 Hand 部位上的物品不能再"装备"
            if (obj.Parent is Part part && part.Type != Part.Types.Hand) return false;
            return true;
        }
        public void Do(Life life, Item item)
        {
            if (Can(life, item))
            {
                var partType = item.EquipPart ?? Part.Types.Hand;
                Part part = life.Content.Get<Part>(p => p.Type == partType);
                
                Item itemToEquip;
                if (item.Count > 1)
                {
                    itemToEquip = life.Hand.Create<Item>(item.Config, 1);
                    item.Count -= 1;
                }
                else
                {
                    itemToEquip = item;
                }
                
                Broadcast.Instance.Local(life, [Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.EquipUp)], ("sub", life), ("item", itemToEquip), ("part", (int)part.Type));
                part.AddAsParent(itemToEquip);
                var manualUnequipped = life.ManualUnequippedItems;
                if (manualUnequipped.Contains(itemToEquip.Config.Id))
                {
                    manualUnequipped.Remove(itemToEquip.Config.Id);
                    life.ManualUnequippedItems = manualUnequipped;
                }
            }
        }

        private void OnAddLife(params object[] args)
        {
            Life life = (Life)args[1];
            if (life is not Player)
            {
                foreach (var equipment in life.Config.equipments)
                {
                    Do(life, life.Hand.Load<global::Data.Config.Item, Item>(equipment, 1));
                }
            }
        }
    }
}