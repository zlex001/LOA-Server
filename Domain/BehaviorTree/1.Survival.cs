using Logic;

namespace Domain.BehaviorTree
{
    /// <summary>
    /// 生存业务类别 - 对应001xxxx系列节点
    /// 包含背包管理、食物补给等生存相关的行为树节点
    /// </summary>
    public class Survival
    {
    /// <summary>
    /// Condition:0011001 - 背包是否中等程度负荷
    /// 检查背包容量或负重是否达到60%以上，任意一个满足即触发自动出售
    /// </summary>
    [BehaviorCondition(0011001)]
    public static bool IsInventoryModeratelyLoaded(Character character)
    {
        if (character is Life life)
        {
            var bags = Exchange.Agent.GetBags(life);
            if (bags.Count == 0)
            {
                return false;
            }

            foreach (var bag in bags)
            {
                if (bag.Container.TryGetValue("Carry", out int maxWeight) && maxWeight > 0)
                {
                    int currentWeight = Exchange.Load.GetContentWeight(bag);
                    float weightRatio = (float)currentWeight / maxWeight;
                    if (weightRatio >= 0.6f) return true;
                }

                if (bag.Container.TryGetValue("Capacity", out int maxCapacity) && maxCapacity > 0)
                {
                    int currentVolume = Exchange.Load.GetContentVolume(bag);
                    float volumeRatio = (float)currentVolume / maxCapacity;
                    if (volumeRatio >= 0.6f) return true;
                }
            }
        }
        return false;
    }

        /// <summary>
        /// Condition:0011002 - 食物是否存在
        /// 检查背包中是否有可以使用来恢复Lp的食物
        /// 用于判断是否需要补给食物
        /// </summary>
        [BehaviorCondition(0011002)]
        public static bool HasUsableFood(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            List<Item> items = Exchange.Agent.GetItems(life);
            if (items.Count == 0) return false;
            if (items.Count(i => i.Type == Item.Types.Food) == 0) return false;
            return true;
        }

        /// <summary>
        /// Condition:0011003 - 食物补给目标是否存在
        /// 动态查找食物补给目标（烹饪店店主）
        /// </summary>
        [BehaviorCondition(0011003)]
        public static bool HasFoodSupplyTarget(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            if (life.Birthplace == null) return false;
            if (life.Birthplace.Scene == null) return false;
            var maps = life.Birthplace.Scene.Content.Gets<Map>();
            return maps.Any(m => m.Content.Has<Life>(l => Infrastructure.Agent.Cook.IsValidOwner(l)));
        }

        /// <summary>
        /// Condition:0011004 - 食物补给目标是否相邻
        /// 动态查找并检查食物补给目标（烹饪店店主）是否在相邻位置
        /// </summary>
        [BehaviorCondition(0011004)]
        public static bool IsFoodSupplyTargetAdjacent(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            return life.Map.Content.Has<Life>(l => Infrastructure.Agent.Cook.IsValidOwner(l));
        }

        /// <summary>
        /// Condition:0011005 - 是否极大幅度饥饿
        /// 检查角色Lp是否低于20%，即饥饿程度达到80%以上
        /// </summary>
        [BehaviorCondition(0011005)]
        public static bool IsExtremelyHungry(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            return life.data.GetRatio<double>(Life.Data.Lp) < 0.2;
        }

        /// <summary>
        /// Condition:0011006 - 背包是否存在
        /// 检查角色是否装备了背包容器，用于判断基础设施是否完整
        /// </summary>
        [BehaviorCondition(0011006)]
        public static bool HasBackpack(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            var bags = Exchange.Agent.GetBags(life);
            return bags.Count > 0;
        }

        /// <summary>
        /// Condition:0011007 - 装备补给目标是否存在
        /// 动态查找装备补给目标（轻装店店主）
        /// </summary>
        [BehaviorCondition(0011007)]
        public static bool HasEquipmentSupplyTarget(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            if (life.Birthplace == null) return false;
            if (life.Birthplace.Scene == null) return false;
            
            var maps = life.Birthplace.Scene.Content.Gets<Map>();
            return maps.Any(m => m.Content.Has<Life>(l => Infrastructure.Agent.Sew.IsValidOwner(l)));
        }

        /// <summary>
        /// Condition:0011008 - 装备补给目标是否相邻
        /// 动态查找并检查装备补给目标（轻装店店主）是否在相邻位置
        /// </summary>
        [BehaviorCondition(0011008)]
        public static bool IsEquipmentSupplyTargetAdjacent(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            return life.Map.Content.Has<Life>(l => Infrastructure.Agent.Sew.IsValidOwner(l));
        }

        /// <summary>
        /// Action:0012001 - 补给食物
        /// NPC主动补给食物来维持生存，从烹饪店购买食物
        /// </summary>
        [BehaviorAction(0012001)]
        public static bool SupplyFood(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            Life obj = character.Map.Content.Get<Life>(l => Infrastructure.Agent.Cook.IsValidOwner(l));
            if (obj == null) return false;
            if (obj.Map == null) return false;
            List<Item> products = Infrastructure.Agent.Cook.GetGoods(obj.Map);
            if (products.Count == 0) return false;
            Exchange.Buy.Do(life, obj, products.FirstOrDefault(), 1);
            return true;
        }

        /// <summary>
        /// Action:0012002 - 自动寻路食物补给目标
        /// 动态查找食物补给目标（烹饪店店主）并自动寻路
        /// </summary>
        [BehaviorAction(0012002)]
        public static bool AutoPathToFoodSupplyTarget(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            if (life.Birthplace == null) return false;
            if (life.Birthplace.Scene == null) return false;
            var maps = life.Birthplace.Scene.Content.Gets<Map>();
            Life obj = maps.SelectMany(m => m.Content.Gets<Life>()).FirstOrDefault(l => Infrastructure.Agent.Cook.IsValidOwner(l));
            if(obj==null)return false;
            Domain.Move.Walk.FollowShortest(life, obj.Map);
            return true;
        }

        /// <summary>
        /// Action:0012003 - 补给装备
        /// NPC主动补给背包等关键装备来维持经济能力，从轻装店购买容器
        /// </summary>
        [BehaviorAction(0012003)]
        public static bool SupplyEquipment(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            
            Life obj = character.Map.Content.Get<Life>(l => Infrastructure.Agent.Sew.IsValidOwner(l));
            if (obj == null) return false;
            if (obj.Map == null) return false;
            List<Item> products = Infrastructure.Agent.Sew.GetGoods(obj.Map);
            if (products.Count == 0) return false;
            Item backpack = products.FirstOrDefault(p => Exchange.Agent.IsContainer(p));
            if (backpack != null)
            {
                Exchange.Buy.Do(life, obj, backpack, 1);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Action:0012004 - 自动寻路装备补给目标
        /// 动态查找装备补给目标（轻装店店主）并自动寻路
        /// </summary>
        [BehaviorAction(0012004)]
        public static bool AutoPathToEquipmentSupplyTarget(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            if (life.Birthplace == null) return false;
            if (life.Birthplace.Scene == null) return false;
            var maps = life.Birthplace.Scene.Content.Gets<Map>();
            Life obj = maps.SelectMany(m => m.Content.Gets<Life>()).FirstOrDefault(l => Infrastructure.Agent.Sew.IsValidOwner(l));
            if(obj==null)return false;
            Domain.Move.Walk.FollowShortest(life, obj.Map);
            return true;
        }
    }
}