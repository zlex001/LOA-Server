using Logic;
using Newtonsoft.Json;

namespace Domain.Display
{
    public class Right
    {
        public static List<Option.Item> Operation(Player player, Ability target)
        {
            try
            {
                var items = Agent.Instance.Generate(player, (Ability)target);
                return items.Count > 0 ? items : new List<Option.Item>();
            }
            catch (System.Exception ex)
            {
                Utils.Debug.Log.Error("DISPLAY", $"[Right Panel Operation Failed]");
                Utils.Debug.Log.Error("DISPLAY", $"Player: {player?.Id ?? "null"}, Target: {target?.GetType().Name ?? "null"}");
                Utils.Debug.Log.Error("DISPLAY", $"Exception: {ex.Message}");
                Utils.Debug.Log.Error("DISPLAY", $"StackTrace: {ex.StackTrace}");
                return new List<Option.Item>();
            }
        }

        public static List<Option.Item> Shop(Player player, Ability target)
        {
            return new List<Option.Item>
            {
                OptionHelper.BuildButton(Domain. Operation.Type.Buy, Text.Agent.Instance.Get((int)Domain.Operation.Type.Buy, player)),
                OptionHelper.BuildButton(Domain.Operation.Type.Sell, Text.Agent.Instance.Get((int)Domain.Operation.Type.Sell, player))
            };
        }

        public static List<Logic.Option.Item> Teleport(Player player, Logic.Ability target)
        {
            var items = new List<Logic.Option.Item>();

            if (target is Life teleporter && player.Map != null)
            {
                var teleportMaps = Logic.Agent.Instance.Content.Gets<Logic.Map>()
                    .Where(m => m.Type == Logic.Map.Types.Teleport)
                    .ToList();

                if (teleportMaps.Count > 0)
                {
                    var currentMapIndex = teleportMaps.FindIndex(m => m == player.Map);
                    if (currentMapIndex >= 0)
                    {
                        var coordinates = teleportMaps.Select(m => ((float)m.Database.pos[0], (float)m.Database.pos[1])).ToList();
                        var targetIndices = Utils.Test.GetTeleportTargets(currentMapIndex, coordinates, 5);

                        foreach (var targetIndex in targetIndices)
                        {
                            var targetMap = teleportMaps[targetIndex];
                            items.Add(new Logic.Option.Item(Logic.Option.Item.Type.Button, Domain.Text.Name.Map(targetMap, player)));
                        }
                    }
                }
            }

            return items;
        }

        public static List<Option.Item> PickOrder(Player player, Ability target)
        {
            if (target is Logic.Item item)
            {
                var setting = player.Option.Setting;
                setting.data.raw[Option.Settings.Data.SliderMax] = Math.Max(1, item.Count);
                if (setting.SliderValue > item.Count)
                {
                    setting.data.raw[Option.Settings.Data.SliderValue] = Math.Min(item.Count, Math.Max(1, setting.SliderValue));
                }

                var slider = new Option.Item
                {
                    type = Option.Item.Type.Slider,
                    data = new Dictionary<string, string>
                    {
                        { "Text", Text.Agent.Instance.Get(Domain. Operation.Type.Pick, player) },
                        { "SliderValues", JsonConvert.SerializeObject(new int[] { setting.SliderMin, setting.SliderValue, setting.SliderMax }) },
                        { "ValueColor", "WHT" }
                    }
                };
                var confirm = new Option.Item(Option.Item.Type.Confirm);

                return new List<Option.Item> { slider, confirm };
            }

            return new List<Option.Item>();
        }

        public static void ShopClick(Player player, Ability target, int index)
        {
            switch (index)
            {
                case 0:
                    player.Create<Option>(Option.Types.Buy, player, target);
                    break;
                case 1:
                    player.Create<Option>(Option.Types.Sell, player, target);
                    break;
            }
        }

        public static void TeleportClick(Player player, Logic.Ability target, int index)
        {
            if (target is Life teleporter && player.Map != null)
            {
                var teleportMaps = Logic.Agent.Instance.Content.Gets<Logic.Map>()
                    .Where(m => m.Type == Logic.Map.Types.Teleport)
                    .ToList();

                if (teleportMaps.Count > 0)
                {
                    var currentMapIndex = teleportMaps.FindIndex(m => m == player.Map);
                    if (currentMapIndex >= 0)
                    {
                        var coordinates = teleportMaps.Select(m => ((float)m.Database.pos[0], (float)m.Database.pos[1])).ToList();
                        var targetIndices = Utils.Test.GetTeleportTargets(currentMapIndex, coordinates, 5);

                        if (index < targetIndices.Count)
                        {
                            var targetMap = teleportMaps[targetIndices[index]];
                            player.Remove<Logic.Option>();
                        }
                    }
                }
            }
        }

        public static void PickOrderConfirm(Player player, Ability target, int index)
        {
            if (target is Logic.Item item && player.Option != null && player.Option.Type == Option.Types.PickOrder)
            {
                int count = player.Option.Setting.SliderValue;
                Domain.Exchange.Pick.Do(player, item, count);
                player.OptionBackward();
            }
        }

        public static void OperationClick(Player player, Ability target, int index)
        {
            var logicOption = player.Option;
            if (logicOption == null || logicOption.Relates.Count == 0)
            {
                return;
            }

            var buttonItems = Agent.Instance.Create(player, Logic.Option.RightPanel.Operation, logicOption.Relates.First());

            if (buttonItems == null || index < 0 || index >= buttonItems.Count)
            {
                return;
            }

            var item = buttonItems[index];

            if (!item.data.TryGetValue("Action", out var actionValue))
            {
                return;
            }

            if (actionValue.ToString().StartsWith("ContainerItem_"))
            {
                HandleContainerItemClick(player, actionValue.ToString(), logicOption.Relates.First());
                return;
            }

            if (Enum.TryParse(typeof(Operation.Type), actionValue.ToString(), out var actionEnum))
            {
                Agent.Instance.Execute(player, (Enum)actionEnum, logicOption.Relates.First(), 0);
                return;
            }
        }

        public static List<Option.Item> Give(Player sub, Ability obj)
        {
            var results = new List<Option.Item>();
            if (sub == null) return results;
            if (obj == null) return results;
            if (sub.Option == null) return results;
            if (sub.Option.Relates == null) return results;
            if (sub.Option.Relates.FirstOrDefault() == null) return results;
            foreach (var character in Exchange.Give.Availability(sub))
            {
                if (character is Item item)
                {
                    results.Add(new Option.Item(Option.Item.Type.Button, ("Text", Domain.Text.Decorate.Item(item, sub)), ("Action", $"GiveItem_{character.GetHashCode()}")));
                }
                else if (character is Life life)
                {
                    results.Add(new Option.Item(Option.Item.Type.Button, ("Text", Domain.Text.Decorate.Life(life, sub)), ("Action", $"GiveItem_{character.GetHashCode()}")));
                }
            }
            return results;
        }

        public static List<Option.Item> GiveOrder(Player player, Ability target)
        {
            if (target is Logic.Item item)
            {
                // 动态更新滑条范围
                var setting = player.Option.Setting;
                setting.data.raw[Option.Settings.Data.SliderMax] = Math.Max(1, item.Count);
                if (setting.SliderValue > item.Count)
                {
                    setting.data.raw[Option.Settings.Data.SliderValue] = Math.Min(item.Count, Math.Max(1, setting.SliderValue));
                }

                var slider = new Option.Item
                {
                    type = Option.Item.Type.Slider,
                    data = new Dictionary<string, string>
                    {
                        { "Text", "给予" },
                        { "SliderValues", JsonConvert.SerializeObject(new int[] { setting.SliderMin, setting.SliderValue, setting.SliderMax }) },
                        { "ValueColor", "WHT" }
                    }
                };
                var confirm = new Option.Item(Option.Item.Type.Confirm);

                return new List<Option.Item> { slider, confirm };
            }

            return new List<Option.Item>();
        }

        public static void GiveClick(Player sub, Ability obj, int index)
        {
            if (index < 0) return;
            if (sub == null) return;
            if (obj == null) return;
            if (sub.Option == null) return;
            if (sub.Option.Relates == null) return;
            if (sub.Option.Relates.FirstOrDefault() == null) return;
            List<Character> targets = Exchange.Give.Availability(sub);
            if (index >= targets.Count) return;
            var target = targets[index];
            //var relate = sub.Option.Relates.FirstOrDefault();
            if (target is Item i && i.Count > 1)
            {
                sub.Create<Option>(Option.Types.GiveOrder, sub, target);
                return;
            }

            if (target is Life lifeTarget && obj is Life lifeObj)
            {
                Exchange.Give.Do(sub, lifeObj, lifeTarget);
            }
            else if (target is Life lifeTarget2 && obj is Item itemObj)
            {
                Exchange.Give.Do(sub, itemObj, lifeTarget2);
            }
            else if (target is Item itemTarget && obj is Life lifeObj2)
            {
                Exchange.Give.Do(sub, lifeObj2, itemTarget,1);
            }
            else if (target is Item itemTarget2 && obj is Item itemObj2)
            {
                Exchange.Give.Do(sub, itemObj2, itemTarget2, 1);
            }
        }

        public static void GiveOrderConfirm(Player player, Ability target, int index)
        {
            if (target is Logic.Item item && player.Option != null && player.Option.Type == Option.Types.GiveOrder)
            {
                var option = player.Content.Gets<Option>().FirstOrDefault(o => o.Type == Option.Types.Give);
                int count = player.Option.Setting.SliderValue;

                if (option?.Relates?.FirstOrDefault() is Life life_receiver)
                {
                    Exchange.Give.Do(player, life_receiver, item, count);

                }
                else if (option?.Relates?.FirstOrDefault() is Logic.Item item_receiver)
                {
                    Exchange.Give.Do(player, item_receiver, item, count);
                }

                player.monitor.Fire(Logic.Option.Event.Refresh, player);
            }
        }

        public static List<Option.Item> Buy(Player player, Ability target)
        {
            var results = new List<Option.Item>();
            results.Add(new Option.Item(Option.Item.Type.Filter, ("PlaceholderText", "在此输入搜索物品..."), ("Text", player.Option.Setting?.Filter ?? "")));
            results.Add(new Option.Item(Option.Item.Type.ToggleGroup, ("Text", player.Option.Setting?.ToggleGroupText ?? "")));
            Character character = target as Character;
            if (character == null) return results;
            Domain.Infrastructure.Shop shop = Infrastructure.Agent.GetShop(character);
            if (shop == null) return results;
            var filteredGoods = shop.GetGoods(character.Map).Where(item => ShouldShowBuyItem(player.Option, item)).ToList();
            foreach (var item in filteredGoods)
            {
                results.Add(new Option.Item
                {
                    type = Option.Item.Type.Button,
                    data = new Dictionary<string, string>
                        {
                            { "Text", $"{Text.Decorate.Item(item, player)} ({item.Count})" },
                            { "Action", $"BuyItem_{item.GetHashCode()}" }
                        }
                });
            }
            return results;
        }

        public static List<Option.Item> BuyOrder(Player player, Ability target)
        {
            if (target is Item item)
            {
                var setting = player.Option.Setting;
                setting.data.raw[Option.Settings.Data.SliderMax] = Math.Max(1, item.Count);
                if (setting.SliderValue > item.Count)
                {
                    setting.data.raw[Option.Settings.Data.SliderValue] = Math.Min(item.Count, Math.Max(1, setting.SliderValue));
                }

                var slider = new Option.Item
                {
                    type = Option.Item.Type.Slider,
                    data = new Dictionary<string, string>
                    {
                        { "Text", "购买" },
                        { "SliderValues", JsonConvert.SerializeObject(new int[] { setting.SliderMin, setting.SliderValue, setting.SliderMax }) },
                        { "ValueColor", "WHT" }
                    }
                };
                var confirm = new Option.Item(Option.Item.Type.Confirm);
                return new List<Option.Item> { slider, confirm };
            }
            return new List<Option.Item>();
        }

        public static void BuyClick(Player player, Ability target, int index)
        {
            if (player == null) return;
            if (target == null) return;
            if (player.Option == null) return;
            if (player.Option.Relates == null) return;
            if (player.Option.Relates.FirstOrDefault() == null) return;
            Character character = target as Character;
            if (character == null) return;
            Domain.Infrastructure.Shop shop = Infrastructure.Agent.GetShop(character);
            if (shop == null) return;
            var filteredGoods = shop.GetGoods(character.Map).Where(item => ShouldShowBuyItem(player.Option, item)).ToList();
            int itemIndex = index - 2;
            if (itemIndex < 0) return;
            if (itemIndex >= filteredGoods.Count) return;
            var selectedItem = filteredGoods[itemIndex];
            if (selectedItem.Count > 1)
            {
                player.Create<Option>(Option.Types.BuyOrder, player, selectedItem);
            }
            else
            {
                Life shopkeeper = character as Life;
                if (shopkeeper == null) return;
                Exchange.Buy.Do(player, shopkeeper, selectedItem, 1);
                player.OptionBackward();
            }
        }

        public static void BuyOrderConfirm(Player player, Ability target, int index)
        {
            if (target is Item item && player.Option != null && player.Option.Type == Option.Types.BuyOrder)
            {
                var buyOption = player.Content.Gets<Option>().FirstOrDefault(o => o.Type == Option.Types.Buy);
                if (buyOption?.Relates?.FirstOrDefault() is Life shopkeeper)
                {
                    int count = player.Option.Setting.SliderValue;
                    Exchange.Buy.Do(player, shopkeeper, item, count);
                    player.monitor.Fire(Logic.Option.Event.Refresh, player);
                    player.Remove<Option>();
                    player.OptionBackward();
                }
            }
        }

        public static List<Option.Item> Sell(Player player, Ability target)
        {
            var finalResults = new List<Option.Item>();
            var input = new Option.Item
            {
                type = Option.Item.Type.Filter,
                data = new Dictionary<string, string>
                {
                    { "PlaceholderText", "在此输入搜索物品..." },
                    { "Text", player.Option.Setting?.Filter ?? "" }
                }
            };

            var toggleGroup = new Option.Item
            {
                type = Option.Item.Type.ToggleGroup,
                data = new Dictionary<string, string>
                {
                    { "Text", player.Option.Setting?.ToggleGroupText ?? "" }
                }
            };

            finalResults.Add(input);
            finalResults.Add(toggleGroup);

            var items = Exchange.Sell.GetItemRange(player).Where(item => ShouldShowSellItem(player.Option, item)).ToList();

            foreach (var item in items)
            {
                finalResults.Add(new Option.Item
                {
                    type = Option.Item.Type.Button,
                    data = new Dictionary<string, string>
                    {
                        { "Text", $"{Text.Decorate.Item(item, player)} ({item.Count})" },
                        { "Action", $"SellItem_{item.GetHashCode()}" }
                    }
                });
            }
            return finalResults;
        }

        public static List<Option.Item> SellOrder(Player player, Ability target)
        {
            if (target is Item item)
            {
                var setting = player.Option.Setting;
                setting.data.raw[Option.Settings.Data.SliderMax] = Math.Max(1, item.Count);
                if (setting.SliderValue > item.Count)
                {
                    setting.data.raw[Option.Settings.Data.SliderValue] = Math.Min(item.Count, Math.Max(1, setting.SliderValue));
                }

                var slider = new Option.Item
                {
                    type = Option.Item.Type.Slider,
                    data = new Dictionary<string, string>
                    {
                        { "Text", "出售" },
                        { "SliderValues", JsonConvert.SerializeObject(new int[] { setting.SliderMin, setting.SliderValue, setting.SliderMax }) },
                        { "ValueColor", "WHT" }
                    }
                };
                var confirm = new Option.Item(Option.Item.Type.Confirm);
                return new List<Option.Item> { slider, confirm };
            }
            return new List<Option.Item>();
        }

        public static void SellClick(Player player, Ability target, int index)
        {
            var option = player.Option;
            if (option?.Relates?.FirstOrDefault() is Life shopkeeper)
            {
                var items = Exchange.Sell.GetItemRange(player).Where(item => ShouldShowSellItem(option, item)).ToList();
                int itemIndex = index - 2;
                if (itemIndex >= 0 && itemIndex < items.Count)
                {
                    var item = items[itemIndex];
                    if (item.Count > 1)
                    {
                        player.Create<Option>(Option.Types.SellOrder, player, item);
                    }
                    else
                    {
                        Exchange.Sell.Do(player, shopkeeper, item, 1);
                        player.OptionBackward();
                    }
                }
            }
        }

        public static void SellOrderConfirm(Player player, Ability target, int index)
        {
            if (target is Item item && player.Option != null && player.Option.Type == Option.Types.SellOrder)
            {
                var option = player.Content.Gets<Option>().FirstOrDefault(o => o.Type == Option.Types.Sell);
                if (option?.Relates?.FirstOrDefault() is Life shopkeeper)
                {
                    int count = player.Option.Setting.SliderValue;
                    Exchange.Sell.Do(player, shopkeeper, item, count);
                    player.monitor.Fire(Logic.Option.Event.Refresh, player);
                }
            }
        }

        public static List<Option.Item> DropOrder(Player player, Ability target)
        {
            if (target is Item item)
            {
                var setting = player.Option.Setting;
                setting.data.raw[Option.Settings.Data.SliderMax] = Math.Max(1, item.Count);
                if (setting.SliderValue > item.Count)
                {
                    setting.data.raw[Option.Settings.Data.SliderValue] = Math.Min(item.Count, Math.Max(1, setting.SliderValue));
                }

                var slider = new Option.Item
                {
                    type = Option.Item.Type.Slider,
                    data = new Dictionary<string, string>
                    {
                        { "Text", Text.Agent.Instance.Get((int)Domain.Operation.Type.Drop, player) },
                        { "SliderValues", JsonConvert.SerializeObject(new int[] { setting.SliderMin, setting.SliderValue, setting.SliderMax }) },
                        { "ValueColor", "WHT" }
                    }
                };
                var confirm = new Option.Item(Option.Item.Type.Confirm);
                return new List<Option.Item> { slider, confirm };
            }

            return new List<Option.Item>();
        }

        public static void DropOrderConfirm(Player player, Ability target, int index)
        {
            if (target is Item item && player.Option != null && player.Option.Type == Option.Types.DropOrder)
            {
                int count = player.Option.Setting.SliderValue;
                Exchange.Drop.Do(player, item, count);
                player.OptionBackward();
            }
        }

        private static bool ShouldShowBuyItem(Option option, Item item)
        {
            return Exchange.Give.ShouldShowItem(option, item, "Buy");
        }

        private static bool ShouldShowSellItem(Option option, Item item)
        {
            return Exchange.Give.ShouldShowItem(option, item, "Sell");
        }



        public static List<Option.Item> Mall(Player player, Ability target)
        {
            var results = new List<Option.Item>();
            
            // Search input
            var input = new Option.Item
            {
                type = Option.Item.Type.Filter,
                data = new Dictionary<string, string>
                {
                    { "PlaceholderText", Text.Agent.Instance.Get(Logic.Text.Labels.SearchProducts, player.Language) },
                    { "Text", player.Option?.Setting?.Filter ?? "" }
                }
            };
            results.Add(input);
            
            // Toggle group for mall types
            var toggleGroup = new Option.Item
            {
                type = Option.Item.Type.ToggleGroup,
                data = new Dictionary<string, string>
                {
                    { "Text", player.Option?.Setting?.ToggleGroupText ?? "" },
                    { "Options", GetMallTypeOptions(player) }
                }
            };
            results.Add(toggleGroup);
            
            // Get all mall configs and filter
            var mallConfigs = Logic.Config.Agent.Instance.Content.Gets<Logic.Config.Mall>()
                .Where(m => ShouldShowMallItem(player.Option, m, player))
                .ToList();
            
            // Show clear search button when filter is active but no results
            string currentFilter = player.Option?.Setting?.Filter ?? "";
            if (mallConfigs.Count == 0 && !string.IsNullOrEmpty(currentFilter))
            {
                string noResultsText = Text.Agent.Instance.Get(Logic.Text.Labels.NoSearchResults, player.Language);
                results.Add(new Option.Item(Option.Item.Type.Text, noResultsText));
                
                string clearSearchText = Text.Agent.Instance.Get(Logic.Text.Labels.ClearSearch, player.Language);
                results.Add(new Option.Item(Option.Item.Type.Button, 
                    ("Text", clearSearchText), 
                    ("Action", "ClearSearch")));
            }
            
            foreach (var mallConfig in mallConfigs)
            {
                string displayName = Text.Agent.Instance.Get(mallConfig.Name, player);
                
                results.Add(new Option.Item(Option.Item.Type.Button, 
                    ("Text", displayName), 
                    ("Action", $"Mall_{mallConfig.Id}")));
            }
            
            return results;
        }
        
        private static string GetMallTypeOptions(Player player)
        {
            var types = new List<string>
            {
                Text.Agent.Instance.Get(Logic.Text.Labels.MallTypeSubscription, player.Language),
                Text.Agent.Instance.Get(Logic.Text.Labels.MallTypeExperience, player.Language),
                Text.Agent.Instance.Get(Logic.Text.Labels.MallTypeEquipment, player.Language),
                Text.Agent.Instance.Get(Logic.Text.Labels.MallTypeOther, player.Language),
                Text.Agent.Instance.Get(Logic.Text.Labels.MallTypePack, player.Language),
            };
            return string.Join(",", types);
        }
        
        private static bool ShouldShowMallItem(Option option, Logic.Config.Mall mallConfig, Player player)
        {
            if (option?.Setting == null) return true;
            
            // Filter by search text
            string filter = option.Setting.Filter;
            if (!string.IsNullOrEmpty(filter))
            {
                string name = Text.Agent.Instance.Get(mallConfig.Name, player);
                if (!name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            
            // Filter by toggle group (mall types)
            var toggleGroup = option.Setting.ToggleGroup;
            if (toggleGroup != null && toggleGroup.Count > 0)
            {
                // Check if "All" (empty key) is selected - if so, show all items
                if (toggleGroup.TryGetValue("", out bool allSelected) && allSelected)
                {
                    return true;
                }
                
                // Check if any specific type is selected
                bool anySelected = toggleGroup.Values.Any(v => v);
                if (anySelected)
                {
                    string typeName = GetMallTypeName(mallConfig.Type, player);
                    if (!toggleGroup.TryGetValue(typeName, out bool selected) || !selected)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        private static string GetMallTypeName(Logic.Config.Mall.Types type, Player player)
        {
            return type switch
            {
                Logic.Config.Mall.Types.Subscription => Text.Agent.Instance.Get(Logic.Text.Labels.MallTypeSubscription, player.Language),
                Logic.Config.Mall.Types.Experience => Text.Agent.Instance.Get(Logic.Text.Labels.MallTypeExperience, player.Language),
                Logic.Config.Mall.Types.Equipment => Text.Agent.Instance.Get(Logic.Text.Labels.MallTypeEquipment, player.Language),
                Logic.Config.Mall.Types.Pack => Text.Agent.Instance.Get(Logic.Text.Labels.MallTypePack, player.Language),
                _ => Text.Agent.Instance.Get(Logic.Text.Labels.MallTypeOther, player.Language),
            };
        }
        
        public static List<Option.Item> CardInput(Player player, Ability target)
        {
            return Mall(player, target);
        }

        public static void MallClick(Player player, Ability target, int index)
        {
            // index 0 = Filter, index 1 = ToggleGroup, index 2+ = items (or clear search button when no results)
            int mallIndex = index - 2;
            if (mallIndex < 0) return;
            
            var mallConfigs = Logic.Config.Agent.Instance.Content.Gets<Logic.Config.Mall>()
                .Where(m => ShouldShowMallItem(player.Option, m, player))
                .ToList();
            
            // Handle clear search button (shown when filter active but no results)
            string currentFilter = player.Option?.Setting?.Filter ?? "";
            if (mallConfigs.Count == 0 && !string.IsNullOrEmpty(currentFilter))
            {
                // Clear filter when any item in the "no results" area is clicked
                player.Option.Setting.Filter = "";
                player.monitor.Fire(Logic.Option.Event.Refresh, player);
                return;
            }
            
            if (mallIndex >= mallConfigs.Count) return;
            
            var mallConfig = mallConfigs[mallIndex];
            
            // All items go through the order confirmation panel
            OpenMallOrderPanel(player, mallConfig);
        }
        
        private static void OpenMallOrderPanel(Player player, Logic.Config.Mall mallConfig)
        {
            var existingSetting = player.Content.Get<Option.Settings>(s => s.Type == Option.Types.MallOrder);
            if (existingSetting == null)
            {
                existingSetting = player.Create<Option.Settings>(Option.Types.MallOrder, player, player);
            }
            existingSetting.data.Change(Option.Settings.Data.MallId, mallConfig.Id);
            player.Create<Option>(Option.Types.MallOrder, player, player);
        }
        
        public static List<Option.Item> MallOrder(Player player, Ability target)
        {
            var results = new List<Option.Item>();
            var setting = player.Option?.Setting;
            if (setting == null) return results;
            
            int mallId = setting.data.Get<int>(Option.Settings.Data.MallId);
            if (mallId == 0) return results;
            
            var mallConfig = Logic.Config.Agent.Instance.Content.Get<Logic.Config.Mall>(m => m.Id == mallId);
            if (mallConfig == null) return results;
            
            int maxBuyable = Domain.Mall.Agent.Instance.GetMaxBuyable(player, mallConfig);
            
            setting.data.raw[Option.Settings.Data.SliderMin] = 1;
            setting.data.raw[Option.Settings.Data.SliderMax] = Math.Max(1, maxBuyable);
            if (setting.SliderValue < 1) setting.SliderValue = 1;
            if (setting.SliderValue > maxBuyable) setting.SliderValue = maxBuyable;
            
            int totalPrice = mallConfig.Price * setting.SliderValue;
            string buyLabel = Text.Agent.Instance.Get(Logic.Text.Labels.BuyLabel, player.Language);
            string totalLabel = Text.Agent.Instance.Get(Logic.Text.Labels.Total, player.Language);
            string gemLabel = Text.Agent.Instance.Get(Logic.Text.Labels.Gem, player.Language);
            
            var slider = new Option.Item
            {
                type = Option.Item.Type.Slider,
                data = new Dictionary<string, string>
                {
                    { "Text", buyLabel },
                    { "SliderValues", JsonConvert.SerializeObject(new int[] { setting.SliderMin, setting.SliderValue, setting.SliderMax }) },
                    { "ValueColor", "WHT" }
                }
            };
            
            results.Add(slider);
            results.Add(new Option.Item(Option.Item.Type.Text, $"{totalLabel}: {totalPrice} {gemLabel}"));
            results.Add(new Option.Item(Option.Item.Type.Confirm));
            
            return results;
        }

        public static void MallOrderConfirm(Player player, Ability target, int index)
        {
            if (player.Option?.Type != Option.Types.MallOrder) return;
            
            var setting = player.Option.Setting;
            int mallId = setting.data.Get<int>(Option.Settings.Data.MallId);
            var mallConfig = Logic.Config.Agent.Instance.Content.Get<Logic.Config.Mall>(m => m.Id == mallId);
            if (mallConfig == null) return;
            
            int count = setting.SliderValue;
            bool success = Domain.Mall.Purchase.Do(player, mallConfig, count, out var reason);
            if (!success)
            {
                string tip = reason switch
                {
                    Domain.Mall.Purchase.FailReason.NoGem or Domain.Mall.Purchase.FailReason.InsufficientGem 
                        => Domain.Text.Agent.Instance.Get(Logic.Text.Labels.MallInsufficientGem, player.Language),
                    Domain.Mall.Purchase.FailReason.ExceedMax 
                        => Domain.Text.Agent.Instance.Get(Logic.Text.Labels.MallLimitReached, player.Language),
                    Domain.Mall.Purchase.FailReason.CannotReceive 
                        => Domain.Text.Agent.Instance.Get(Logic.Text.Labels.MallCannotReceive, player.Language),
                    _ => ""
                };
                if (!string.IsNullOrEmpty(tip))
                {
                    Net.Tcp.Instance.Send(player, new Net.Protocol.FlyTip(tip));
                }
                return;
            }
            
            player.OptionBackward();
        }

        private static void HandleContainerItemClick(Player player, string action, Ability containerTarget)
        {
            if (containerTarget is not Logic.Item container)
            {
                return;
            }

            if (!action.StartsWith("ContainerItem_"))
            {
                return;
            }

            string hashCodeStr = action.Substring("ContainerItem_".Length);
            if (!int.TryParse(hashCodeStr, out int targetHashCode))
            {
                return;
            }

            var containerItems = container.Content.Gets<Logic.Item>();
            var targetItem = containerItems.FirstOrDefault(item => item.GetHashCode() == targetHashCode);

            if (targetItem == null)
            {
                return;
            }

            Click.Character.On(player, targetItem);
        }
    }
}
