using Basic;
using System.Text;
using System.Text.RegularExpressions;
using Utils;
using System.Collections.Generic;
using System.Linq;

namespace Logic.Design
{
    public class Code
    {
        public static void Init()
        {
            Utils.Generator.GenerateBatch(new Utils.CodeGenerateRequest { ClassName = "Constant", Summary = "系统常量 - 自动生成，请勿手动修改", Usings = { "System", "System.Collections.Generic", "System.Linq", "Utils" }, CodeGenerator = Constant });
        }
        private static string Constant()
        {
            var sb = new StringBuilder();

            // BehaviorTree相关常量
            sb.AppendLine("// BehaviorTree相关常量");
            var oneTimePathfinding = Agent.Instance.Content.Get<BehaviorTree>(bt => bt.cid == "一次性寻路");
            if (oneTimePathfinding != null)
            {
                sb.AppendLine($"public const int OneTimePathfinding = {oneTimePathfinding.id};");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'一次性寻路'的配置");
                sb.AppendLine("// public const int OneTimePathfinding = 0;");
            }
            sb.AppendLine();

            // Map相关常量
            sb.AppendLine("// Map相关常量");
            var initialMap = Agent.Instance.Content.Get<Map>(m => m.cid == "尚清村·驿站");
            if (initialMap != null)
            {
                sb.AppendLine($"public const int InitialMap = {initialMap.id};");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'尚清村·驿站'的配置");
                sb.AppendLine("public const int InitialMap = 1170; // 尚清村·驿站的默认ID");
            }
            sb.AppendLine();

            // Tutorial Map Constants
            sb.AppendLine("// Tutorial相关常量 - 新手引导地图");
            var tutorialShore = Agent.Instance.Content.Get<Map>(m => m.cid == "遗迹-岸边");
            if (tutorialShore != null)
            {
                sb.AppendLine($"public const int TutorialShore = {tutorialShore.id};           // 遗迹-岸边（新手出生点）");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'遗迹-岸边'的配置");
                sb.AppendLine("// public const int TutorialShore = 0;");
            }
            var tutorialSand = Agent.Instance.Content.Get<Map>(m => m.cid == "遗迹-沙地");
            if (tutorialSand != null)
            {
                sb.AppendLine($"public const int TutorialSand = {tutorialSand.id};            // 遗迹-沙地（金矿和蜥蜴）");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'遗迹-沙地'的配置");
                sb.AppendLine("// public const int TutorialSand = 0;");
            }
            var tutorialTower = Agent.Instance.Content.Get<Map>(m => m.cid == "遗迹-通天塔");
            if (tutorialTower != null)
            {
                sb.AppendLine($"public const int TutorialTower = {tutorialTower.id};          // 遗迹-通天塔（石碑）");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'遗迹-通天塔'的配置");
                sb.AppendLine("// public const int TutorialTower = 0;");
            }
            sb.AppendLine("public static readonly int[] TutorialSpawnPoint = new int[] { 0, 0, 0 };  // 新手出生坐标");
            sb.AppendLine();

            sb.AppendLine("// Item相关常量");
            var tokenItem = Agent.Instance.Content.Get<Item>(i => i.cid == "等价币");
            if (tokenItem != null)
            {
                sb.AppendLine($"public const int Token = {tokenItem.id};");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'等价币'的配置");
                sb.AppendLine("// public const int Token = 0;");
            }

            var moneyItem = Agent.Instance.Content.Get<Item>(i => i.cid == "游戏币");
            if (moneyItem != null)
            {
                sb.AppendLine($"public const int Money = {moneyItem.id};");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'游戏币'的配置");
                sb.AppendLine("// public const int Money = 0;");
            }
            sb.AppendLine();

            sb.AppendLine("// 生命体部位掉落道具常量 - 基于策划数据动态生成");

            var rawMeatItem = Agent.Instance.Content.Get<Item>(i => i.cid == "生肉");
            if (rawMeatItem != null)
            {
                sb.AppendLine($"public const int RawMeat = {rawMeatItem.id};      // 生肉（价值{rawMeatItem.value}，标签Meat）");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'生肉'的道具配置");
                sb.AppendLine("// public const int RawMeat = 0;");
            }

            var animalHideItem = Agent.Instance.Content.Get<Item>(i => i.cid == "兽皮");
            if (animalHideItem != null)
            {
                sb.AppendLine($"public const int AnimalHide = {animalHideItem.id};    // 兽皮（价值{animalHideItem.value}，标签Thick）");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'兽皮'的道具配置");
                sb.AppendLine("// public const int AnimalHide = 0;");
            }
            
            var sheepWoolItem = Agent.Instance.Content.Get<Item>(i => i.cid == "羊绒");
            sb.AppendLine("// Buff相关常量");
            var concealmentBuff = Agent.Instance.Content.Get<Buff>(b => b.cid == "隐匿");
            if (concealmentBuff != null)
            {
                sb.AppendLine($"public const int ConcealmentBuff = {concealmentBuff.id};");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'隐匿'的Buff配置");
                sb.AppendLine("// public const int ConcealmentBuff = 0;");
            }
            sb.AppendLine();

            sb.AppendLine("// Multilingual相关常量 - 玩家创角描述系统");
            var playerInitDescTemplate = Agent.Instance.Content.Get<Multilingual>(m => m.cid == "【生物】[描述]玩家创角");
            if (playerInitDescTemplate != null)
            {
                sb.AppendLine($"public const int PlayerInitializeDescriptionTemplate = {playerInitDescTemplate.id};");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'【生物】[描述]玩家创角'的多语言配置");
                sb.AppendLine("// public const int PlayerInitializeDescriptionTemplate = 0;");
            }


            sb.AppendLine();

            if (sheepWoolItem != null)
            {
                sb.AppendLine($"public const int SheepWool = {sheepWoolItem.id};    // 羊绒（价值{sheepWoolItem.value}，标签Elastic）");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'羊绒'的道具配置");
                sb.AppendLine("// public const int SheepWool = 0;");
            }

            var chickenCombItem = Agent.Instance.Content.Get<Item>(i => i.cid == "鸡冠");
            if (chickenCombItem != null)
            {
                sb.AppendLine($"public const int ChickenComb = {chickenCombItem.id};     // 鸡冠（特殊材料）");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'鸡冠'的道具配置");
                sb.AppendLine("public const int ChickenComb = 1005;     // 鸡冠（备用默认值）");
            }

            var chickenWingItem = Agent.Instance.Content.Get<Item>(i => i.cid == "鸡翅");
            if (chickenWingItem != null)
            {
                sb.AppendLine($"public const int ChickenWing = {chickenWingItem.id};     // 鸡翅（特殊材料）");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'鸡翅'的道具配置");
                sb.AppendLine("// public const int ChickenWing = 0;     // 鸡翅（需要在策划表中添加）");
            }
            sb.AppendLine();



            var productContainer = Agent.Instance.Content.Get<Item>(i => i.cid == "木桌");
            if (productContainer != null)
            {
                sb.AppendLine($"public const int ProductContainer = {productContainer.id};     // 产品容器（木桌）");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'木桌'的道具配置");
                sb.AppendLine("// public const int ProductContainer = 0;");
            }

            var miscellaneousContainer = Agent.Instance.Content.Get<Item>(i => i.cid == "木箱");
            if (miscellaneousContainer != null)
            {
                sb.AppendLine($"public const int MiscellaneousContainer = {miscellaneousContainer.id};     // 杂物容器（木箱）");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'木箱'的道具配置");
                sb.AppendLine("// public const int MiscellaneousContainer = 0;");
            }
            
            var stele = Agent.Instance.Content.Get<Item>(i => i.cid == "石碑");
            if (stele != null)
            {
                sb.AppendLine($"public const int Stele = {stele.id};                          // 石碑（新手引导交互物）");
            }
            else
            {
                sb.AppendLine("// 警告：未找到'石碑'的道具配置");
                sb.AppendLine("// public const int Stele = 0;");
            }
            sb.AppendLine();

            sb.AppendLine("// Player相关常量");
            sb.AppendLine("public const int InitialPlayerAge = 1;                  // 玩家初始年龄：1岁");
            sb.AppendLine();

            sb.AppendLine("// ViewScale相关常量");
            sb.AppendLine("public const int BaseViewScale = 1;                     // 视野基础值：1步");
            sb.AppendLine();

            sb.AppendLine("// StateMachine相关常量");
            sb.AppendLine("public const int StateMachineTickIntervalMs = 100;       // 状态机Tick间隔：100毫秒（已废弃，保留用于兼容）");
            sb.AppendLine("public const int StateNormalTickIntervalMs = 300;        // 普通状态刷新间隔：300毫秒");
            sb.AppendLine("public const int StateBattleTickIntervalMs = 100;        // 战斗状态刷新间隔：100毫秒");
            sb.AppendLine("public const int StateDefaultTickIntervalMs = 1000;      // 其他状态刷新间隔：1000毫秒（Rest、Unconscious等）");
            sb.AppendLine("public const double NormalLpDepletionSeconds = 7200.0;    // 满水谷（100）消耗完所需真实时间：7200秒（120分钟）");
            sb.AppendLine("public const double NormalLpDepletionTicks = NormalLpDepletionSeconds * 1000 / StateNormalTickIntervalMs;    // 派生：耗尽Lp所需Tick次数");
            sb.AppendLine();

            sb.AppendLine("// WorldLighting相关常量 - 极端色温对比方案");
            sb.AppendLine("public const double LightingDawnBrightness = 0.60;        // 黎明亮度：60%（破晓微光）");
            sb.AppendLine("public const double LightingDawnColorTintR = 0.75;        // 黎明色调R：大幅降低（强冷）");
            sb.AppendLine("public const double LightingDawnColorTintG = 0.85;        // 黎明色调G：降低");
            sb.AppendLine("public const double LightingDawnColorTintB = 1.30;        // 黎明色调B：大幅升高（极蓝）");
            sb.AppendLine();
            sb.AppendLine("public const double LightingMorningBrightness = 1.10;     // 上午亮度：110%（正午阳光）");
            sb.AppendLine("public const double LightingMorningColorTintR = 1.05;     // 上午色调R：微升（自然暖白）");
            sb.AppendLine("public const double LightingMorningColorTintG = 1.05;     // 上午色调G：微升");
            sb.AppendLine("public const double LightingMorningColorTintB = 1.0;      // 上午色调B：标准");
            sb.AppendLine();
            sb.AppendLine("public const double LightingAfternoonBrightness = 0.75;   // 下午亮度：75%（午后阴影）");
            sb.AppendLine("public const double LightingAfternoonColorTintR = 1.40;   // 下午色调R：大幅升（暖橙）");
            sb.AppendLine("public const double LightingAfternoonColorTintG = 1.10;   // 下午色调G：升高");
            sb.AppendLine("public const double LightingAfternoonColorTintB = 0.70;   // 下午色调B：大幅降（去蓝）");
            sb.AppendLine();
            sb.AppendLine("public const double LightingEveningBrightness = 0.50;     // 傍晚亮度：50%（黄昏昏暗）");
            sb.AppendLine("public const double LightingEveningColorTintR = 1.60;     // 傍晚色调R：极度升（火红）");
            sb.AppendLine("public const double LightingEveningColorTintG = 1.05;     // 傍晚色调G：微升");
            sb.AppendLine("public const double LightingEveningColorTintB = 0.50;     // 傍晚色调B：极度降（强暖）");
            sb.AppendLine();
            sb.AppendLine("public const double LightingNightBrightness = 0.30;       // 夜晚亮度：30%（月光幽暗）");
            sb.AppendLine("public const double LightingNightColorTintR = 0.70;       // 夜晚色调R：大幅降（强冷）");
            sb.AppendLine("public const double LightingNightColorTintG = 0.80;       // 夜晚色调G：降低");
            sb.AppendLine("public const double LightingNightColorTintB = 1.30;       // 夜晚色调B：大幅升（极蓝）");
            sb.AppendLine();

            sb.AppendLine("// ServerOperation相关常量 - 区服运营参数");
            sb.AppendLine("public const int NewServerIntervalDays = 1;              // 新服开放间隔：每1天开1个新服");
            sb.AppendLine("public const int MergeIntervalDays = 30;                 // 合服周期：每30天合并一次");
            sb.AppendLine("public const int ServersPerMergeBatch = 10;              // 每批合服数量：10个区服合并为1个");
            sb.AppendLine("public const int MergedServerGroupsPerBatch = 3;         // 每批保留区服数：合并为3个大区");
            sb.AppendLine("public const int MaxProcessesPerServer = 50;             // 单服务器进程上限：最多50个进程");
            sb.AppendLine("public const double MergeGoldDiscountRate = 0.8;         // 金币打折比例：合服时老服金币×0.8");
            sb.AppendLine("public const int RecommendedPlayersPerProcess = 500;     // 推荐单进程承载：500人在线");
            sb.AppendLine();

            // Cultivation System Constants
            sb.AppendLine("// CultivationSystem相关常量 - 养成系统锚点参数");
            sb.AppendLine();

            // Character Growth Anchors
            sb.AppendLine("// Character Growth - 人物成长锚点");
            sb.AppendLine("public const int CharacterMaxLevel = 100;                          // MaxLevel: 最高等级");
            sb.AppendLine("public const int CharacterCeilingMax = 3600000;                    // CeilingMax: 极限效率终点（秒），即Ceiling(100) = 1000小时");
            sb.AppendLine("public const int CharacterCeilingExponent = 4;                     // CeilingExponent: 凹凸参数，控制曲线形态（>1下凸，前慢后快）");
            sb.AppendLine("public const int CharacterBattleDuration = 10;                     // BattleDuration: 战斗时长（秒/敌人），单次战斗平均时长");
            sb.AppendLine("public const int CharacterExpPerKillMultiplier = 10;               // ExpPerKill = CurrentLevel × 10");
            sb.AppendLine("public const int CharacterPremiumMultiplier = 9;                   // PremiumMultiplier: 极限效率乘数，满配付费玩家相对于免费玩家的效率倍数");
            sb.AppendLine("public const double CharacterExpMultiplier = 4.0;                  // ExpMultiplier: 人物经验倍率道具效果（随机2~6倍，期望4倍）");
            sb.AppendLine();

            // Skill Growth Anchors
            sb.AppendLine("// Skill Growth - 技能成长锚点");
            sb.AppendLine("public const int SkillMaxLevel = 100;                              // MaxLevel: 技能最高等级");
            sb.AppendLine("public const int SkillCeilingMax = 3600000;                        // CeilingMax: 极限效率终点（秒），与人物相同");
            sb.AppendLine("public const int SkillCeilingExponent = 4;                         // CeilingExponent: 与人物相同");
            sb.AppendLine("public const int SkillUseDuration = 10;                            // UseDuration: 使用时长（秒/次），单次使用平均时长");
            sb.AppendLine("public const int SkillExpPerUse = 1;                               // ExpPerUse: 每次使用获得的经验（固定+1）");
            sb.AppendLine("public const int SkillPremiumMultiplier = 9;                       // PremiumMultiplier: 与人物相同");
            sb.AppendLine("public const double SkillExpMultiplier = 6.0;                      // ExpMultiplier: 技能经验倍率道具效果（随机2~10倍，期望6倍）");
            sb.AppendLine();

            // Pet Growth Anchors
            sb.AppendLine("// Pet Growth - 宠物成长锚点");
            sb.AppendLine("public const int PetMaxLevel = 100;                                // MaxLevel: 宠物最高等级");
            sb.AppendLine("public const int TamingMaxBonus = 10;                              // 驯兽技能100级时的加成倍率");
            sb.AppendLine("public const int PetPremiumMultiplier = 90;                        // PremiumMultiplier: 人物极限效率(9) × 驯兽技能加成(10) = 90");
            sb.AppendLine("public const double PetExpMultiplier = 4.0;                        // ExpMultiplier: 宠物经验倍率道具效果（随机2~6倍，期望4倍）");
            sb.AppendLine();

            // Skill Slot Constants
            sb.AppendLine("// SkillSlot - 技能栏常量");
            sb.AppendLine("public const int DefaultSkillSlots = 4;                            // 默认技能栏数量");
            sb.AppendLine("public const int MaxSkillSlots = 10;                               // 最大技能栏数量");
            sb.AppendLine();

            // Monthly Card Constants (stackable design: Basic + Premium = full benefits)
            sb.AppendLine("// MonthlyCard - 月卡常量（可叠加设计：基础+高级=完整权益）");
            sb.AppendLine("public const double BasicMonthlyCardExpBonus = 0.15;               // 基础月卡经验加成（+15%）");
            sb.AppendLine("public const double PremiumMonthlyCardExpBonus = 0.35;             // 高级月卡经验加成（+35%）");
            sb.AppendLine("public const double BasicMonthlyCardMonthlyExp = 0.8;              // Basic monthly card monthly exp (80%)");
            sb.AppendLine("public const double PremiumMonthlyCardMonthlyExp = 1.2;            // Premium monthly card monthly exp (120%)");
            sb.AppendLine("public const int MonthlyCardDurationDays = 30;                     // 月卡有效期（天）");
            sb.AppendLine("public const int BasicMonthlyCardDailyExpMultiplier = 1;           // 基础月卡每日赠送经验倍率次数（每种类型各1次）");
            sb.AppendLine("public const int PremiumMonthlyCardDailyExpMultiplier = 1;         // 高级月卡每日赠送经验倍率次数（每种类型各1次）");
            sb.AppendLine("public const int BasicMonthlyCardDailyCredit = 2;                  // 基础月卡每日代金币");
            sb.AppendLine("public const int PremiumMonthlyCardDailyCredit = 4;                // 高级月卡每日代金币");
            sb.AppendLine();

            // Currency Exchange Constants
            sb.AppendLine("// CurrencyExchange - 货币兑换常量");
            sb.AppendLine("public const int GemToCreditRatio = 100;                           // 充值币→代金币比例：1:100");
            sb.AppendLine("public const int GemToCreditDailyLimit = 200;                      // 充值币→代金币每日上限");
            sb.AppendLine("public const int CreditToGoldRatio = 10;                           // 代金币→游戏币比例：1:10");
            sb.AppendLine("public const int CreditToGoldDailyLimit = 2000;                    // 代金币→游戏币每日上限");
            sb.AppendLine();

            // Market Tax Constants (stackable: 8% - Basic 2% - Premium 4% = 2%)
            sb.AppendLine("// MarketTax - 市场交易税常量（可叠加减免：8% - 基础2% - 高级4% = 2%）");
            sb.AppendLine("public const double FreePlayerTaxRate = 0.08;                      // 免费玩家交易税率（8%）");
            sb.AppendLine("public const double BasicMonthlyCardTaxReduction = 0.02;           // 基础月卡交易税减免（-2%）");
            sb.AppendLine("public const double PremiumMonthlyCardTaxReduction = 0.04;         // 高级月卡交易税减免（-4%）");
            sb.AppendLine();

            // Consignment Slot Constants (stackable: 5 + Basic 5 + Premium 10 = 20, capped at 15)
            sb.AppendLine("// ConsignmentSlot - 寄售槽位常量（可叠加：5 + 基础5 + 高级10，上限15）");
            sb.AppendLine("public const int FreePlayerConsignmentSlots = 5;                   // 免费玩家寄售槽位");
            sb.AppendLine("public const int BasicMonthlyCardConsignmentSlotsBonus = 5;        // 基础月卡寄售槽位增加");
            sb.AppendLine("public const int PremiumMonthlyCardConsignmentSlotsBonus = 10;     // 高级月卡寄售槽位增加");
            sb.AppendLine("public const int MaxConsignmentSlots = 15;                         // 寄售槽位上限");
            sb.AppendLine();

            return sb.ToString();
        }

    }
}