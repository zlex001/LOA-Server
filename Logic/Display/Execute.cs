using Data;
using Newtonsoft.Json;

namespace Logic.Display
{
    public class Execute
    {
        public static void OptionButton(global::Data.Player player, int side, int index)
        {
            if (player?.Option != null)
            {
                var target = player.Option.Relates.FirstOrDefault();
                if (target != null)
                {
                    Enum actualType = side switch
                    {
                        0 => Enum.Parse<global::Data.Option.LeftPanel>(player.Option.Type.ToString()),
                        1 => Enum.Parse<global::Data.Option.RightPanel>(player.Option.Type.ToString()),
                        _ => throw new ArgumentOutOfRangeException(nameof(side), $"未知的 side：{side}")
                    };

                    Agent.Instance.Execute(player, actualType, target, index);
                    player.monitor.Fire(global::Data.Option.Event.Refresh, player);
                }
            }
        }

        public static void OptionConfirm(global::Data.Player player, int side, int index)
        {
            if (player.Option != null && player.Option.Setting != null)
            {
                if (player.Option.Setting.SliderValue >= player.Option.Setting.SliderMin &&
                    player.Option.Setting.SliderValue <= player.Option.Setting.SliderMax)
                {
                    var target = player.Option.Relates.FirstOrDefault();
                    if (target != null)
                    {
                        Enum panel = side switch
                        {
                            0 => Enum.Parse<global::Data.Option.LeftPanel>(player.Option.Type.ToString()),
                            1 => Enum.Parse<global::Data.Option.RightPanel>(player.Option.Type.ToString()),
                            _ => throw new ArgumentOutOfRangeException(nameof(side), $"未知的 side：{side}")
                        };

                        Agent.Instance.ExecuteConfirm(player, panel, target, index);
                    }

                    player.monitor.Fire(global::Data.Option.Event.Refresh, player);
                }
            }
        }

        public static void NetworkOptionButton(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[0];
            int side = (int)args[1];
            int index = (int)args[2];
            OptionButton(player, side, index);
        }

        public static void NetworkOptionConfirm(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[0];
            int side = (int)args[1];
            int index = (int)args[2];
            OptionConfirm(player, side, index);
        }

        public static void ShopRightPanelClick(Player player, Ability target, int index)
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

        public static void LeftPanelClick(Player player, Ability target, int index)
        {
            var leftItems = Agent.Instance.Create(player, global::Data.Option.LeftPanel.Operation, target);

            if (leftItems == null || index < 0 || index >= leftItems.Count)
            {
                return;
            }

            var clickedItem = leftItems[index];

            if (!clickedItem.data.TryGetValue("Action", out var actionValue))
            {
                return;
            }

            if (actionValue.ToString().StartsWith("EquipmentSlot_"))
            {
                EquipmentSlotClick(player, clickedItem, target);
            }
            else if (actionValue.ToString().StartsWith("ContainerItem_"))
            {
                ContainerItemClick(player, actionValue.ToString(), target);
            }
        }

        public static void EquipmentSlotClick(Player player, Option.Item clickedItem, Ability target)
        {
            if (target is not Life targetLife)
            {
                return;
            }

            if (!clickedItem.data.TryGetValue("PartIndex", out var partIndexStr) ||
                !int.TryParse(partIndexStr, out var partIndex))
            {
                return;
            }

            var parts = targetLife.Content.Gets<Part>().OrderBy(p => (int)p.Type).ToList();
            if (partIndex >= parts.Count)
            {
                return;
            }

            var part = parts[partIndex];
            var partName = Text.Agent.Instance.Get((global::Data.Text.Labels)part.Type, player);
            var equippedItem = part.Content.Gets<global::Data.Item>().FirstOrDefault();

            if (equippedItem != null)
            {
                Click.Character.On(player, equippedItem);
            }

        }

        public static void RightPanelClick(Player player, Ability target, int index)
        {
            var logicOption = player.Option;
            if (logicOption == null || logicOption.Relates.Count == 0)
            {
                return;
            }

            // 所有右侧面板点击现在都通过统一的 RightPanel 注册处理器处理
            // 不再需要对 Sign 进行特殊处理，因为已在 Signal.cs 中注册了 RightPanel.Sign
            var buttonItems = Agent.Instance.Create(player, global::Data.Option.RightPanel.Operation, logicOption.Relates.First());

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
                ContainerItemClick(player, actionValue.ToString(), logicOption.Relates.First());
                return;
            }

            if (Enum.TryParse(typeof(Operation.Type), actionValue.ToString(), out var actionEnum))
            {
                Agent.Instance.Execute(player, (Enum)actionEnum, logicOption.Relates.First(), 0);
                return;
            }
        }

        public static void ContainerItemClick(Player player, string action, Ability containerTarget)
        {
            if (containerTarget is not global::Data.Item container)
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

            var containerItems = container.Content.Gets<global::Data.Item>();
            var targetItem = containerItems.FirstOrDefault(item => item.GetHashCode() == targetHashCode);

            if (targetItem == null)
            {
                return;
            }

            Click.Character.On(player, targetItem);
        }
    }
}


