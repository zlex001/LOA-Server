using System;
using System.Linq;

namespace Data
{
    public class Option : Ability
    {
        #region Item Class
        public class Item
        {
            public Item() { }

            public Item(Type type)
            {
                this.type = type;
            }

            public Item(string text)
            {
                type = Type.Text;
                data["Text"] = text;
            }

            public Item(Type type, Dictionary<string, string> data)
            {
                this.type = type;
                this.data = data;
            }

            public Item(Type type, string text)
            {
                this.type = type;
                data["Text"] = text;
            }

            public Item(Type type, params (string Key, string Value)[] keyValues)
            {
                this.type = type;
                foreach (var (key, value) in keyValues)
                {
                    data[key] = value;
                }
            }

            public enum Type
            {
                Text,
                Button,
                TitleButton,
                TitleButtonWithProgress,
                IAPButton,
                Radar,
                ProgressWithValue,
                Progress,
                Slider,
                Input,
                Filter,
                ToggleGroup,
                Confirm,
                Amount,
            }

            public Type type;
            public Dictionary<string, string> data = new Dictionary<string, string>();
        }

        #endregion

        #region Enumerations

        public enum Event
        {
            Update,
            Delete,
            Refresh,
            DropOrder,
            AfterRemove,
        }

        public enum Types
        {
            Buy,
            BuyOrder,
            CardInput,
            Cast,
            DropOrder,
            Give,
            GiveOrder,
            Language,
            Mall,
            MallOrder,
            Operation,
            PickOrder,
            Sell,
            SellOrder,
            Settings,
            Shop,
            Sign,
            Teleport,
        }

        public enum LeftPanel
        {
            Buy,
            BuyOrder,
            CardInput,
            DropOrder,
            Give,
            GiveOrder,
            Language,
            Mall,
            MallOrder,
            Operation,
            PickOrder,
            Sell,
            SellOrder,
            Settings,
            Shop,
            Sign,
            Teleport,
        }

        public enum RightPanel
        {
            Buy,
            BuyOrder,
            CardInput,
            DropOrder,
            Give,
            GiveOrder,
            Language,
            Mall,
            MallOrder,
            Operation,
            PickOrder,
            Sell,
            SellOrder,
            Settings,
            Shop,
            Sign,
            Teleport,
        }


        #endregion

        #region Properties

        public Types Type { get; private set; }
        public Player Player { get; private set; }
        public List<Ability> Relates { get; private set; } = new List<Ability>();
        public Settings Setting => Player.Content.Get<Settings>(s => Equals(s.Type, Type));

        #endregion

        #region Constructor

        public Option()
        {
            monitor.Register(Player.Event.AfterAddAsParent, OnAfterPlayerObtainThis);
        }

        #endregion

        #region Initialization

        public override void Init(params object[] args)
        {
            Type = (Types)args[0];
            Player = (Player)args[1];
            
            // Validate that at least one target is provided
            if (args.Length < 3)
            {
                string playerId = (Player?.Database?.Id ?? "unknown");
                Utils.Debug.Log.Warning("OPTION", $"[Option.Init] Creating Option with no targets! Type={Type}, Player={playerId}, ArgsLength={args.Length}");
            }
            
            for (int i = 2; i < args.Length; i++)
            {
                Ability obj = (Ability)args[i];
                if (obj == null)
                {
                    string playerId = (Player?.Database?.Id ?? "unknown");
                    Utils.Debug.Log.Warning("OPTION", $"[Option.Init] Target at index {i} is null! Type={Type}, Player={playerId}");
                    continue;
                }
                obj.monitor.Register(Event.Update, OnUpdate);
                obj.monitor.Register(Event.Delete, OnDelete);
                Relates.Add(obj);
            }
            
            if (!Player.Content.Has<Settings>(s => s.Type == Type))
            {
                Player.Create<Settings>(args);
            }
            else
            {
                Settings setting = Player.Content.Get<Settings>(s => s.Type == Type);
                setting.Relates.Clear();
                for (int i = 2; i < args.Length; i++)
                {
                    if (args[i] is Ability ability && ability != null)
                    {
                        setting.Relates.Add(ability);
                    }
                }
                Player.Content.Add.Fire(typeof(Settings), Player, setting);
            }
        }

        #endregion

        #region Event Handlers

        private void OnDelete(params object[] args)
        {
            if (Player.Has(this))
            {
                bool wasCurrentOption = (Player.Option == this);
                Player.Remove(this);
                
                if (wasCurrentOption)
                {
                    Player.monitor.Fire(Event.Refresh, Player);
                }
            }
        }

        private void OnUpdate(params object[] args)
        {
            if (Player.Option == this)
            {
                Player.monitor.Fire(Event.Refresh, Player);
            }
        }

        private void OnAfterPlayerObtainThis(params object[] args)
        {
            Player player = (Player)args[0];
            if (!Relates.Contains(Player))
            {
                player.monitor.Fire(Event.Refresh, player);
            }
        }

        private void OnAfterPlayerRemoveThis(params object[] args)
        {
            Player player = (Player)args[1];
            foreach (Ability obj in Relates)
            {
                obj.monitor.Unregister(Event.Update, OnUpdate);
                obj.monitor.Unregister(Event.Delete, OnDelete);
            }
        }

        #endregion

        #region Settings Class

        public class Settings : Ability
        {
            #region Enumerations

            public enum Event
            {
            }

            public enum Data
            {
                Type,
                Player,
                Relates,
                SliderMin,
                SliderValue,
                SliderMax,
                Filter,
                ToggleGroup,
                Input,
                Amount,
                MallId,
            }

            #endregion

            #region Properties

            public Types Type => data.Get<Types>(Data.Type);
            public Player Player => data.Get<Player>(Data.Player);
            public List<Ability> Relates => data.Get<List<Ability>>(Data.Relates);
            public int Amount { get => data.Get<int>(Data.Amount); set => data.Change(Data.Amount, value); }
            public int SliderMin { get => data.Get<int>(Data.SliderMin); set => data.Change(Data.SliderMin, value); }
            public int SliderValue { get => data.Get<int>(Data.SliderValue); set => data.Change(Data.SliderValue, value); }
            public int SliderMax { get => data.Get<int>(Data.SliderMax); set => data.Change(Data.SliderMax, value); }
            public string Filter { get => data.Get<string>(Data.Filter); set => data.Change(Data.Filter, value); }
            public string Input { get => data.Get<string>(Data.Input); set => data.Change(Data.Input, value); }
            public Dictionary<string, bool> ToggleGroup { get => data.Get<Dictionary<string, bool>>(Data.ToggleGroup); set => data.Change(Data.ToggleGroup, value); }
            public string ToggleGroupText => string.Join("、", ToggleGroup.Keys.Select(key => $"{key}:{ToggleGroup[key]}"));

            #endregion

            #region Constructor and Initialization

            public Settings()
            {
                data.raw[Data.Relates] = new List<Ability>();
                data.raw[Data.ToggleGroup] = new Dictionary<string, bool>();
            }

            public override void Init(params object[] args)
            {
                data.raw[Data.Type] = args[0];
                data.raw[Data.Player] = (Player)args[1];
                for (int i = 2; i < args.Length; i++)
                {
                    Relates.Add((Ability)args[i]);
                }
            }

            #endregion
        }

        #endregion
    }
}


