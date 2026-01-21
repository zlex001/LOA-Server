using Logic;
using Newtonsoft.Json;

namespace Domain.Display
{
    public class Left
    {
        public static List<Option.Item> Operation(Player player, Ability target)
        {
            List<Option.Item> items = new();
            
            // For remote Life, show name + description + distance (replace Part info)
            if (target is Life life && life.Map != player.Map)
            {
                // Name
                items.Add(new Option.Item(Domain.Text.Decorate.Life(life, player)));
                // Description
                items.Add(new Option.Item($"{Utils.Text.Indent(0)}{Text.Description.Life(life, player)}"));
                // Distance instead of Part info
                int distance = Perception.Agent.Instance.GetDistance(player, life);
                string distanceLabel = Text.Agent.Instance.Get(Logic.Text.Labels.Distance, player);
                items.Add(new Option.Item($"{Utils.Text.Indent(0)}{distanceLabel}: {distance}"));
                return items;
            }
            
            // For remote Item, show name + description + distance (replace container info)
            // Exclude items equipped on player's own Part (they are "local" to player)
            if (target is Logic.Item item && item.Map != player.Map
                && !(item.Parent is Part part && part.Parent == player))
            {
                // Name
                items.Add(new Option.Item(Domain.Text.Decorate.Item(item, player)));
                // Description
                items.Add(new Option.Item($"{Utils.Text.Indent(0)}{Domain.Text.Description.Item(item, player)}"));
                // Distance
                int distance = Perception.Agent.Instance.GetDistance(player, item);
                string distanceLabel = Text.Agent.Instance.Get(Logic.Text.Labels.Distance, player);
                items.Add(new Option.Item($"{Utils.Text.Indent(0)}{distanceLabel}: {distance}"));
                return items;
            }
            
            var results = Agent.Instance.GetInformation((Ability)target, player, target);
            if (results != null)
            {
                items = results.Cast<Option.Item>().ToList();
            }
            return items;
        }

        public static List<Option.Item> Shop(Player player, Ability target)
        {
            return target is Life life ? Information.Life(player, life).Cast<Option.Item>().ToList() : new List<Option.Item>();
        }

        public static List<Logic.Option.Item> Teleport(Player player, Logic.Ability target)
        {
            var items = new List<Logic.Option.Item>();

            if (target is Life teleporter)
            {
                items.Add(new Logic.Option.Item(Logic.Option.Item.Type.Text, Domain.Text.Decorate.Life(teleporter, player)));

                string speech = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.TeleporterSpeech, player.Language);
                items.Add(new Logic.Option.Item(Logic.Option.Item.Type.Text, speech));
            }

            return items;
        }

        public static List<Option.Item> PickOrder(Player player, Ability target)
        {
            if (target is Logic.Item item)
            {
                var leftResults = Information.Item(player, item);
                return leftResults.Cast<Option.Item>().ToList();
            }

            return new List<Option.Item>();
        }

        public static List<Option.Item> Give(Player player, Ability target)
        {
            var option = player.Option;
            if (option?.Relates?.FirstOrDefault() == null)
                return new List<Option.Item>();

            var targetElement = option.Relates[0];
            var informationResults = Agent.Instance.GetInformation(targetElement, player, targetElement);
            return informationResults.Cast<Option.Item>().ToList();
        }

        public static List<Option.Item> GiveOrder(Player player, Ability target)
        {
            if (target is Logic.Item item)
            {
                var leftResults = Information.Item(player, item);
                return leftResults.Cast<Option.Item>().ToList();
            }

            return new List<Option.Item>();
        }

        public static List<Option.Item> Buy(Player player, Ability target)
        {
            return Agent.Instance.GetInformation(target, player, target).Cast<Option.Item>().ToList();
        }

        public static List<Option.Item> BuyOrder(Player player, Ability target)
        {
            return target is Item item ? Information.Item(player, item).Cast<Option.Item>().ToList() : new List<Option.Item>();
        }

        public static List<Option.Item> Sell(Player player, Ability target)
        {
            return Agent.Instance.GetInformation(target, player, target).Cast<Option.Item>().ToList();
        }

        public static List<Option.Item> SellOrder(Player player, Ability target)
        {
            return target is Item item ? Information.Item(player, item).Cast<Option.Item>().ToList() : new List<Option.Item>();
        }

        public static List<Option.Item> DropOrder(Player player, Ability target)
        {
            if (target is Logic.Item item)
            {
                var leftResults = Information.Item(player, item);
                return leftResults.Cast<Option.Item>().ToList();
            }

            return new List<Option.Item>();
        }

        public static List<Option.Item> Mall(Player player, Ability target)
        {
            var items = new List<Option.Item>();
            
            // 0: Title - use Operation.Type.Mall (id=2024) for "Divine Revelation" packaging
            string mallTitle = Text.Agent.Instance.Get((int)Domain.Operation.Type.Mall, player);
            items.Add(new Option.Item(mallTitle));
            
            // 1: Gem Button - format: "God's Revelation: n [+]"
            string gemLabel = Text.Agent.Instance.Get(Logic.Text.Labels.Gem, player.Language);
            items.Add(new Option.Item(Option.Item.Type.Button, 
                ("Text", $"{gemLabel}: {player.Gem}"),
                ("Action", "Recharge")));
            
            // Calculate accumulated monthly exp for display
            Subscription.Agent.AccumulateMonthlyExp(player);
            string monthlyExpLabel = Text.Agent.Instance.Get(Logic.Text.Labels.MonthlyExp, player.Language);
            
            // 2: BasicCard (Angel's Blessing) - as level 0 item
            string basicCardName = Text.Agent.Instance.Get(Logic.Text.Labels.BasicMonthlyCard, player.Language);
            items.Add(new Option.Item($"{Utils.Text.Indent(0)}{basicCardName}"));
            
            // 3: BasicCard days remaining - as level 1 item
            int basicDaysRemaining = Math.Max(0, (int)(player.TokenMonthBasicLastTime - DateTime.Now).TotalDays);
            string basicDaysText = Text.Agent.Instance.GetDynamic(Logic.Text.Labels.DaysRemaining, player.Language, 
                ("days", basicDaysRemaining.ToString()));
            items.Add(new Option.Item($"{Utils.Text.Indent(1)}{basicDaysText}"));
            
            // 4: BasicCard blessing exp - as level 1 item (show basic card accumulated exp)
            double basicAccumulatedExp = player.MonthlyExpAccumulatorBasic;
            items.Add(new Option.Item($"{Utils.Text.Indent(1)}{monthlyExpLabel}: {basicAccumulatedExp:F2}"));
            
            // 5: PremiumCard (God's Blessing) - as level 0 item
            string premiumCardName = Text.Agent.Instance.Get(Logic.Text.Labels.PremiumMonthlyCard, player.Language);
            items.Add(new Option.Item($"{Utils.Text.Indent(0)}{premiumCardName}"));
            
            // 6: PremiumCard days remaining - as level 1 item
            int premiumDaysRemaining = Math.Max(0, (int)(player.TokenMonthPremiumLastTime - DateTime.Now).TotalDays);
            string premiumDaysText = Text.Agent.Instance.GetDynamic(Logic.Text.Labels.DaysRemaining, player.Language, 
                ("days", premiumDaysRemaining.ToString()));
            items.Add(new Option.Item($"{Utils.Text.Indent(1)}{premiumDaysText}"));
            
            // 7: PremiumCard blessing exp - as level 1 item (show premium card accumulated exp)
            double premiumAccumulatedExp = player.MonthlyExpAccumulatorPremium;
            items.Add(new Option.Item($"{Utils.Text.Indent(1)}{monthlyExpLabel}: {premiumAccumulatedExp:F2}"));
            
            // 8: Claim Button
            string claimLabel = Text.Agent.Instance.Get(Logic.Text.Labels.ClaimMonthlyExp, player.Language);
            items.Add(new Option.Item(Option.Item.Type.Button, ("Text", claimLabel), ("Action", "ClaimMonthlyExp")));
            
            // 9: ExpMultiplier (Divine Insights) - as level 0 item
            string expMultiplierLabel = Text.Agent.Instance.Get(Logic.Text.Labels.ExpMultiplier, player.Language);
            items.Add(new Option.Item($"{Utils.Text.Indent(0)}{expMultiplierLabel}"));
            
            // 10-12: Character, Skill, Pet - as level 1 items
            string characterLabel = Text.Agent.Instance.Get(Logic.Text.Labels.Character, player.Language);
            string skillLabel = Text.Agent.Instance.Get(Logic.Text.Labels.SkillLabel, player.Language);
            string petLabel = Text.Agent.Instance.Get(Logic.Text.Labels.Pet, player.Language);
            string characterUses = Text.Agent.Instance.GetDynamic(Logic.Text.Labels.Uses, player.Language, ("count", player.BenefitCountForLife.ToString()));
            string skillUses = Text.Agent.Instance.GetDynamic(Logic.Text.Labels.Uses, player.Language, ("count", player.BenefitCountForSkill.ToString()));
            string petUses = Text.Agent.Instance.GetDynamic(Logic.Text.Labels.Uses, player.Language, ("count", player.BenefitCountForLive.ToString()));
            items.Add(new Option.Item($"{Utils.Text.Indent(1)}{characterLabel}: {characterUses}"));
            items.Add(new Option.Item($"{Utils.Text.Indent(1)}{skillLabel}: {skillUses}"));
            items.Add(new Option.Item($"{Utils.Text.Indent(1)}{petLabel}: {petUses}"));
            
            return items;
        }
        
        public static void MallClick(Player player, Ability target, int index)
        {
            var cardType = Subscription.Agent.GetCardType(player);
            
            // Index 1: Recharge button (index 0 is Title)
            if (index == 1)
            {
                var client = Net.Tcp.Instance.Content.Get<Net.Client>(c => c.Player == player);
                if (client == null) return;
                
                switch (client.Platform)
                {
                    case Logic.Database.Device.Platforms.Android:
                        Domain.Recharge.Instance.CreateAlipayOrder(player);
                        break;
                    case Logic.Database.Device.Platforms.IPhonePlayer:
                        Net.Tcp.Instance.Send(player, new Net.Protocol.RequestIAPReceipts());
                        break;
                    default:
                        player.Create<Option>(Option.Types.CardInput, player, player);
                        break;
                }
                return;
            }
            
            // Index 8: Monthly Exp claim button (only visible for card holders)
            // Position: 0=Title, 1=Gem, 2=BasicCard, 3=BasicDays, 4=BasicExp, 5=PremiumCard, 6=PremiumDays, 7=PremiumExp, 8=ClaimButton
            if (index == 8 && cardType != Subscription.CardType.None)
            {
                // Message is sent inside ClaimMonthlyExp before exp is added (ensures correct order: claim -> level up)
                Subscription.Agent.ClaimMonthlyExp(player);
                
                // Refresh the display to update the accumulated exp count
                Agent.Instance.Refresh(player);
            }
        }
        
        public static List<Option.Item> CardInput(Player player, Ability target)
        {
            var items = new List<Option.Item>();
            items.Add(new Option.Item(Option.Item.Type.Text, 
                ("Text", Text.Agent.Instance.Get(Logic.Text.Labels.RechargeCard, player.Language))));
            items.Add(new Option.Item(Option.Item.Type.Input,
                ("Text", ""),
                ("PlaceholderText", Text.Agent.Instance.Get(Logic.Text.Labels.CardInputPlaceholder, player.Language))));
            items.Add(new Option.Item(Option.Item.Type.Confirm));
            return items;
        }
        
        public static void CardInputConfirm(Player player, Ability target, int index)
        {
            if (player.Option?.Setting?.Input == null) return;
            
            string cardCode = player.Option.Setting.Input;
            player.monitor.Fire(Logic.Player.Event.CardPurchase, player, cardCode);
            player.OptionBackward();
        }

        public static List<Option.Item> MallOrder(Player player, Ability target)
        {
            var items = new List<Option.Item>();
            var setting = player.Option?.Setting;
            if (setting == null) return items;
            
            int mallId = setting.data.Get<int>(Option.Settings.Data.MallId);
            var mallConfig = Logic.Config.Agent.Instance.Content.Get<Logic.Config.Mall>(m => m.Id == mallId);
            if (mallConfig == null) return items;
            
            // Title - Product Name (decorated style)
            string mallName = Text.Agent.Instance.Get(mallConfig.Name, player);
            items.Add(new Option.Item(Utils.Text.Color(Utils.Text.Colors.EntityItem, mallName)));
            
            // Subscription type - simplified display
            if (mallConfig.Type == Logic.Config.Mall.Types.Subscription)
            {
                bool isPremium = mallConfig.Value == 2;
                
                // Blessing Effects header
                string blessingEffectsLabel = Text.Agent.Instance.Get(Logic.Text.Labels.BlessingEffects, player.Language);
                items.Add(new Option.Item($"{Utils.Text.Indent(0)}{blessingEffectsLabel}:"));
                
                // Benefits list
                Logic.Text.Labels benefitLabel = isPremium 
                    ? Logic.Text.Labels.PremiumMonthlyCardBenefits 
                    : Logic.Text.Labels.BasicMonthlyCardBenefits;
                string benefits = Text.Agent.Instance.Get(benefitLabel, player.Language);
                
                // Split benefits by '|' and display each with indent
                string[] benefitList = benefits.Split('|');
                foreach (string benefit in benefitList)
                {
                    if (!string.IsNullOrEmpty(benefit))
                    {
                        items.Add(new Option.Item($"{Utils.Text.Indent(1)}{benefit.Trim()}"));
                    }
                }
                
                // Price
                string priceLabel = Text.Agent.Instance.Get(Logic.Text.Labels.Price, player.Language);
                items.Add(new Option.Item($"{Utils.Text.Indent(0)}{priceLabel}: {mallConfig.Price}"));
            }
            // Experience type - show current count
            else if (mallConfig.Type == Logic.Config.Mall.Types.Experience)
            {
                // Description
                string mallDesc = Text.Agent.Instance.Get(mallConfig.Description, player);
                items.Add(new Option.Item($"{Utils.Text.Indent(0)}{mallDesc}"));
                
                string expMultiplierLabel = Text.Agent.Instance.Get(Logic.Text.Labels.ExpMultiplier, player.Language);
                int expType = mallConfig.Id / 10;
                
                string currentLabel = "";
                int currentCount = 0;
                
                switch (expType)
                {
                    case 2:  // Character Exp
                        currentLabel = Text.Agent.Instance.Get(Logic.Text.Labels.Character, player.Language);
                        currentCount = player.BenefitCountForLife;
                        break;
                    case 3:  // Skill Exp
                        currentLabel = Text.Agent.Instance.Get(Logic.Text.Labels.SkillLabel, player.Language);
                        currentCount = player.BenefitCountForSkill;
                        break;
                    case 4:  // Pet Exp
                        currentLabel = Text.Agent.Instance.Get(Logic.Text.Labels.Pet, player.Language);
                        currentCount = player.BenefitCountForLive;
                        break;
                }
                
                if (!string.IsNullOrEmpty(currentLabel))
                {
                    string usesText = Text.Agent.Instance.GetDynamic(Logic.Text.Labels.Uses, player.Language, 
                        ("count", currentCount.ToString()));
                    items.Add(new Option.Item($"{Utils.Text.Indent(1)}{currentLabel} {expMultiplierLabel}: {usesText}"));
                }
                
                // Price
                string priceLabel = Text.Agent.Instance.Get(Logic.Text.Labels.Price, player.Language);
                string gemLabel = Text.Agent.Instance.Get(Logic.Text.Labels.Gem, player.Language);
                items.Add(new Option.Item($"{Utils.Text.Indent(1)}{priceLabel}: {mallConfig.Price} {gemLabel}"));
            }
            // Other types - show description and price
            else
            {
                // Description
                string mallDesc = Text.Agent.Instance.Get(mallConfig.Description, player);
                items.Add(new Option.Item($"{Utils.Text.Indent(0)}{mallDesc}"));
                
                // Price
                string priceLabel = Text.Agent.Instance.Get(Logic.Text.Labels.Price, player.Language);
                string gemLabel = Text.Agent.Instance.Get(Logic.Text.Labels.Gem, player.Language);
                items.Add(new Option.Item($"{Utils.Text.Indent(1)}{priceLabel}: {mallConfig.Price} {gemLabel}"));
            }
            
            return items;
        }

        private static List<Logic.Option.Item> GenerateOptionItems(Logic.Option.Item.Type itemType, string[] texts)
        {
            var items = new List<Logic.Option.Item>();
            foreach (string text in texts)
            {
                items.Add(new Logic.Option.Item
                {
                    type = itemType,
                    data = new Dictionary<string, string> { { "Text", text } }
                });
            }
            return items;
        }
    }
}
