using Data;


namespace Logic.Exchange
{
    public class Unequip
    {

        public static bool Can(Life sub, Item item)
        {
            if (sub == null) return false;
            if (item == null) return false;
            // 排除 Hand 部位的物品（手上物品不是装备，不能"卸下"）
            if (item.Parent is Part part && part.Type == Part.Types.Hand) return false;
            return Agent.GetEquipments(sub).Contains(item);
        }
        public static void Do(Life sub, Item item)
        {
            if (Can(sub, item))
            {
                Broadcast.Instance.Local(sub, [Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.EquipDown)], ("sub", sub), ("part", item.Parent), ("item", item));
                var manualUnequipped = sub.ManualUnequippedItems;
                if (!manualUnequipped.Contains(item.Config.Id))
                {
                    manualUnequipped.Add(item.Config.Id);
                    sub.ManualUnequippedItems = manualUnequipped;
                }
                Receive.Do(sub, item, item.Count);
            }
        }
    }
}