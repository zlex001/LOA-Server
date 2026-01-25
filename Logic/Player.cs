
using System;
using Newtonsoft.Json;

namespace Logic
{

    [JsonConverter(typeof(ChannelConverter))]
    public enum Channel
    {
        System = 4000,
        Private = 4001,
        Local = 4002,
        Battle = 4003,
        All = 4004,
        Rumor = 4005,
        Automation = 4006,
    }
    public class Player : Life
    {
        // Delegate for Domain layer to provide visible characters (set by Perception system)
        public static Func<Player, List<Character>> GetVisibleCharacters { get; set; }

        public Database.Player Database { get; set; }

        public string ServerId => Database?.GetText("ServerId");

        public DateTime HostelTime { get => data.Get<DateTime>(Data.HostelTime); set => data.Change(Data.HostelTime, value); }
        public int ScreenUIAdaptation { get => data.Get<int>(Data.ScreenUIAdaptation); set => data.Change(Data.ScreenUIAdaptation, value); }
        public int BenefitCountForLife { get => data.Get<int>(Data.BenefitCountForLife); set => data.Change(Data.BenefitCountForLife, value); }
        public int BenefitCountForSkill { get => data.Get<int>(Data.BenefitCountForSkill); set => data.Change(Data.BenefitCountForSkill, value); }
        public int BenefitCountForLive { get => data.Get<int>(Data.BenefitCountForLive); set => data.Change(Data.BenefitCountForLive, value); }
        public int TaskCycle { get => data.Get<int>(Data.TaskCycle); set => data.Change(Data.TaskCycle, value); }
        public int ProfitMoney { get => data.Get<int>(Data.ProfitMoney); set => data.Change(Data.ProfitMoney, value); }
        public int ProfitToken { get => data.Get<int>(Data.ProfitToken); set => data.Change(Data.ProfitToken, value); }
        public int CumulativeGem { get => data.Get<int>(Data.CumulativeGem); set => data.Change(Data.CumulativeGem, value); }
        public string Pw { get => data.Get<string>(Data.Pw); set => data.Change(Data.Pw, value); }
        public uint TowerLevel { get => data.Get<uint>(Data.TowerLevel); set => data.Change(Data.TowerLevel, value); }
        public int OpvpScore { get => data.Get<int>(Data.OpvpScore); set => data.Change(Data.OpvpScore, value); }
        public int OpvpRank => Utils.Mathematics.IntervalSearch(Utils.Mathematics.OPVP_RANK_SCORE_RANGE, OpvpScore);
        public int Gem { get => data.Get<int>(Data.Gem); set => data.Change(Data.Gem, value); }
        public int Credit { get => data.Get<int>(Data.Credit); set => data.Change(Data.Credit, value); }
        public Option Option => Content.Gets<Option>().LastOrDefault();
        public List<string> History { get => data.Get<List<string>>(Data.History); set => data.Change(Data.History, value); }
        public Basic.Element Commodity { get => data.Get<Basic.Element>(Data.Commodity); set => data.Change(Data.Commodity, value); }
        public DateTime SignIn { get => data.Get<DateTime>(Data.SignIn); set => data.Change(Data.SignIn, value); }

        // 非监听的时间属性（直接字段）
        public DateTime SignOutTime { get; set; }
        public DateTime RegisterTime { get; set; }
        public DateTime DinerTime { get; set; }
        public DateTime EatTime { get; set; }
        public DateTime TokenMonthBasicLastTime { get; set; }
        public DateTime TokenMonthPremiumLastTime { get; set; }
        public DateTime MonthlyExpLastCalcTime { get; set; }
        public DateTime RankRewardTime { get; set; }
        public string Name { get => data.Get<string>(Data.Name); set => data.Change(Data.Name, value); }
        public Queue<string> Activitys { get; set; } = new Queue<string>();



        public void LoadItemsFromData(List<Database.Item> itemData)
        {
            if (itemData == null || itemData.Count == 0) return;
            
            foreach (var dbItem in itemData)
            {
                try
                {
                    var part = Content.Get<Part>(p => p.Type == dbItem.Part);
                    if (part == null) continue;
                    
                    part.Load<Config.Item, Item>(dbItem.Id, dbItem.Count, dbItem.Properties);
                }
                catch (Exception ex)
                {
                    Utils.Debug.Log.Warning("PLAYER", $"物品加载失败 - Player: {Id}, Item Id: {dbItem.Id}, Exception: {ex.Message}");
                }
            }
        }
        public List<Database.Item> ConvertEquipmentsToData()
        {
            var result = new List<Database.Item>();
            
            foreach (var part in Content.Gets<Part>())
            {
                foreach (var item in part.Content.Gets<Item>())
                {
                    try
                    {
                        var dbItem = new Database.Item(item.Config.Id, item.Count);
                        dbItem.Properties = item.CollectPropertiesForDatabase();
                        dbItem.Part = part.Type;
                        result.Add(dbItem);
                    }
                    catch (Exception ex)
                    {
                        Utils.Debug.Log.Error("LOGOUT", $"装备转换失败 - Player: {Id}, Item.Config.Id: {item.Config.Id}, Error: {ex.Message}");
                    }
                }
            }
            
            return result;
        }
        public Player() : base()
        {
            data.before.Register(Data.OpvpScore, OnOpvpScoreChangeStart);

            data.after.Register(Data.OpvpScore, OnOpvpScoreChangeComplete);

            monitor.Register(Hostel.Event.AfterAdd, OnAfterHostelAddMe);
            monitor.Register(Hostel.Event.AfterRemove, OnAfterHostelRemoveThis);


            monitor.Register(Life.Event.Die, OnDie);

            data.after.Register(Data.SignIn, OnSignInChangeComplete);

        }


        public string Id { get; private set; }
        public override void Init(params object[] args)
        {
            Database = (Logic.Database.Player)args[0];
            Id = Database.Id;
            data.raw[Data.Name] = Database.GetText("Name");
            data.raw[Data.Description] = Database.GetRecord("Description");
            data.raw[Life.Data.Gender] = Enum.TryParse<Genders>(Database.GetText("Gender"), out var gender) ? gender : Genders.Female;
            Category = Enum.TryParse<Categories>(Database.GetText("Category"), out var category) ? category : Categories.Atlantean;
            Array.ForEach(Part.Template.GetByCategory(Category), partType => Create<Part>(partType));
            data.raw[Life.Data.Age] = (double)Database.GetRecord("Age");
            data.raw[Life.Data.Exp] = Database.GetRecord("Exp");
            data.raw[Life.Data.Level] = Database.GetRecord("Level");

            Grade = Database.grade;

            data.raw[Life.Data.Mp] = Database.GetRecord("Mp");
            data.raw[Life.Data.Lp] = (double)Database.GetRecord("Lp");

            foreach (var partData in Database.parts)
            {
                if(Content.Has<Part>(p => p.Type == partData.Type,out Part part))
                {
                    part.MaxHp = partData.MaxHp;
                    part.Hp = partData.Hp;
                }
              
            }




            data.raw[Data.Gem] = Database.GetRecord("Gem");
            data.raw[Data.Credit] = Database.GetRecord("Credit");
            data.raw[Data.TaskCycle] = Database.GetRecord("TaskCycle");
            int screenUIAdaptation = Database.GetRecord("ScreenUIAdaptation");
            data.raw[Data.ScreenUIAdaptation] = screenUIAdaptation > 0 ? screenUIAdaptation : 100;
            data.raw[Data.ProfitMoney] = Database.GetRecord("ProfitMoney");
            data.raw[Data.ProfitToken] = Database.GetRecord("ProfitToken");
            data.raw[Data.OpvpScore] = Database.GetRecord("OpvpScore");
            data.raw[Data.OpvpWinningStreak] = Database.GetRecord("OpvpWinningStreak");
            data.raw[Data.OpvpLosingStreak] = Database.GetRecord("OpvpLosingStreak");
            data.raw[Data.CumulativeGem] = Database.GetRecord("CumulativeGem");
            data.raw[Data.TowerLevel] = (uint)Database.GetRecord("Tower");
            data.raw[Data.TokenFund] = Database.GetRecord("TokenFund") != default;
            data.raw[Life.Data.Master] = Database.GetText("Master");

            SignOutTime = Database.GetTime("SignOut");
            data.raw[Data.SignIn] = Database.GetTime("SignIn");
            RegisterTime = Database.GetTime("Register");
            DinerTime = Database.GetTime("Diner");
            EatTime = Database.GetTime("Eat");
            TokenMonthBasicLastTime = Database.GetTime("TokenMonthBasicLast");
            TokenMonthPremiumLastTime = Database.GetTime("TokenMonthPremiumLast");
            MonthlyExpLastCalcTime = Database.GetTime("MonthlyExpLastCalc");
            RankRewardTime = Database.GetTime("RankReward");
            data.raw[Data.HostelTime] = Database.GetTime("HostelTime");


            data.before.Register(Data.Gem, OnGemChangeStart);
            Activitys = Database.activitys;

            foreach (var s in Database.skills)
            {
                try
                {
                    Load<Config.Skill, Skill>(s.Id, s.Exp, s.Level);
                }
                catch (Exception ex)
                {
                    Utils.Debug.Log.Warning("PLAYER", $"技能加载失败 - Player: {Id}, Skill Id: {s.Id}, Exception: {ex.Message}");
                }
            }

            LoadItemsFromData(Database.equipments);

            Content.Add.Register(typeof(Skill), OnContentAddSkill);
            Content.Add.Register(typeof(Item), OnContentAddItem);
            Content.Add.Register(typeof(Movement), OnContentAddMovement);
            Content.Add.Register(typeof(Option), OnContentAddOption);
            Content.Remove.Register(typeof(Option), OnContentRemoveOption);
            Content.Add.Register(typeof(Warehouse), OnContentAddWarehouse);

            // 仓库处理已移至业务层Domain.Deposit负责
            foreach (Database.Payment payment in Database.payments) Create<Payment>(payment);

            if (Database.signs?.Count > 0)
            {
                foreach (int signId in Database.signs.Distinct())
                {
                    if (!Content.Has<Plot>(s => s.Config.Id == signId))
                    {
                        var existingSign = Logic.Agent.Instance.Content.Get<Plot>(s => s.Config.Id == signId);
                        if (existingSign != null) Add(existingSign);
                    }
                }
            }

            Agent.Instance.Add(this);
        }
        public override void Release()
        {
            // TODO: 这里没有副本清理逻辑！如果玩家在副本中下线，副本可能不会被销毁
            
            Agent.Instance.Remove(this);
            base.Release();
        }
        private void OnContentAddSkill(params object[] args)
        {
            Skill skill = (Skill)args[1];
            skill.monitor.Fire(Event.AfterAddAsParent, this);
        }
        private void OnContentAddItem(params object[] args)
        {
            Item item = (Item)args[1];
            item.monitor.Fire(Event.AfterAddAsParent, this);
        }
        private void OnContentAddMovement(params object[] args)
        {
            Movement movement = (Movement)args[1];
            movement.monitor.Fire(Event.AfterAddAsParent, this);
        }
        private void OnContentAddOption(params object[] args)
        {
            Option option = (Option)args[1];
            if (option.Relates.Count > 0)
            {
                var target = option.Relates[0];
            }
            option.monitor.Fire(Event.AfterAddAsParent, this);
        }
        
        private void OnContentRemoveOption(params object[] args)
        {
            Option option = (Option)args[1];
            option.monitor.Fire(Option.Event.AfterRemove, this, option);
        }
        // 旧的BehaviorTree事件处理已删除
        private void OnContentAddWarehouse(params object[] args)
        {
            Warehouse warehouse = (Warehouse)args[1];
            warehouse.monitor.Fire(Event.AfterAddAsParent, this);
        }


     





        private void OnDie(params object[] args)
        {

        }


        private void OnGemChangeStart(params object[] args)
        {
            int o = (int)args[0];
            int v = (int)args[1];
            int d = v - o;
            if (d > 0)
            {
                CumulativeGem += d;
            }
            monitor.Fire(Event.GemChanged, this, o, v);
        }


        private void OnOpvpScoreChangeStart(object[] args)
        {
            int o = (int)args[0];
            int v = (int)args[1];
            monitor.Fire(Event.OpvpScoreChanged, this, o, v);
        }
        private void OnOpvpScoreChangeComplete(object[] args)
        {
            int v = (int)args[0];
        }
        private void OnAutomatic(object[] args)
        {
            Logic.Ability obj = (Logic.Ability)args[0];
            Remove<Option>();
        }
        public Logic.Map ClickTarget { get; set; }

        public enum Click
        {
            Map,
            Character,
            Scene,
        }
        public enum Event
        {
            AfterAddAsParent,
            GemChanged,
            CreditChanged,
            OpvpScoreChanged,
            ArenaPayOut,
            Chat,
            QuitToMenu,
            QuitToDesktop,
            Purchase,
            ApplePurchase,
            AlipayPurchase,
            CardPurchase,
        }
        public enum Record
        {
            Exp,
            Lp,
            Money,
        }
        public enum Data
        {
            Name,
            Pw,
            Gem,
            Credit,
            CumulativeGem,
            TowerLevel,
            Mass,
            MassGiveObject,
            MassGiveInObject,
            MapCopyEntrance,
            History,
            Commodity,
            ProfitMoney,
            ProfitToken,
            OpvpScore,
            OpvpWinningStreak,
            OpvpLosingStreak,
            TokenFund,
            TaskCycle,
            BenefitCountForLife,
            BenefitCountForSkill,
            BenefitCountForLive,
            ScreenUIAdaptation,
            HostelTime,
            AlipayOrder,
            Language,
            Description,
            Viewer,
            SignIn,
            MonthlyExpAccumulator,
            MonthlyExpAccumulatorBasic,
            MonthlyExpAccumulatorPremium,
        }
        public string AlipayOrder { get => data.Get<string>(Data.AlipayOrder); set => data.Change(Data.AlipayOrder, value); }
        public Text.Languages Language { get => data.Get<Text.Languages>(Data.Language); set => data.Change(Data.Language, value, this); }
        public Life Viewer { get => data.Get<Life>(Data.Viewer); set => data.Change(Data.Viewer, value, this); }
        public double MonthlyExpAccumulator { get => data.Get<double>(Data.MonthlyExpAccumulator); set => data.Change(Data.MonthlyExpAccumulator, value); }
        public double MonthlyExpAccumulatorBasic { get => data.Get<double>(Data.MonthlyExpAccumulatorBasic); set => data.Change(Data.MonthlyExpAccumulatorBasic, value); }
        public double MonthlyExpAccumulatorPremium { get => data.Get<double>(Data.MonthlyExpAccumulatorPremium); set => data.Change(Data.MonthlyExpAccumulatorPremium, value); }

        public void OptionBackward()
        {
            Option last = Content.Gets<Option>().LastOrDefault();
            if (last != null)
            {
                Remove(last);
            }
            monitor.Fire(Option.Event.Refresh, this);
        }

        private void OnAfterHostelAddMe(params object[] args)
        {
            Hostel hostel = (Hostel)args[0];
            if (!Content.Has<Warehouse>()) { AddAsParent(new Warehouse(1)); }
            Dictionary<Item, Warehouse> warehouse = new Dictionary<Item, Warehouse>();
            foreach (Warehouse playerWarehouse in Content.Gets<Warehouse>())
            {
                Item box = hostel.Start.Create<Item>(Logic.Config.Agent.Instance.Content.Get<Config.Item>(i => i.Type == "PrivateBox" && i.feature["Rarity"] == playerWarehouse.Level * 10000));
                foreach (var i in playerWarehouse.Item)
                {
                    box.Load<Config.Item, Item>(i.Key, i.Value);
                }
                warehouse[box] = playerWarehouse;

            }
            hostel.Warehouse = warehouse;
        }
        private void OnAfterHostelRemoveThis(params object[] args)
        {
            Hostel hostel = (Hostel)args[0];
        }
        private void OnSignInChangeComplete(params object[] args)
        {
            DateTime dateTime = (DateTime)args[0];
            string dateOnly = dateTime.ToString("yyyy-MM-dd");

            if (Activitys.All(d => !d.StartsWith(dateOnly)))
            {
                if (Activitys.Count >= 90)
                {
                    Activitys.Dequeue();
                }
                Activitys.Enqueue($"{dateOnly} {dateTime.ToString("HH:mm:ss")}");
            }
        }
    }
}

