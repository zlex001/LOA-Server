using Basic;


namespace Data
{
    public class Life : Character<Config.Life>
    {
        public enum Attributes
        {
            Hp = 1001,
            Atk = 1002,
            Def = 1003,
            Agi = 1004,
            Mp = 1005,
            Ine = 1006,
            Con = 1007,
        }
        public enum Crime
        {
            Murder = 3000,
            Assault = 3001,
            Theft = 3002,
            Robbery = 3003,
            AssaultOfficer = 3004,
            PrisonBreak = 3005,
            JailBreak = 3006,
            Kidnap = 3007,
        }
        public enum Categories
        {
            Lemurian,      // 雷姆利亚人（玩家可选）
            Atlantean,     // 亚特兰斯人（玩家可选）
            Druk,          // 德鲁克人（BOSS/遗迹守卫）
            Beastman,      // 半兽人（守护者：狮身人、阿努比斯、牛头人、半人马、蛇人、象神、荷鲁斯）
            Demon,         // 魔兽（敌对怪物）
            Animal,        // 野生动物（鸡、兔、羊、鹿、蛇、狐狸、猴子、野猪、鳄鱼、狼、大象、熊、豹、虎）
        }
        public enum States
        {
            Normal = 1010,
            Unconscious = 1011,
            Battle = 1012,
        }
        public enum Genders
        {
            Female = 1020,
            Male = 1021,
        }


        public enum Event
        {
            Walk,
            Addable,
            AfterRemove,
            Talk,
            Talked,
            Die,
            DisAttack,
            DisAttacked,
            AgeChanged,
        }
        public enum Data
        {
            Burden,
            Grade,
            MaxMp,
            Atk,
            Def,
            Agi,
            Ine,
            Con,
            Gender,
            Mp,
            Lp,
            Age,
            Master,
            Exp,
            Level,
            Action,
            BattleKillCount,
            Mount,
            WorkStep,
            SkillExpSkill,
            CriticalIncrease,
            CriticalFix,
            BuffDetermineIncrease,
            BuffDetermineFix,
            DamageIncrease,
            DamageFix,
            CostIncrease,
            CostFix,
            CompeteInitialHp,
            CompeteInitialMp,
            MasterApprenticeshipSuccesses,
           

            SupplyLp,
            Literary,
            Round,
            WalkScale,
            ViewScale,
            Leader,
            Bearer,
            Birthplace,
            ManualUnequippedItems,
            Tracker,
        }
        public Categories Category { get; protected set; }
        public Genders Gender => data.Get<Genders>(Data.Gender);
        public State<States> State { get; set; }
        public Dictionary<Character, double> Relation { get; set; } = new();
        public DateTime FaintDateTime { get; set; }
        public DateTime WakeUpTime { get; set; }
        public int Injury { get; set; }
        public global::Data.BehaviorTree.Node CurrentExecutingNode { get; set; }
        public global::Data.BehaviorTree.Node BtRoot { get; set; }
        public long BtTaskId { get; set; }
        public List<Scene> WorkingScenes { get; set; }
        public Life Leader { get => data.Get<Life>(Data.Leader); set => data.Change(Data.Leader, value, this); }
        public Life Bearer { get => data.Get<Life>(Data.Bearer); set => data.Change(Data.Bearer, value, this); }
        public Life Tracker { get => data.Get<Life>(Data.Tracker); set => data.Change(Data.Tracker, value, this); }
        public Part Hand => Content.Get<Part>(p => p.Type == Part.Types.Hand);
        public Life() : base()
        {
            data.after.Register(Data.Lp, OnLpChangeComplete);
            data.after.Register(Data.Age, OnAfterAgeChanged);
            monitor.Register(Basic.Manager.Event.Addable, OnAddable);

            monitor.Register(Event.DisAttack, OnDisAttack);
            monitor.Register(Event.DisAttacked, OnDisAttacked);

            data.min.Add(Data.Mp, (Func<int>)(() => 0));
            data.max.Add(Data.Mp, (Func<int>)(() => (int)MaxMp));
            data.min.Add(Data.Lp, (Func<double>)(() => 0));
            data.max.Add(Data.Lp, (Func<double>)(() => 100));

            for (int i = 1; i < 9; i++)
            {
                Load<global::Data.Config.Movement, global::Data.Movement>(i);
            }
            Content.Remove.Register(typeof(Part), OnRemovePart);

            data.raw[Data.ManualUnequippedItems] = new HashSet<int>();
        }
        public override void Init(params object[] args)
        {
            Config = (Config.Life)args[0];
            data.raw[Data.Level] = (int)args[1];
            data.raw[Data.Age] = (double)Config.age;
            Category = Enum.Parse<Categories>(Config.category, true);
            Array.ForEach(Part.Template.Get(Config), part => Create<Part>(part));  // 改为从Config读取Parts
            Grade = Config.attribute.ToDictionary(a => a.Key, a => Math.Max(a.Value + random.Next(-2, 2), 0));

            foreach (int s in Config.skills)
            {
                Load<Config.Skill, Skill>(s, 0, Level);
            }
            foreach (var d in Config.item)
            {
                var item = Load<Config.Item, Item>(d.Key, d.Value);
            }

            Agent.Instance.Add(this);
        }
        public override void Release()
        {
            ClearAllHateTowardsMe();
            Agent.Instance.Remove(this);
            base.Release();
        }
        public int Exp { get => data.Get<int>(Data.Exp); set => data.Change(Data.Exp, value, this); }
        public int Level { get => data.Get<int>(Data.Level); set => data.Change(Data.Level, value, this); }
        public int NextExp { get; set; }

        public double MaxMp { get => data.Get<double>(Data.MaxMp); set => data.Change(Data.MaxMp, value); }
        public double MaxLp => 100;
        public double Atk { get => data.Get<double>(Data.Atk); set => data.Change(Data.Atk, value); }
        public double Def { get => data.Get<double>(Data.Def); set => data.Change(Data.Def, value); }
        public double Agi { get => data.Get<double>(Data.Agi) * (1 - Burden); set => data.Change(Data.Agi, value); }
        public double Ine { get => data.Get<double>(Data.Ine); set => data.Change(Data.Ine, value); }
        public double Con { get => data.Get<double>(Data.Con); set => data.Change(Data.Con, value); }
        public Dictionary<Attributes, int> Grade { get => data.Get<Dictionary<Attributes, int>>(Data.Grade); set => data.Change(Data.Grade, value); }
        public double Burden { get => data.Get<double>(Data.Burden); set => data.Change(Data.Burden, value); }
        public double WalkScale { get => data.Get<double>(Data.WalkScale); set => data.Change(Data.WalkScale, value); }
        public double ViewScale { get => data.Get<double>(Data.ViewScale); set => data.Change(Data.ViewScale, value); }
        public double Age { get => data.Get<double>(Data.Age); set => data.Change(Data.Age, value, this); }
        public Map Birthplace { get => data.Get<Map>(Data.Birthplace); set => data.Change(Data.Birthplace, value, this); }
        public HashSet<int> ManualUnequippedItems { get => data.Get<HashSet<int>>(Data.ManualUnequippedItems); set => data.Change(Data.ManualUnequippedItems, value); }
        public int Mp { get => data.Get<int>(Data.Mp); set => data.Change(Data.Mp, value); }
        public double Lp { get => data.Get<double>(Data.Lp); set => data.Change(Data.Lp, value, this); }
        public string Master { get => data.Get<string>(Data.Master); set => data.Change(Data.Master, value); }
        public double Action { get => data.Get<double>(Data.Action); set => data.Change(Data.Action, value, this); }
        public int Round { get => data.Get<int>(Data.Round); set => data.Change(Data.Round, value); }
        private void OnLpChangeComplete(params object[] args)
        {
            double v = (double)args[0];
            if (v >= data.GetMax<double>(Data.Lp))
            {
                data.raw[Data.SupplyLp] = false;
            }
        }
        private void OnAfterAgeChanged(params object[] args)
        {
            monitor.Fire(Event.AgeChanged, this);
        }
        private void OnDisAttack(params object[] args)
        {
            Character obj = (Character)args[0];

            if (Relation.ContainsKey(obj))
            {
                Relation.Remove(obj);
            }

            obj.monitor.Fire(Event.DisAttacked, this);
        }
        private void OnDisAttacked(params object[] args)
        {
            Character sub = (Character)args[0];

            if (Relation.ContainsKey(sub))
            {
                Relation.Remove(sub);
            }
        }
        private void ClearAllHateTowardsMe()
        {
            if (Map != null)
            {
                string myName = this is global::Data.Player p ? p.Id : this.GetType().Name;

                // 重置自己的Action值，停止回合系统
                if (Action > 0)
                {
                    Action = 0;
                }

                int clearedCount = 0;
                foreach (var life in Map.Content.Gets<Life>())
                {
                    if (life != this && life.Relation.ContainsKey(this))
                    {
                        string lifeName = life is global::Data.Player lp ? lp.Id : life.GetType().Name;
                        life.Relation.Remove(this);
                        clearedCount++;
                    }
                }

            }
        }
        private bool OnAddable(params object[] args)
        {
            Basic.Element obj = (Basic.Element)args[0];
            return obj.monitor.Check(Event.Addable, this);
        }
        public void HealHp(int healAmount)
        {
            if (healAmount <= 0) return;

            var parts = Content.Gets<Part>().Where(p => p.Hp < p.MaxHp).ToList();
            if (!parts.Any()) return;

            // 按权重分配恢复量
            int remainingHeal = healAmount;
            foreach (var part in parts)
            {
                if (remainingHeal <= 0) break;

                double partWeight = part.GetHpWeight();
                int partHeal = Math.Min(
                    Math.Min((int)(healAmount * partWeight), part.MaxHp - part.Hp),
                    remainingHeal
                );

                if (partHeal > 0)
                {
                    part.Hp += partHeal;
                    remainingHeal -= partHeal;
                }
            }
        }
        public void SetMinimumHp(int minHp)
        {
            // 确保关键部位（头部和胸部）有最低血量
            var criticalParts = Content.Gets<Part>().Where(p => p.IsCriticalPart()).ToList();
            foreach (var part in criticalParts)
            {
                if (part.Hp <= 0)
                {
                    part.Hp = Math.Max(1, (int)(minHp * part.GetHpWeight()));
                }
            }
        }
        public static System.Func<object, List<string>, (bool success, string failureReason)> ConditionChecker { get; set; }
        public List<Skill> GetAllSkills()
        {
            var allSkills = new List<Skill>();

            // 添加Life自身的技能
            allSkills.AddRange(Content.Gets<Skill>());

            // 添加所有Part的技能
            foreach (var part in Content.Gets<Part>())
            {
                allSkills.AddRange(part.Content.Gets<Skill>());
            }

            // 去重（基于Config.Id，保留等级最高的技能）
            return allSkills.GroupBy(s => s.Config.Id)
                .Select(g => g.OrderByDescending(s => s.Level).First())
                .ToList();
        }
        private void OnRemovePart(params object[] args)
        {
            if (Map == null) return;
            Part part = (Part)args[1];
            if (!Content.Has<Part>())
            {
                foreach (var player in Map.Content.Gets<Player>())
                {
                    monitor.Fire(Event.Die, player, this);
                }
                Destroy();
            }
        }

    }
}