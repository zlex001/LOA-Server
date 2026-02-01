using Logic;
using System.Text.RegularExpressions;

namespace Domain.Display
{
    public class Agent : Agent<Agent>
    {

        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }
        private Dictionary<string, EquipmentChangeListener> equipmentChangeListeners = new Dictionary<string, EquipmentChangeListener>();
        private Dictionary<string, ContainerChangeListener> containerChangeListeners = new Dictionary<string, ContainerChangeListener>();
        private Dictionary<Type, Basic.Monitor.Object> information = new Dictionary<Type, Basic.Monitor.Object>();
        private Dictionary<Enum, Func<Player, Ability, bool>> shouldShowHandlers = new Dictionary<Enum, Func<Player, Ability, bool>>();
        private Dictionary<Enum, Func<Player, Ability, List<Option.Item>>> buildHandlers = new Dictionary<Enum, Func<Player, Ability, List<Option.Item>>>();
        private Dictionary<Enum, Action<Player, Ability, int>> clickHandlers = new Dictionary<Enum, Action<Player, Ability, int>>();
        private Dictionary<Enum, Action<Player, Ability, int>> confirmHandlers = new Dictionary<Enum, Action<Player, Ability, int>>();
        private Dictionary<Enum, Action<Player, Ability, string>> inputHandlers = new Dictionary<Enum, Action<Player, Ability, string>>();
        private Dictionary<Enum, Action<Player, Ability, int>> sliderHandlers = new Dictionary<Enum, Action<Player, Ability, int>>();
        private Dictionary<Enum, Action<Player, Ability, string>> filterHandlers = new Dictionary<Enum, Action<Player, Ability, string>>();
        private Dictionary<Enum, Action<Player, Ability, string, bool>> toggleHandlers = new Dictionary<Enum, Action<Player, Ability, string, bool>>();
        private Dictionary<Enum, Action<Player, Ability, int>> amountHandlers = new Dictionary<Enum, Action<Player, Ability, int>>();
        
        private int _displayRefreshDepth = 0;
        private const int MaxDisplayRefreshDepth = 10;
        
        public void Init()
        {
            RegisterInformation(typeof(Item), Information.Item);
            RegisterInformation(typeof(Life), Information.Life);
            RegisterInformation(typeof(Player), Information.Player);
            Register(Option.LeftPanel.Operation, build: Left.Operation, onClick:Display.Execute.LeftPanelClick);
            Register(Option.RightPanel.Operation, build: Right.Operation, onClick: Right.OperationClick);

            Register(Logic.Option.LeftPanel.Teleport, build: Left.Teleport);
            Register(Logic.Option.RightPanel.Teleport, build: Right.Teleport, onClick: Right.TeleportClick);

            Register(Option.LeftPanel.Shop, build: Left.Shop);
            Register(Option.RightPanel.Shop, build: Right.Shop, onClick: Display.Execute.ShopRightPanelClick);

            Register(Option.LeftPanel.PickOrder, build: Left.PickOrder);
            Register(Option.RightPanel.PickOrder, build: Right.PickOrder, onConfirm: Right.PickOrderConfirm);

            Register(Option.LeftPanel.Give, build: Left.Give);
            Register(Option.RightPanel.Give, build: Right.Give, onClick: Right.GiveClick);
            Register(Option.LeftPanel.GiveOrder, build: Left.GiveOrder);
            Register(Option.RightPanel.GiveOrder, build: Right.GiveOrder, onConfirm: Right.GiveOrderConfirm);

            Register(Option.LeftPanel.Buy, build: Left.Buy);
            Register(Option.RightPanel.Buy, build: Right.Buy, onClick: Right.BuyClick);
            Register(Option.LeftPanel.BuyOrder, build: Left.BuyOrder);
            Register(Option.RightPanel.BuyOrder, build: Right.BuyOrder, onConfirm: Right.BuyOrderConfirm);

            Register(Option.LeftPanel.Sell, build: Left.Sell);
            Register(Option.RightPanel.Sell, build: Right.Sell, onClick: Right.SellClick);
            Register(Option.LeftPanel.SellOrder, build: Left.SellOrder);
            Register(Option.RightPanel.SellOrder, build: Right.SellOrder, onConfirm: Right.SellOrderConfirm);

            Register(Option.LeftPanel.DropOrder, build: Left.DropOrder);
            Register(Option.RightPanel.DropOrder, build: Right.DropOrder, onConfirm: Right.DropOrderConfirm);

            Register(Option.LeftPanel.Mall, build: Left.Mall, onClick: Left.MallClick);
            Register(Option.RightPanel.Mall, build: Right.Mall, onClick: Right.MallClick);
            Register(Option.LeftPanel.CardInput, build: Left.CardInput, onConfirm: Left.CardInputConfirm);
            Register(Option.RightPanel.CardInput, build: Right.CardInput);
            Register(Option.LeftPanel.MallOrder, build: Left.MallOrder);
            Register(Option.RightPanel.MallOrder, build: Right.MallOrder, onConfirm: Right.MallOrderConfirm);

            Net.Manager.Instance.monitor.Register(Net.Manager.Event.OptionButton, Display.Execute.NetworkOptionButton);
            Net.Manager.Instance.monitor.Register(Net.Manager.Event.OptionConfirm, Display.Execute.NetworkOptionConfirm);

            Logic.Agent.Instance.Content.Add.Register(typeof(Player), OnAddPlayer);
            Logic.Agent.Instance.Content.Remove.Register(typeof(Player), OnUnbundlePlayer);
            Logic.Agent.Instance.Content.Add.Register(typeof(Option), OnPlayerAddOption);
            Logic.Agent.Instance.Content.Remove.Register(typeof(Option), OnPlayerRemoveOption);
            Logic.Agent.Instance.Content.Add.Register(typeof(Life), OnAddLife);
        }

        public void RegisterInformation(Type type, Basic.Monitor.Object handler)
        {
            if (information.ContainsKey(type))
            {
                return;
            }

            information[type] = handler;
        }

        public object[] GetInformation(Ability element, params object[] args)
        {
            var type = element.GetType();
            Basic.Monitor.Object handler = null;

            if (!information.TryGetValue(type, out handler))
            {
                Type baseType = type.BaseType;
                while (handler == null && baseType != null && baseType != typeof(object))
                {
                    if (information.TryGetValue(baseType, out handler))
                        break;
                    baseType = baseType.BaseType;
                }

                if (handler == null)
                {
                    foreach (var iface in type.GetInterfaces())
                    {
                        if (information.TryGetValue(iface, out handler))
                            break;
                    }
                }
            }
            if (handler != null)
            {
                return handler(args);
            }
            var list = new List<object>();
            if (args.Length > 0)
            {
                list.AddRange(args);

            }
            else
            {
                list.Add(new Option.Item(element.GetType().Name));
            }
            return list.ToArray();
        }

        public Net.Protocol.Option CreateOptionProtocol(Logic.Option option)
        {
            var info = (Logic.Option.LeftPanel)Enum.Parse(typeof(Logic.Option.LeftPanel), option.Type.ToString());
            var oper = (Logic.Option.RightPanel)Enum.Parse(typeof(Logic.Option.RightPanel), option.Type.ToString());

            var target = option.Relates.FirstOrDefault();
            if (target == null)
            {
                Utils.Debug.Log.Info("OPTION", $"[CreateOptionProtocol] Relates is empty, returning empty Option. Type={option.Type}, RelatesCount={option.Relates.Count}");
                return new Net.Protocol.Option();
            }
            Utils.Debug.Log.Info("OPTION", $"[CreateOptionProtocol] Type={option.Type}, target={target.GetType().Name}, RelatesCount={option.Relates.Count}");

            var left = Create(option.Player, info, target);
            var right = Create(option.Player, oper, target);

            var protocol = new Net.Protocol.Option();
            if (left != null && left.Count > 0)
            {
                protocol.lefts = left;
            }
            if (right != null && right.Count > 0)
            {
                protocol.rights = right;
            }

            return protocol;
        }

        public void Register(Enum type, Func<Player, Ability, bool> shouldShow = null, Func<Player, Ability, List<Option.Item>> build = null, Action<Player, Ability, int> onClick = null, Action<Player, Ability, int> onConfirm = null, Action<Player, Ability, string> onInput = null, Action<Player, Ability, int> onSlider = null, Action<Player, Ability, string> onFilter = null, Action<Player, Ability, string, bool> onToggle = null, Action<Player, Ability, int> onAmount = null)
        {
            if (shouldShow != null) shouldShowHandlers[type] = shouldShow;
            if (build != null) buildHandlers[type] = build;
            if (onClick != null) clickHandlers[type] = onClick;
            if (onConfirm != null) confirmHandlers[type] = onConfirm;
            if (onInput != null) inputHandlers[type] = onInput;
            if (onSlider != null) sliderHandlers[type] = onSlider;
            if (onFilter != null) filterHandlers[type] = onFilter;
            if (onToggle != null) toggleHandlers[type] = onToggle;
            if (onAmount != null) amountHandlers[type] = onAmount;
        }

        public List<Logic.Option.Item> Create(Logic.Player player, Enum type, Logic.Ability target)
        {
            if (buildHandlers.TryGetValue(type, out var handler))
            {
                return handler(player, target);
            }

            return new List<Logic.Option.Item>();
        }

        public void Execute(Logic.Player player, Enum type, Logic.Ability target, int index)
        {
            if (clickHandlers.TryGetValue(type, out var handler))
            {
                handler(player, target, index);
            }
        }

        public void ExecuteConfirm(Logic.Player player, Enum type, Logic.Ability target, int index)
        {
            if (confirmHandlers.TryGetValue(type, out var handler))
                handler(player, target, index);
        }
        public List<Logic.Option.Item> Generate(Logic.Player player, Logic.Ability target)
        {
            var results = new List<Logic.Option.Item>();

            foreach (var kvp in shouldShowHandlers)
            {
                var type = kvp.Key;
                var shouldShow = kvp.Value;

                if (shouldShow(player, target) && buildHandlers.TryGetValue(type, out var buildHandler))
                {
                    var items = buildHandler(player, target);
                    if (items != null)
                        results.AddRange(items);
                }
            }

            return results;
        }
        private void OnAddPlayer(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[1];
            
            Register(player.data.after, Life.Data.Mp, player, OnAfterPlayerMp);
            Register(player.data.after, Life.Data.Lp, player, OnAfterPlayerLp);
            Register(player.data.before, Life.Data.Exp, player, OnBeforePlayerExp);
            player.data.before.Register(Player.Data.Viewer, OnPlayerViewerChanging);
            player.data.after.Register(Player.Data.Viewer, OnPlayerViewerChanged);
            
            player.monitor.Register(Logic.Option.Event.Refresh, OnRefresh);

            player.Content.Add.Register(typeof(Logic.Option), OnPlayerAddOption);
            player.Content.Remove.Register(typeof(Logic.Option), OnPlayerRemoveOption);
            player.Content.Add.Register(typeof(Option.Settings), OnPlayerAddOptionSettings);
        }
        private void OnUnbundlePlayer(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[1];
            
            foreach (var part in player.Content.Gets<Part>())
            {
                Unregister(part.data.after, Logic.Part.Data.Hp);
            }
            
            Unregister(player.data.after, Life.Data.Mp);
            Unregister(player.data.after, Life.Data.Lp);
            Unregister(player.data.before, Life.Data.Exp);
            player.data.before.Unregister(Player.Data.Viewer, OnPlayerViewerChanging);
            player.data.after.Unregister(Player.Data.Viewer, OnPlayerViewerChanged);
            player.monitor.Unregister(Option.Event.Refresh, OnRefresh);

            player.Content.Remove.Unregister(typeof(Logic.Option), OnPlayerRemoveOption);
            player.Content.Add.Unregister(typeof(Logic.Option), OnPlayerAddOption);

            if (player.Viewer != null)
            {
                UnlistenTargetLife(player, player.Viewer);
            }

            UnlistenAllContainersForPlayer(player);
        }
        public void Refresh(Player player)
        {
            if (player.Option == null)
            {
                Utils.Debug.Log.Info("OPTION", "[Refresh] player.Option is null, skipping send (option already closed)");
                return;
            }
            
            Utils.Debug.Log.Info("OPTION", $"[Refresh] Sending option update, Type={player.Option.Type}");
            var protocol = CreateOptionProtocol(player.Option);
            Net.Tcp.Instance.Send(player, protocol);
        }
        private void OnRefresh(params object[] args)
        {
            Player player = (Player)args[0];
            Refresh(player);
        }

        private void OnAfterPlayerMp(Logic.Player player, object[] args)
        {
            int v = (int)args[0];
            string color = Utils.Text.GetDangerRatioColor((double)player.Mp / player.data.GetMax<int>(Logic.Life.Data.Mp));
            Net.Tcp.Instance.Send(player, new Net.Protocol.DataPair(Net.Protocol.DataPair.Type.Mp, [player.Mp, (int)player.MaxMp], color));
        }
        private void OnAfterPlayerLp(Logic.Player player, object[] args)
        {
            string color = Utils.Text.GetDangerRatioColor(player.Lp / player.data.GetMax<double>(Logic.Life.Data.Lp));
            Net.Tcp.Instance.Send(player, new Net.Protocol.DataPair(Net.Protocol.DataPair.Type.Lp, [(int)player.Lp, (int)player.MaxLp], color));
        }
        
        private void OnBeforePlayerExp(Logic.Player player, object[] args)
        {
            if (player.Option == null) return;
            
            int oldExp = (int)args[0];
            int newExp = (int)args[1];
            
            if (oldExp != newExp)
            {
                Refresh(player);
            }
        }

        private void OnPlayerAddOption(params object[] args)
        {
            Player player = (Player)args[0];
            Option option = (Option)args[1];

            var protocol = CreateOptionProtocol(option);
            Net.Tcp.Instance.Send(player, protocol);

            if (option.Relates.Count > 0)
            {
                var target = option.Relates[0];
                if (target is Life targetLife && targetLife != player)
                {
                    player.Viewer = targetLife;
                }
                else if (target is Item targetItem && targetItem.Content.Has<Item>())
                {
                    // 当查看一个容器Item时，监听其内容变化
                    ListenTargetContainer(player, targetItem);
                    player.Viewer = null;
                }
                else
                {
                    player.Viewer = null;
                }
            }
            else
            {
                player.Viewer = null;
            }
        }

        private void OnPlayerRemoveOption(params object[] args)
        {
            Player player = (Player)args[0];
            Option removedOption = (Option)args[1];
            
            Utils.Debug.Log.Info("OPTION", $"[OnPlayerRemoveOption] Removed option Type={removedOption.Type}, player.Option={(player.Option == null ? "null" : player.Option.Type.ToString())}");
            
            // 清理所有容器监听器
            UnlistenAllContainersForPlayer(player);
            player.Viewer = null;
            
            // If this was the current option and no other options remain, send close signal
            if (player.Option == null)
            {
                Utils.Debug.Log.Info("OPTION", "[OnPlayerRemoveOption] Sending empty Option protocol to close UI");
                Net.Tcp.Instance.Send(player, new Net.Protocol.Option());
            }
        }

        private void ListenTargetLife(Player viewer, Life target)
        {
            foreach (var part in target.Content.Gets<Part>())
            {
                Register(part.data.before, Logic.Part.Data.Hp, viewer, OnBeforeTargetPartHpChanged);
            }
            
            Register(target.data.before, Life.Data.Mp, viewer, OnBeforeTargetLifeDataChanged);
            Register(target.data.before, Life.Data.Lp, viewer, OnBeforeTargetLifeDataChanged);
            Register(target.data.before, Life.Data.Exp, viewer, OnBeforeTargetLifeDataChanged);

            var targetLifeKey = $"viewer_{viewer.GetHashCode()}_target_{target.GetHashCode()}";
            if (!equipmentChangeListeners.ContainsKey(targetLifeKey))
            {
                equipmentChangeListeners[targetLifeKey] = new EquipmentChangeListener(viewer, target);
            }
        }

        private void ListenTargetContainer(Player viewer, Item container)
        {
            var containerKey = $"viewer_{viewer.GetHashCode()}_container_{container.GetHashCode()}";
            if (!containerChangeListeners.ContainsKey(containerKey))
            {
                containerChangeListeners[containerKey] = new ContainerChangeListener(viewer, container);
            }
        }

        private void UnlistenTargetLife(Player viewer, Life target)
        {
            foreach (var part in target.Content.Gets<Part>())
            {
                Unregister(part.data.before, Logic.Part.Data.Hp);
            }
            
            Unregister(target.data.before, Life.Data.Mp);
            Unregister(target.data.before, Life.Data.Lp);
            Unregister(target.data.before, Life.Data.Exp);
            
            var targetLifeKey = $"viewer_{viewer.GetHashCode()}_target_{target.GetHashCode()}";
            if (equipmentChangeListeners.ContainsKey(targetLifeKey))
            {
                equipmentChangeListeners[targetLifeKey].Dispose();
                equipmentChangeListeners.Remove(targetLifeKey);
            }
        }

        private void UnlistenAllContainersForPlayer(Player viewer)
        {
            var keysToRemove = containerChangeListeners.Keys
                .Where(key => key.StartsWith($"viewer_{viewer.GetHashCode()}_container_"))
                .ToList();

            foreach (var key in keysToRemove)
            {
                containerChangeListeners[key].Dispose();
                containerChangeListeners.Remove(key);
            }
        }

        private void OnBeforeTargetPartHpChanged(Player viewer, object[] args)
        {
            if (viewer.Option == null) return;
            Refresh(viewer);
        }

        private void OnBeforeTargetLifeDataChanged(Player viewer, object[] args)
        {
            if (viewer.Option == null) return;

            int oldInt = Utils.Mathematics.AsInt(args[0]);
            int newInt = Utils.Mathematics.AsInt(args[1]);

            if (oldInt != newInt)
            {
                Refresh(viewer);
            }
        }

        private void OnPlayerViewerChanging(object[] args)
        {
            Life previous = (Life)args[0];
            Life target = (Life)args[1];
            Player player = (Player)args[2];
            if (previous != null && previous != player)
            {
                UnlistenTargetLife(player, previous);
            }
        }

        private void OnPlayerViewerChanged(object[] args)
        {
            Life target = (Life)args[0];
            Player player = (Player)args[1];

            if (target != null && target != player)
            {
                ListenTargetLife(player, target);
            }

            if (player.Option != null)
            {
                Refresh(player);
            }
        }
        private void OnPlayerAddOptionSettings(params object[] args)
        {
            Player player = (Player)args[0];
            Option.Settings settings = (Option.Settings)args[1];

            if (settings.Type == Option.Types.Buy && settings.Relates.Count > 0 && settings.Relates[0] is Life shopkeeper)
            {
                var shop = Infrastructure.Agent.GetShop(shopkeeper);
                if (shop != null)
                {
                    var goods = shop.GetGoods(shopkeeper.Map);
                    var availableTypes = goods
                        .Select(item => item.Type.ToString())
                        .Distinct()
                        .Prepend("")
                        .ToList();

                    settings.data.raw[Option.Settings.Data.ToggleGroup] = availableTypes.ToDictionary(v => v, v => v == "");
                    settings.data.raw[Option.Settings.Data.Filter] = "";
                }
            }
            else if (new[] { Option.Types.BuyOrder, Option.Types.SellOrder, Option.Types.DropOrder, Option.Types.PickOrder, Option.Types.GiveOrder }.Contains(settings.Type) 
                && settings.Relates.Count > 0 && settings.Relates[0] is Item item)
            {
                settings.data.raw[Option.Settings.Data.SliderMin] = 1;
                settings.data.raw[Option.Settings.Data.SliderMax] = Math.Max(1, item.Count);
                if (settings.SliderValue < settings.SliderMin) { settings.SliderValue = settings.SliderMin; }
                if (settings.SliderValue > settings.SliderMax) { settings.SliderValue = settings.SliderMax; }
            }
            else if (settings.Type == Option.Types.Sell)
            {
                List<string> types = Exchange.Sell.GetItemRange(player).Select(s => s.Type.ToString()).Distinct().Prepend("").ToList();
                settings.data.raw[Option.Settings.Data.ToggleGroup] = types.ToDictionary(v => v, v => v == "");
                settings.data.raw[Option.Settings.Data.Filter] = "";
            }
            else if (settings.Type == Option.Types.Mall)
            {
                var types = new List<string>
                {
                    Text.Agent.Instance.Get(Logic.Text.Labels.MallTypeSubscription, player.Language),
                    Text.Agent.Instance.Get(Logic.Text.Labels.MallTypeExperience, player.Language),
                    Text.Agent.Instance.Get(Logic.Text.Labels.MallTypeEquipment, player.Language),
                    Text.Agent.Instance.Get(Logic.Text.Labels.MallTypeOther, player.Language),
                    Text.Agent.Instance.Get(Logic.Text.Labels.MallTypePack, player.Language),
                }.Where(s => !string.IsNullOrEmpty(s)).Distinct().Prepend("").ToList();
                settings.data.raw[Option.Settings.Data.ToggleGroup] = types.ToDictionary(v => v, v => v == "");
                settings.data.raw[Option.Settings.Data.Filter] = "";
            }
        }

        private void OnAddLife(params object[] args)
        {
            Life life = (Life)args[1];
            life.data.after.Register(Life.Data.Action, OnLifeActionChanged);
            Register(life.State.monitor, Basic.State<Life.States>.Event.Changed, life, OnLifeStateChanged);
        }

        private void OnLifeActionChanged(params object[] args)
        {
            Life life = (Life)args[1];
            if (life.Map == null) return;

            if (_displayRefreshDepth >= MaxDisplayRefreshDepth)
            {
                Utils.Debug.Log.Fatal($"[DISPLAY] Display refresh recursion depth exceeded! Current: {_displayRefreshDepth}, Life: {life.GetType().Name}, Action: {life.Action}");
                return;
            }

            _displayRefreshDepth++;
            try
            {
                var players = life.Map.Content.Gets<Player>();
                foreach (var player in players)
                {
                    var data = GetCharactersForDisplay(player);
                    Net.Tcp.Instance.Send(player, new Net.Protocol.BattleProgress(data));
                }
            }
            finally
            {
                _displayRefreshDepth--;
            }
        }

        private void OnLifeStateChanged(Life life, object[] args)
        {
            if (life.Map == null) return;

            if (_displayRefreshDepth >= MaxDisplayRefreshDepth)
            {
                Utils.Debug.Log.Fatal($"[DISPLAY] Display refresh recursion depth exceeded! Current: {_displayRefreshDepth}, Life: {life.GetType().Name}, State: {life.State.Current}");
                return;
            }

            _displayRefreshDepth++;
            try
            {
                var players = life.Map.Content.Gets<Player>();
                foreach (var player in players)
                {
                    var data = GetCharactersForDisplay(player);
                    Net.Tcp.Instance.Send(player, new Net.Protocol.Characters(data));
                    
                    if (player.Option != null && player.Option.Relates.Contains(life))
                    {
                        Refresh(player);
                    }
                }
            }
            finally
            {
                _displayRefreshDepth--;
            }
        }

        private class EquipmentChangeListener : IDisposable
        {
            private readonly Player viewer;
            private readonly Life target;
            private readonly List<Part> parts;
            private bool disposed = false;

            public EquipmentChangeListener(Player viewer, Life target)
            {
                this.viewer = viewer;
                this.target = target;
                this.parts = target.Content.Gets<Part>().ToList();

                foreach (var part in parts)
                {
                    part.Content.Add.Register(typeof(Item), OnPartEquipmentAdded);
                    part.Content.Remove.Register(typeof(Item), OnPartEquipmentRemoved);
                }
            }

            private void OnPartEquipmentAdded(params object[] args)
            {
                if (!disposed && viewer.Option != null)
                {
                    Agent.Instance.Refresh(viewer);
                }
            }

            private void OnPartEquipmentRemoved(params object[] args)
            {
                if (!disposed && viewer.Option != null)
                {
                    Agent.Instance.Refresh(viewer);
                }
            }

            public void Dispose()
            {
                if (!disposed)
                {
                    foreach (var part in parts)
                    {
                        part.Content.Add.Unregister(typeof(Item), OnPartEquipmentAdded);
                        part.Content.Remove.Unregister(typeof(Item), OnPartEquipmentRemoved);
                    }


                    disposed = true;
                }
            }
        }
        private class ContainerChangeListener : IDisposable
        {
            private readonly Player viewer;
            private readonly Item container;
            private bool disposed = false;

            public ContainerChangeListener(Player viewer, Item container)
            {
                this.viewer = viewer;
                this.container = container;

                // 监听容器内Item的添加和移除
                container.Content.Add.Register(typeof(Item), OnContainerItemAdded);
                container.Content.Remove.Register(typeof(Item), OnContainerItemRemoved);

                // 监听容器内现有Item的数量变化
                var containerItems = container.Content.Gets<Item>();
                foreach (var item in containerItems)
                {
                    item.data.after.Register(Logic.Item.Data.Count, OnContainerItemCountChanged);
                }
            }

            private void OnContainerItemAdded(params object[] args)
            {
                if (!disposed && viewer.Option != null && viewer.Option.Relates.Contains(container))
                {
                    var addedItem = (Item)args[1];
                    // 监听新加入Item的数量变化
                    addedItem.data.after.Register(Logic.Item.Data.Count, OnContainerItemCountChanged);
                    Agent.Instance.Refresh(viewer);
                }
            }

            private void OnContainerItemRemoved(params object[] args)
            {
                if (!disposed && viewer.Option != null && viewer.Option.Relates.Contains(container))
                {
                    var removedItem = (Item)args[1];
                    // 取消对移除Item的监听
                    removedItem.data.after.Unregister(Logic.Item.Data.Count, OnContainerItemCountChanged);
                    Agent.Instance.Refresh(viewer);
                }
            }

            private void OnContainerItemCountChanged(params object[] args)
            {
                if (!disposed && viewer.Option != null && viewer.Option.Relates.Contains(container))
                {
                    Agent.Instance.Refresh(viewer);
                }
            }

            public void Dispose()
            {
                if (!disposed)
                {
                    container.Content.Add.Unregister(typeof(Item), OnContainerItemAdded);
                    container.Content.Remove.Unregister(typeof(Item), OnContainerItemRemoved);

                    // 取消对所有容器内Item数量变化的监听
                    var containerItems = container.Content.Gets<Item>();
                    foreach (var item in containerItems)
                    {
                        item.data.after.Unregister(Logic.Item.Data.Count, OnContainerItemCountChanged);
                    }

                    disposed = true;
                }
            }
        }
        
        public static List<Net.Protocol.Characters.CharacterData> GetCharactersForDisplay(Logic.Player player)
        {
            var content = new List<Net.Protocol.Characters.CharacterData>();
            
            if (player?.Map == null) return content;
            
            foreach (var c in Perception.Agent.Instance.GetVisibleCharacters(player))
            {
                if (c == null) continue;
                
                string name;
                double progress;

                int configId = 0;
                
                if (c is Logic.Life life)
                {
                    name = Domain.Text.Decorate.Life(life, player);
                    progress = (!life.State.Is(Logic.Life.States.Unconscious) && life.Relation.Where(kvp => kvp.Value < 0).OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).Any(t => t is Life l && !l.State.Is(Logic.Life.States.Unconscious) && l.Map == life.Map) ? life.Action : 0);
                    configId = life.Config?.Id ?? 0;
                }
                else if (c is Logic.Item item)
                {
                    name = Domain.Text.Decorate.Item(item, player);
                    progress = 0;
                    configId = item.Config?.Id ?? 0;
                }
                else
                {
                    name = c.GetType().Name;
                    progress = 0;
                }

                content.Add(new Net.Protocol.Characters.CharacterData(name, progress, c.GetHashCode(), configId));
            }
            
            return content;
        }
        
        public static (int CurrentValue, int MaxValue, string Color) GetResourceForDisplay(Logic.Player player, string resourceType)
        {
            switch (resourceType.ToLower())
            {

                case "mp":
                    return (player.Mp, (int)player.MaxMp, Utils.Text.GetDangerRatioColor((double)player.Mp / player.MaxMp));

                case "lp":
                    return ((int)player.Lp, (int)player.MaxLp, Utils.Text.GetDangerRatioColor(player.Lp / player.MaxLp));

                default:
                    throw new ArgumentException($"Unsupported resource type: {resourceType}");
            }
        }
        public static Net.Protocol.WorldMap CreateWorldMap(Logic.Player player)
        {
            var sceneInfos = new List<Net.Protocol.WorldMap.SceneInfo>();
            
            foreach (var (sceneCid, pos) in Logic.Design.World.SceneCoordinates)
            {
                var sceneDesign = Logic.Design.Agent.Instance.Content.Get<Logic.Design.Scene>(s => s.cid == sceneCid);
                if (sceneDesign == null) continue;
                
                var multilingual = Logic.Design.Agent.Instance.Content.Get<Logic.Design.Multilingual>(m => m.cid == sceneDesign.name);
                if (multilingual == null) continue;
                
                string sceneName = Domain.Text.Agent.Instance.Get(multilingual.id, player);
                string sceneType = string.IsNullOrEmpty(sceneDesign.type) ? "其他" : sceneDesign.type;
                string color = Net.Protocol.SceneColorHelper.GetSceneTypeColor(sceneType);
                
                sceneInfos.Add(new Net.Protocol.WorldMap.SceneInfo(pos, sceneName, color, sceneType));
            }
            
            sceneInfos.Sort((a, b) =>
            {
                int xCompare = a.pos[0].CompareTo(b.pos[0]);
                if (xCompare != 0) return xCompare;
                int yCompare = a.pos[1].CompareTo(b.pos[1]);
                if (yCompare != 0) return yCompare;
                return a.pos[2].CompareTo(b.pos[2]);
            });
            
            return new Net.Protocol.WorldMap(sceneInfos);
        }
        
        private static string GetSceneType(Logic.Scene scene)
        {
            if (scene.Config == null) return "其他";
            if (scene is Logic.Maze) return "迷宫";
            return string.IsNullOrEmpty(scene.Config.Type) ? "其他" : scene.Config.Type;
        }

        public static Dictionary<string, (int CurrentValue, int MaxValue, string Color)> GetAllResourcesForDisplay(Logic.Player player)
        {
            return new Dictionary<string, (int CurrentValue, int MaxValue, string Color)>
            {
                {"Mp", GetResourceForDisplay(player, "mp")},
                {"Lp", GetResourceForDisplay(player, "lp")}
            };
        }
    }
}

