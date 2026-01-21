using Domain.Infrastructure;
using Logic;

namespace Domain.BehaviorTree
{
    /// <summary>
    /// 经济业务类别 - 对应002xxxx系列节点
    /// 包含出售、补给、店铺管理等经济相关的行为树节点
    /// </summary>
    public class Economy
    {
        public static bool IsMaterial(Character character)
        {
            var item = character as Logic.Item;
            if (item == null) return false;
            return item.Type == Item.Types.Material;
        }
        public static bool IsProduct(Character character)
        {
            var item = character as Logic.Item;
            if (item == null) return false;
            return item.Type == Item.Types.Food || item.Type == Item.Types.Beverage || item.Type == Item.Types.Medicine || item.Type == Item.Types.Weapon || item.Type == Item.Types.Armor || item.Type == Item.Types.Bag;
        }
        private static bool IsGarbage(Character character)
        {
            var item = character as Logic.Item;
            if (item == null) return false;
            if (item.Config.Id == Logic.Constant.Money) return false;
            if (item.Type == Item.Types.Food) return false;
            return true;
        }
        private static bool IsMiscellaneous(Character character)
        {
            var item = character as Logic.Item;
            if (item == null) return false;
            if (IsMaterial(character)) return false;
            if (IsProduct(character)) return false;
            return true;
        }
        /// <summary>
        /// Condition:0021001 - 清仓对象是否存在
        /// 动态查找清仓对象
        /// </summary>
        [BehaviorCondition(0021001)]
        public static bool HasClearanceObject(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            if (life.Birthplace == null) return false;
            if (life.Birthplace.Scene == null) return false;
            foreach (Map map in Infrastructure.Agent.GetMaps(Map.Types.Guild))
            {
                if (map.Scene == life.Birthplace.Scene)
                {
                    if (map.Content.Has<Life>(l => Infrastructure.Agent.Guild.IsOwner(l)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Condition:0021002 - 清仓对象是否相邻
        /// 动态查找并检查清仓对象是否在相邻位置
        /// </summary>
        [BehaviorCondition(0021002)]
        public static bool IsClearanceObjectAdjacent(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            return life.Map.Content.Has<Life>(l => Infrastructure.Agent.Guild.IsOwner(l));
        }

        /// <summary>
        /// Condition:0021004 - 是否位于店铺
        /// 检查角色是否在任何类型的店铺内
        /// </summary>
        [BehaviorCondition(0021004)]
        public static bool IsInShop(Character character)
        {
            return character.Map?.Type == Map.Types.Restaurant;
        }

        /// <summary>
        /// Condition:0021007 - 清仓目标是否存在
        /// 动态查找清仓目标（店主或商贩）
        /// </summary>
        [BehaviorCondition(0021007)]
        public static bool HasClearanceTarget(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            List<Item> garbage = Domain.Exchange.Agent.GetItemsInBag(life, IsGarbage);
            return garbage.Count > 0;
        }

        [BehaviorCondition(0021008)]
        public static bool HasMaterialContainer(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            Item box = Infrastructure.Agent.GetBox(life.Map, Infrastructure.ContainerType.Material);
            if (box == null) return false;
            return true;
        }

        [BehaviorCondition(0021009)]
        public static bool HasMaterialInBag(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            foreach (Character c in Exchange.Give.Availability(life))
            {
                if (IsMaterial(c))
                {
                    return true;
                }
            }
            return false;
        }

        [BehaviorCondition(0021010)]
        public static bool HasProductContainer(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            Item box = Infrastructure.Agent.GetBox(life.Map, Infrastructure.ContainerType.Product);
            if (box == null) return false;
            return true;
        }

        [BehaviorCondition(0021011)]
        public static bool HasProductInBag(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            List<Character> characters = Domain.Exchange.Give.Availability(life);
            foreach (Character c in characters)
            {
                if (IsProduct(c))
                {
                    return true;
                }
            }
            return false;
        }

        [BehaviorCondition(0021014)]
        public static bool HasMiscellaneousContainer(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            Item box = Infrastructure.Agent.GetBox(life.Map, Infrastructure.ContainerType.Miscellaneous);
            if (box == null) return false;
            return true;
        }

        [BehaviorCondition(0021015)]
        public static bool HasMiscellaneousInBag(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            foreach (Character c in Exchange.Give.Availability(life))
            {
                if (IsMiscellaneous(c))
                {
                    return true;
                }
            }
            return false;
        }

        [BehaviorCondition(0021016)]
        public static bool HasFoodInShop(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            foreach (Item box in Infrastructure.Agent.GetBoxes(life.Map))
            {
                foreach (Item item in box.Content.Gets<Item>())
                {
                    if (item.Type == Item.Types.Food)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Action:0022001 - 清仓
        /// 向指定店主清仓物品，排除货币和食物，确保不会清仓生存必需品
        /// </summary>
        [BehaviorAction(0022001)]
        public static bool ClearanceToMerchant(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            var owner = life.Map.Content.Get<Life>(l => Domain.Infrastructure.Agent.Guild.IsOwner(l));
            if (owner == null) return false;
            List<Item> garbage = Domain.Exchange.Agent.GetItemsInBag(life, IsGarbage);
            if (garbage.Count == 0) return false;
            foreach (var g in garbage)
            {
                Exchange.Sell.Do(life, owner, g, g.Count);
            }
            return true;
        }

        /// <summary>
        /// Action:0022002 - 自动寻路清仓目标
        /// 动态查找清仓目标并自动寻路
        /// </summary>
        [BehaviorAction(0022002)]
        public static bool AutoPathToClearanceTarget(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            if (life.Birthplace == null) return false;
            if (life.Birthplace.Scene == null) return false;
            foreach (Map map in Infrastructure.Agent.GetMaps(Map.Types.Guild))
            {
                if (map.Scene == life.Birthplace.Scene)
                {
                    if (map.Content.Has<Life>(l => Infrastructure.Agent.Guild.IsOwner(l)))
                    {
                        Move.Walk.FollowShortest(life, map);
                        return true;
                    }
                }
            }
            return false;


        }

        [BehaviorAction(0022005)]
        public static bool OrganizeMaterial(Character character)
        {
            if (character is not Logic.Life life) return false;
            Item box = Infrastructure.Agent.GetBox(life.Map, Infrastructure.ContainerType.Material);
            if (box == null) return false;
            
            foreach (Character c in Exchange.Give.Availability(life))
            {
                if (IsMaterial(c))
                {
                    var item = c as Logic.Item;
                    Exchange.Give.Do(life, box, item, item.Count);
                }
            }
            
            return true;
        }

        [BehaviorAction(0022006)]
        public static bool OrganizeProduct(Character character)
        {
            if (character is not Logic.Life life) return false;
            Item box = Infrastructure.Agent.GetBox(life.Map, Infrastructure.ContainerType.Product);
            if (box == null) return false;
            foreach (Character c in Exchange.Give.Availability(life))
            {
                if (IsProduct(c))
                {
                    var item = c as Logic.Item;
                    Exchange.Give.Do(life, box, item, item.Count);
                }
            }
            return true;
        }

        [BehaviorAction(0022008)]
        public static bool OrganizeMiscellaneous(Character character)
        {
            if (character is not Logic.Life life) return false;
            Item box = Infrastructure.Agent.GetBox(life.Map, Infrastructure.ContainerType.Miscellaneous);
            if (box == null) return false;
            foreach (Character c in Exchange.Give.Availability(life))
            {
                if (IsMiscellaneous(c))
                {
                    var item = c as Logic.Item;
                    Exchange.Give.Do(life, box, item, item.Count);
                }
            }
            return true;
        }

        [BehaviorAction(0022009)]
        public static bool SelfSupply(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            foreach (Item box in Infrastructure.Agent.GetBoxes(life.Map))
            {
                foreach (Item item in box.Content.Gets<Item>())
                {
                    if (item.Type == Item.Types.Food)
                    {
                        Domain.Use.Agent.Instance.Do(life, item);
                        return true;
                    }
                }
            }
            return false;


        }

        [BehaviorCondition(0021017)]
        public static bool HasDeliveryTarget(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            foreach (Character c in Exchange.Give.Availability(life))
            {
                if (c is Item item && Cast.Agent.Instance.cook.IsMaterial(item))
                {
                    return true;
                }
            }
            return false;
        }

        [BehaviorCondition(0021018)]
        public static bool HasDeliveryObject(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            if (life.Birthplace == null) return false;
            if (life.Birthplace.Scene == null) return false;
            
            foreach (Map map in Infrastructure.Agent.GetMaps(Map.Types.Restaurant))
            {
                if (map.Scene == life.Birthplace.Scene)
                {
                    Life shopkeeper = map.Content.Get<Life>(l => Infrastructure.Agent.Cook.IsOwner(l));
                    if (shopkeeper != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        [BehaviorCondition(0021019)]
        public static bool IsDeliveryObjectAdjacent(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            Life shopkeeperOfFoodShop = life.Map.Content.Get<Life>(l => Infrastructure.Agent.Cook.IsOwner(l));
            List<Item> itemsForFoodShop = Domain.Exchange.Agent.GetItems(life, Cast.Agent.Instance.cook.IsMaterial);
            if (shopkeeperOfFoodShop != null && itemsForFoodShop.Count > 0) return true;
            return false;
        }

        [BehaviorCondition(0021020)]
        public static bool HasPickupTarget(Character character)
        {
            return false;
        }

        [BehaviorCondition(0021021)]
        public static bool HasPickupObject(Character character)
        {
            return false;
        }

        [BehaviorCondition(0021022)]
        public static bool IsPickupObjectAdjacent(Character character)
        {
            return false;
        }

        [BehaviorAction(0022010)]
        public static bool Deliver(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            Life shopkeeper = life.Map.Content.Get<Life>(l => Infrastructure.Agent.Cook.IsOwner(l));
            if (shopkeeper == null) return false;
            
            foreach (Character c in Exchange.Give.Availability(life))
            {
                if (c is Item item && Cast.Agent.Instance.cook.IsMaterial(item))
                {
                    Domain.Exchange.Give.Do(life, shopkeeper, item, item.Count);
                }
            }
            
            return true;
        }

        [BehaviorAction(0022011)]
        public static bool AutoPathToDeliveryObject(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            if (life.Birthplace == null) return false;
            if (life.Birthplace.Scene == null) return false;
            
            List<Item> itemsForFoodShop = Domain.Exchange.Agent.GetItems(life, Cast.Agent.Instance.cook.IsMaterial);
            if (itemsForFoodShop.Count == 0) return false;
            
            foreach (Map map in Infrastructure.Agent.GetMaps(Map.Types.Restaurant))
            {
                if (map.Scene == life.Birthplace.Scene)
                {
                    Life shopkeeper = map.Content.Get<Life>(l => Infrastructure.Agent.Cook.IsOwner(l));
                    if (shopkeeper != null)
                    {
                        Domain.Move.Walk.FollowShortest(life, map);
                        return true;
                    }
                }
            }
            return false;
        }

        [BehaviorAction(0022012)]
        public static bool Pickup(Character character)
        {
            return false;
        }

        [BehaviorAction(0022013)]
        public static bool AutoPathToPickupObject(Character character)
        {
            return false;
        }

        /// <summary>
        /// Condition:0021023 - 存储对象是否存在
        /// 检查是否存在可用的存储容器（仓库等通用容器）
        /// </summary>
        [BehaviorCondition(0021023)]
        public static bool HasStorageObject(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            if (life.Birthplace == null) return false;

            bool hasContainer = life.Birthplace.Content.Has<Item>(i => i.Config.Id == Logic.Constant.MiscellaneousContainer);
            bool hasMiscellaneous = Domain.Exchange.Agent.GetItemsInBag(life, IsMiscellaneous).Count > 0;
            
            return hasContainer && hasMiscellaneous;
        }

        /// <summary>
        /// Condition:0021024 - 存储对象是否相邻
        /// 检查存储容器是否在相邻位置（当前地图内）
        /// </summary>
        [BehaviorCondition(0021024)]
        public static bool IsStorageObjectAdjacent(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            if (life.Birthplace == null) return false;
            if (life.Map != life.Birthplace) return false;
            return life.Map.Content.Has<Item>(i => i.Config.Id == Logic.Constant.MiscellaneousContainer);
        }

        /// <summary>
        /// Action:0022014 - 存储
        /// 将背包中的物品存入存储容器
        /// </summary>
        [BehaviorAction(0022014)]
        public static bool Store(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            if (life.Birthplace == null) return false;
            if (life.Map != life.Birthplace) return false;
            Item obj = life.Birthplace.Content.Get<Item>(i => i.Config.Id == Logic.Constant.MiscellaneousContainer);
            if (obj == null) return false;
            List<Item> items = Domain.Exchange.Agent.GetItemsInBag(life, IsMiscellaneous);
            if (items.Count == 0) return false;
            foreach (var g in items)
            {
                Exchange.Give.Do(life, obj, g, g.Count);
            }
            return true;
        }

        /// <summary>
        /// Action:0022015 - 自动寻路存储对象
        /// 自动寻路到存储容器所在位置
        /// </summary>
        [BehaviorAction(0022015)]
        public static bool AutoPathToStorageObject(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            if (life.Birthplace == null) return false;
            Item obj = life.Birthplace.Content.Get<Item>(i => i.Config.Id == Logic.Constant.MiscellaneousContainer);
            if (obj == null) return false;
            Domain.Move.Walk.FollowShortest(life, obj.Map);
            return true;
        }

        [BehaviorCondition(0021025)]
        public static bool HasDeliveryLightGearShopTarget(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            foreach (Character c in Exchange.Give.Availability(life))
            {
                if (c is Item item && Cast.Agent.Instance.sew.IsMaterial(item))
                {
                    return true;
                }
            }
            return false;
        }

        [BehaviorCondition(0021026)]
        public static bool HasDeliveryLightGearShopObject(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            if (life.Birthplace == null) return false;
            if (life.Birthplace.Scene == null) return false;
            
            foreach (Map map in Infrastructure.Agent.GetMaps(Map.Types.LightGearShop))
            {
                if (map.Scene == life.Birthplace.Scene)
                {
                    Life shopkeeper = map.Content.Get<Life>(l => Infrastructure.Agent.Sew.IsOwner(l));
                    if (shopkeeper != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        [BehaviorCondition(0021027)]
        public static bool IsDeliveryLightGearShopObjectAdjacent(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            Life shopkeeperOfLightGearShop = life.Map.Content.Get<Life>(l => Infrastructure.Agent.Sew.IsOwner(l));
            List<Item> itemsForLightGearShop = Domain.Exchange.Agent.GetItems(life, Cast.Agent.Instance.sew.IsMaterial);
            if (shopkeeperOfLightGearShop != null && itemsForLightGearShop.Count > 0) return true;
            return false;
        }

        [BehaviorAction(0022016)]
        public static bool DeliverToLightGearShop(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            Life shopkeeper = life.Map.Content.Get<Life>(l => Infrastructure.Agent.Sew.IsOwner(l));
            if (shopkeeper == null) return false;
            foreach (Character c in Exchange.Give.Availability(life))
            {
                if (c is Item item && Cast.Agent.Instance.sew.IsMaterial(item))
                {
                    Domain.Exchange.Give.Do(life, shopkeeper, item, item.Count);
                }
            }
            return true;
        }

        [BehaviorAction(0022017)]
        public static bool AutoPathToDeliveryLightGearShopObject(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            if (life.Birthplace == null) return false;
            if (life.Birthplace.Scene == null) return false;
            
            List<Item> itemsForLightGearShop = Domain.Exchange.Agent.GetItems(life, Cast.Agent.Instance.sew.IsMaterial);
            if (itemsForLightGearShop.Count == 0) return false;
            
            foreach (Map map in Infrastructure.Agent.GetMaps(Map.Types.LightGearShop))
            {
                if (map.Scene == life.Birthplace.Scene)
                {
                    Life shopkeeper = map.Content.Get<Life>(l => Infrastructure.Agent.Sew.IsOwner(l));
                    if (shopkeeper != null)
                    {
                        Domain.Move.Walk.FollowShortest(life, map);
                        return true;
                    }
                }
            }
            return false;
        }

        [BehaviorCondition(0021028)]
        public static bool HasDeliveryHeavyGearShopTarget(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            foreach (Character c in Exchange.Give.Availability(life))
            {
                if (c is Item item && Cast.Agent.Instance.smith.IsMaterial(item))
                {
                    return true;
                }
            }
            return false;
        }

        [BehaviorCondition(0021029)]
        public static bool HasDeliveryHeavyGearShopObject(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            if (life.Birthplace == null) return false;
            if (life.Birthplace.Scene == null) return false;
            
            foreach (Map map in Infrastructure.Agent.GetMaps(Map.Types.HeavyGearShop))
            {
                if (map.Scene == life.Birthplace.Scene)
                {
                    Life shopkeeper = map.Content.Get<Life>(l => Infrastructure.Agent.Forge.IsOwner(l));
                    if (shopkeeper != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        [BehaviorCondition(0021030)]
        public static bool IsDeliveryHeavyGearShopObjectAdjacent(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            Life shopkeeperOfHeavyGearShop = life.Map.Content.Get<Life>(l => Infrastructure.Agent.Forge.IsOwner(l));
            List<Item> itemsForHeavyGearShop = Domain.Exchange.Agent.GetItems(life, Cast.Agent.Instance.smith.IsMaterial);
            if (shopkeeperOfHeavyGearShop != null && itemsForHeavyGearShop.Count > 0) return true;
            return false;
        }

        [BehaviorAction(0022018)]
        public static bool DeliverToHeavyGearShop(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            Life shopkeeper = life.Map.Content.Get<Life>(l => Infrastructure.Agent.Forge.IsOwner(l));
            if (shopkeeper == null) return false;
            foreach (Character c in Exchange.Give.Availability(life))
            {
                if (c is Item item && Cast.Agent.Instance.smith.IsMaterial(item))
                {
                    Domain.Exchange.Give.Do(life, shopkeeper, item, item.Count);
                }
            }
            return true;
        }

        [BehaviorAction(0022019)]
        public static bool AutoPathToDeliveryHeavyGearShopObject(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            if (life.Birthplace == null) return false;
            if (life.Birthplace.Scene == null) return false;
            
            List<Item> itemsForHeavyGearShop = Domain.Exchange.Agent.GetItems(life, Cast.Agent.Instance.smith.IsMaterial);
            if (itemsForHeavyGearShop.Count == 0) return false;
            
            foreach (Map map in Infrastructure.Agent.GetMaps(Map.Types.HeavyGearShop))
            {
                if (map.Scene == life.Birthplace.Scene)
                {
                    Life shopkeeper = map.Content.Get<Life>(l => Infrastructure.Agent.Forge.IsOwner(l));
                    if (shopkeeper != null)
                    {
                        Domain.Move.Walk.FollowShortest(life, map);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
