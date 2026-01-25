using Logic;

namespace Domain.Mall
{
    public static class Purchase
    {
        private const int PackageBagId = 1000049;

        public static bool CanReceive(Player player)
        {
            if (player == null) return false;
            if (player.Hand == null) return false;
            if (player.Hand.Content.Count<Item>() > 0) return false;
            return true;
        }

        public enum FailReason
        {
            None,
            InvalidCount,
            NoGem,
            ExceedMax,
            InsufficientGem,
            CannotReceive
        }

        public static bool Can(Player player, Logic.Config.Mall mallConfig, int count, out FailReason reason)
        {
            reason = FailReason.None;
            
            if (count <= 0) { reason = FailReason.InvalidCount; return false; }
            if (player.Gem <= 0) { reason = FailReason.NoGem; return false; }

            int maxBuyable = Agent.Instance.GetMaxBuyable(player, mallConfig);
            if (count > maxBuyable) { reason = FailReason.ExceedMax; return false; }

            int totalPrice = mallConfig.Price * count;
            if (player.Gem < totalPrice) { reason = FailReason.InsufficientGem; return false; }

            // Only check CanReceive for item-based products (not subscription or exp multipliers)
            bool isVirtualProduct = mallConfig.Type == Logic.Config.Mall.Types.Subscription ||
                                    mallConfig.Type == Logic.Config.Mall.Types.Experience;
            if (!isVirtualProduct && !CanReceive(player)) { reason = FailReason.CannotReceive; return false; }

            return true;
        }
        
        public static bool Can(Player player, Logic.Config.Mall mallConfig, int count)
        {
            return Can(player, mallConfig, count, out _);
        }

        private static bool NeedPackage(Logic.Config.Mall mallConfig)
        {
            return mallConfig.Items != null && mallConfig.Items.Count > 1;
        }

        private static Item CreatePackage(Player player, Logic.Config.Mall mallConfig, int count)
        {
            var bag = player.Load<Logic.Config.Item, Item>(PackageBagId, 1);
            foreach (var item in mallConfig.Items)
            {
                bag.Load<Logic.Config.Item, Item>(item.Key, item.Value * count);
            }
            return bag;
        }

        private static void DeliverDirect(Player player, Logic.Config.Mall mallConfig, int count)
        {
            foreach (var item in mallConfig.Items)
            {
                var created = player.Load<Logic.Config.Item, Item>(item.Key, item.Value * count);
                Exchange.Receive.Do(player, created, created.Count);
            }
        }

        public static bool Do(Player player, Logic.Config.Mall mallConfig, int count, out FailReason reason)
        {
            if (!Can(player, mallConfig, count, out reason)) return false;

            int totalPrice = mallConfig.Price * count;

            player.Gem -= totalPrice;

            // Handle different product types
            switch (mallConfig.Type)
            {
                case Logic.Config.Mall.Types.Subscription:
                    DeliverSubscription(player, mallConfig, count);
                    break;
                case Logic.Config.Mall.Types.Experience:
                    DeliverExperience(player, mallConfig, count);
                    break;
                default:
                    if (NeedPackage(mallConfig))
                    {
                        var package = CreatePackage(player, mallConfig, count);
                        Exchange.Receive.Do(player, package, 1);
                    }
                    else
                    {
                        DeliverDirect(player, mallConfig, count);
                    }
                    break;
            }

            Agent.Instance.RecordPurchase(player, mallConfig.Id, count);

            BroadcastPurchase(player, mallConfig, count, totalPrice);

            return true;
        }
        
        private static void DeliverSubscription(Player player, Logic.Config.Mall mallConfig, int count)
        {
            int targetCardValue = mallConfig.Value;
            
            // Each purchase adds 30 days per count
            int daysToAdd = Logic.Constant.MonthlyCardDurationDays * count;
            DateTime now = DateTime.Now;
            
            if (targetCardValue == 1)
            {
                // Basic card - extend or activate TokenMonthBasicLastTime
                if (player.TokenMonthBasicLastTime > now)
                {
                    player.TokenMonthBasicLastTime = player.TokenMonthBasicLastTime.AddDays(daysToAdd);
                }
                else
                {
                    player.TokenMonthBasicLastTime = now.AddDays(daysToAdd);
                }
            }
            else if (targetCardValue == 2)
            {
                // Premium card - extend or activate TokenMonthPremiumLastTime
                if (player.TokenMonthPremiumLastTime > now)
                {
                    player.TokenMonthPremiumLastTime = player.TokenMonthPremiumLastTime.AddDays(daysToAdd);
                }
                else
                {
                    player.TokenMonthPremiumLastTime = now.AddDays(daysToAdd);
                }
            }
            
            // Start Mall refresh if player is viewing Mall panel (fixes: refresh not starting after first purchase)
            Subscription.Agent.TryStartMallRefresh(player);
        }
        
        private static void DeliverExperience(Player player, Logic.Config.Mall mallConfig, int count)
        {
            // Experience type is determined by mall item id:
            // 20 = Character Exp, 30 = Skill Exp, 40 = Pet Exp
            int expType = mallConfig.Id / 10;
            int amount = mallConfig.Value * count;
            
            switch (expType)
            {
                case 2:  // Character Exp (id 20-29)
                    player.BenefitCountForLife += amount;
                    break;
                case 3:  // Skill Exp (id 30-39)
                    player.BenefitCountForSkill += amount;
                    break;
                case 4:  // Pet Exp (id 40-49)
                    player.BenefitCountForLive += amount;
                    break;
            }
        }
        
        public static bool Do(Player player, Logic.Config.Mall mallConfig, int count)
        {
            return Do(player, mallConfig, count, out _);
        }

        private static void BroadcastPurchase(Player player, Logic.Config.Mall mallConfig, int count, int totalPrice)
        {
            string mallName = Text.Agent.Instance.Get(mallConfig.Name, player);
            Domain.Broadcast.Instance.System(player,
                [Domain.Text.Agent.Instance.Id(Logic.Text.Labels.MallPurchase)],
                ("item", mallName),
                ("count", count.ToString()),
                ("price", totalPrice.ToString()));
        }
    }
}
