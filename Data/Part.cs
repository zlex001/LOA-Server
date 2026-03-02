using System;
using System.Collections.Generic;
using System.Linq;

namespace Data
{

    public class Part : Character
    {
        public enum Types
        {

            Default = 1100,
            // === 道具 ===
            Top = 1101,         // 上面
            Bottom = 1102,      // 下面
            Inside = 1103,      // 里面
            Outside = 1104,     // 外面

            // === 生物 ===
            Head = 1110,        // 头
            Chest = 1111,       // 胸
            Back = 1112,        // 背部
            Wing = 1113,        // 翅膀
            Waist = 1114,       // 腰部
            Hand = 1115,        // 手
            Leg = 1116,         // 腿
            Foot = 1117,        // 脚
            Claw = 1118,        // 爪子
            Tail = 1119,        // 尾巴
        }


        public enum Data
        {
            Hp,
            MaxHp,
        }
        public Types Type { get; private set; }
        public int Hp { get => data.Get<int>(Data.Hp); set => data.Change(Data.Hp, value, this); }
        public int MaxHp { get => data.Get<int>(Data.MaxHp); set => data.Change(Data.MaxHp, value); }

        private static readonly Dictionary<Types, double> PartHpWeight = new()
        {
            { Types.Head, 0.25 },     // 头部 - 25%
            { Types.Chest, 0.30 },    // 胸部 - 30%
            { Types.Back, 0.15 },     // 背部 - 15%
            { Types.Waist, 0.10 },    // 腰部 - 10%
            { Types.Hand, 0.08 },     // 手部 - 8%
            { Types.Leg, 0.08 },      // 腿部 - 8%
            { Types.Foot, 0.04 },     // 脚部 - 4%
            { Types.Claw, 0.08 },     // 爪子 - 8%
            { Types.Tail, 0.05 },     // 尾巴 - 5%
            { Types.Wing, 0.10 },     // 翅膀 - 10%
            { Types.Default, 0.10 }   // 默认值 - 10%
        };

        public Part()
        {
            data.after.Register(Data.Hp, OnAfterHpChanged);
            data.min.Add(Data.Hp, (Func<int>)(() => 0));
            data.max.Add(Data.Hp, (Func<int>)(() => MaxHp));
        }

        public override void Init(params object[] args)
        {
            Type = (Types)args[0];

            // 初始化时MaxHp由Life在创建Part后统一设置
            data.raw[Data.MaxHp] = 0;
            data.raw[Data.Hp] = 0;
        }

        /// <summary>
        /// 获取部位Hp分配权重
        /// </summary>
        public double GetHpWeight()
        {
            return PartHpWeight.TryGetValue(Type, out double weight) ? weight : 0.10;
        }

        public bool IsCriticalPart()
        {
            return Type == Types.Head || Type == Types.Chest;
        }


        private void OnAfterHpChanged(params object[] args)
        {
            // Hp变化的其他处理逻辑（如果需要）
            // 状态转换由Logic.State.*.Update负责
        }




        public override void Release()
        {
            data.after.Unregister(Data.Hp, OnAfterHpChanged);
            base.Release();
        }

        public static class Template
        {
            // 已废弃：旧的Category硬编码映射（仅保留最小后备方案）
            private static readonly Dictionary<Life.Categories, Types[]> lifeMap = new()
            {
                [Life.Categories.Lemurian] = new[]
                {
                    Types.Head, Types.Chest, Types.Hand,
                    Types.Back, Types.Waist, Types.Leg, Types.Foot
                },
                [Life.Categories.Atlantean] = new[]
                {
                    Types.Head, Types.Chest, Types.Hand,
                    Types.Back, Types.Waist, Types.Leg, Types.Foot, Types.Tail
                },
                [Life.Categories.Druk] = new[]
                {
                    Types.Head, Types.Chest, Types.Hand,
                    Types.Back, Types.Waist, Types.Leg, Types.Foot
                },
                [Life.Categories.Beastman] = new[]
                {
                    Types.Head, Types.Chest, Types.Hand,
                    Types.Back, Types.Waist, Types.Leg, Types.Foot
                },
                [Life.Categories.Demon] = new[]
                {
                    Types.Head, Types.Chest, Types.Back,
                    Types.Claw, Types.Tail, Types.Wing
                },
                [Life.Categories.Animal] = new[]
                {
                    Types.Head, Types.Chest, Types.Back,
                    Types.Leg, Types.Claw, Types.Tail
                },
            };

            private static readonly Types[] defaultLifeParts = new[]
            {
                Types.Head, Types.Chest, Types.Hand,
                Types.Back, Types.Waist, Types.Leg, Types.Foot
            };

            // 英文Part名称到Types的映射
            private static readonly Dictionary<string, Types> partNameMapping = new()
            {
                ["Head"] = Types.Head,
                ["Chest"] = Types.Chest,
                ["Hand"] = Types.Hand,
                ["Back"] = Types.Back,
                ["Waist"] = Types.Waist,
                ["Leg"] = Types.Leg,
                ["Foot"] = Types.Foot,
                ["Claw"] = Types.Claw,
                ["Tail"] = Types.Tail,
                ["Wing"] = Types.Wing
            };

            /// <summary>
            /// 从Life配置读取Parts（新方法，优先使用）
            /// </summary>
            public static Types[] Get(global::Data.Config.Life config)
            {
                if (config?.parts != null && config.parts.Length > 0)
                {
                    var result = new List<Types>();
                    foreach (var partName in config.parts)
                    {
                        if (partNameMapping.TryGetValue(partName, out var partType))
                        {
                            result.Add(partType);
                        }
                        else
                        {
                            Utils.Debug.Log.Error("PART", $"Unknown part name in config: {partName} for Life {config.Id}");
                        }
                    }
                    
                    if (result.Count > 0)
                        return result.ToArray();
                }

                // If config has no parts, fallback to category-based parts
                Utils.Debug.Log.Warning("PART", $"Life {config.Id} has no parts config, falling back to category-based parts.");
                return GetByCategory(Enum.Parse<Life.Categories>(config.category));
            }

            /// <summary>
            /// For dynamic entities without Config.Life (e.g., Player loaded from database)
            /// </summary>
            public static Types[] GetByCategory(Life.Categories category)
                => lifeMap.TryGetValue(category, out var result) ? result : defaultLifeParts;

            /// <summary>
            /// Obsolete: Use Get(global::Data.Config.Life config) for NPC, or GetByCategory for Player
            /// </summary>
            [Obsolete("Use Get(global::Data.Config.Life config) for NPC, or GetByCategory for Player")]
            public static Types[] Get(Life.Categories category)
                => GetByCategory(category);

            public static Types[] Get(Item.Types type)
                => new[] { Types.Top, Types.Bottom, Types.Inside, Types.Outside };
        }
    }
}