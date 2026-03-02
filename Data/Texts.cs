using Utils;

namespace Data
{
    public class Text : Ability
    {

        public class Raw
        {
            public string Value { get; }

            public Raw(string value)
            {
                Value = value;
            }

            public override string ToString() => Value;
        }
        public enum Languages
        {
            Danish = 8,
            Dutch = 9,
            English = 10,
            Finnish = 13,
            French = 14,
            German = 15,
            Indonesian = 20,
            Italian = 21,
            Japanese = 22,
            Korean = 23,
            Norwegian = 26,
            Polish = 27,
            Portuguese = 28,
            Russian = 30,
            Spanish = 34,
            Swedish = 35,
            Thai = 36,
            Turkish = 37,
            Ukrainian = 38,
            Vietnamese = 39,
            ChineseSimplified = 40,
            ChineseTraditional = 41,
        }

        public enum Labels
        {
            Cook, Brew, Forge, Sewing,
            Broken,
            GiveLifeToLife,
            GiveLifeToItem,
            GiveItemToLife,
            GiveItemToItem,
            Appearance,
            Give,
            Acquire,
            NormalExit,
            DeathExit,
            SleepExit,
            DazuoExit,
            MealExit,
            PlayerAddItem,
            SkillUpgrade,
            Drop,
            Pick,
            Carry,
            ItemCountIncrease,
            ItemCountDecrease,
            Male,
            Female,
            Aquire,
            CheckingHotVersion,
            VersionCheckFailed,
            DownloadingResources,
            ExtractComplete,
            ExtractingResources,
            ComparingResources,
            NoUpdateNeeded,
            StartDownloading,
            UpdateComplete,
            DownloadFailed,
            LoadingMetadata,
            LaunchingGame,
            // Start界面文本
            StartTitle,     // 标题
            StartSubtitle,  // 副标题
            StartLogin,     // 登录按钮
            StartRegister,  // 注册按钮
            StartGuest,     // 游客按钮
            StartSettings,  // 设置按钮
            StartExit,      // 退出按钮
            StartTitle1,    // 标题第1个字符
            StartTitle2,    // 标题第2个字符
            StartTitle3,    // 标题第3个字符
            StartTitle4,    // 标题第4个字符
            StartTip,       // 闪烁提示文本
            StartFooter,    // 页脚文本（带占位符）
            Age,
            Status,
            Hp,
            Mp,
            Lp,
            Pp,
            Byes,
            ChatPlaceholder,
            AccountIdPlaceholder,
            AccountPasswordPlaceholder,
            AccountNotePlaceholder,
            ErrorAccountEmpty,
            ErrorAccountFormat,
            ErrorPasswordEmpty,
            ErrorPasswordFormat,
            LoginPasswordError,
            LoginAppVersionUnfit,
            LoginUnsafeAccount,
            InitializeNamePlaceholder,
            InitializeRandomButton,
            InitializeConfirmButton,
            InitializeErrorNameEmpty,
            InitializeErrorNameTooLong,
            InitializeErrorNameUnsafe,
            InitializeErrorNameAlreadyExist,
            InitializeDescription,
            ReceiveToBag,
            ReceiveToEquipment,
            ReceiveToHand,
            EquipDown,

            Sunrise,        // 日出
            Sunset,         // 日落

            Reward,         // 奖励
            Condition,      // 条件
            Event,
            Target,
            PickFail,
            Sell,
            // 装备相关
            ItemEquipped,     // 装备成功
            ItemUnequipped,   // 卸下成功
            EquipUp,     // 装备成功
            UnequipSuccess,   // 卸下成功
            ReceiveFail,      // 装备失败
            UnequipFailed,    // 卸下失败
            NoSpaceForUnequip,

            // 给予相关
            GivePartialSuccess,
            GiveFail,

            // 跟随相关
            FollowStart,      // 开始跟随
            FollowStop,       // 停止跟随

            // 追踪相关
            TrackStart,       // 开始追踪
            TrackStop,        // 停止追踪
            TelescopePrompt,  // 望远镜提示
            Distance,         // 距离

            // 移动相关

            // 方向枚举
            DirectionEast,      // 东
            DirectionSouth,     // 南
            DirectionWest,      // 西
            DirectionNorth,     // 北
            DirectionNortheast, // 东北
            DirectionSoutheast, // 东南
            DirectionSouthwest, // 西南
            DirectionNorthwest, // 西北
            Leave,         // 离开地图
            Enter,         // 进入容器

            // 物品属性相关
            Weight,           // 重量
            Volume,           // 体积
            Capacity,         // 容量
            Durability,       // 耐久度

            // 烹饪相关
            CookSuccess,      // 烹饪成功（保留向后兼容）

            // 制造系统统一消息
            Craft,              // 制作成功

            SignSource,
            SignClue,
            SignClueTalk,
            SignClueArrived,
            SignCluePick,
            SignClueGive,

            // 传送相关
            TeleporterSpeech,  // 驿站马夫说话

            // 设备相关
            DeviceIdMissing,   // 设备ID未提供
            PlatformInvalid,   // 平台参数无效
            JsonParseError,    // JSON解析失败

            // 目击者谴责
            WitnessMurder,          // 目击谋杀
            WitnessAssault,         // 目击攻击  
            WitnessAssaultOfficer,  // 目击袭警
            WitnessRobbery,         // 目击抢劫
            WitnessTheft,           // 目击盗窃
            WitnessJailBreak,       // 目击越狱
            WitnessPrisonBreak,     // 目击劫狱
            WitnessKidnap,          // 目击绑架

            Use,
            Buy,
            BuyFailedForInsufficientMoney,
            Gender,
            Hostile,
            Destroy,
            AttackItem,
            Faint,
            WakeUp,
            Level,
            ItemLocked,
            PartBroken,
            Damage,


            Infant,
            Child,
            Adolescent,
            Young,
            Adult,
            Elderly,
            Centenarian,
            Overhandle,

            Lemurian,
            Atlantean,
            Druk,
            Beastman,
            Demon,
            Animal,
            DescriptionTemplate,

            // 设置面板相关
            SettingsScreenAdaptive,    // 屏幕自适应
            SettingsFontScale,         // 字体大小
            SettingsFontSmall,         // 小
            SettingsFontStandard,      // 标准
            SettingsFontLarge,         // 大
            SettingsFontExtraLarge,    // 超大
            SettingsLanguage,          // 语言
            SettingsReturnToMenu,      // 返回主界面
            SettingsQuitGame,          // 退出江湖
            SettingsCurrentLanguage,   // 当前语言

            // 同伴系统相关
            CompanionSlotFull,         // 驯化槽位已满
            CompanionTameSuccess,      // 驯化成功
            CompanionTameFail,         // 驯化失败
            CompanionDeath,            // 同伴死亡
            CompanionLeadHunt,         // 同伴带领狩猎
            CompanionLeadHuntFollow,   // 主人跟随同伴狩猎

            // 宠物捕捉系统相关
            CaptureNotInterested,      // Animal对Item不感兴趣
            CaptureNeedSkill,          // 缺少驯兽技能
            CaptureSuccess,            // 捕捉成功
            CaptureFail,               // 捕捉失败

            // 商城相关
            GemBalance,                // 充值币余额
            CreditBalance,             // 代金币余额
            Price,                     // 价格
            Total,                     // 总计
            BuyLabel,                  // 购买（简单标签，用于UI）
            MallPurchase,              // 商城购买
            MallInsufficientGem,       // 商城充值币不足
            MallLimitReached,          // 商城限购已满
            MallCannotReceive,         // 商城无法接收
            RechargeCard,              // 卡密充值
            CardInputPlaceholder,      // 卡密输入提示
            CardNotExist,              // 卡号不存在
            GemObtained,               // 获得充值币
            
            // 商城UI相关
            Gem,                       // 充值币
            CardStatus,                // 月卡状态
            NotSubscribed,             // None - general "none" status
            BasicMonthlyCard,          // 基础月卡
            PremiumMonthlyCard,        // 高级月卡
            DaysRemaining,             // 剩余天数
            ExpMultiplier,             // 经验倍率
            Character,                 // 人物
            SkillLabel,                // 技能
            Pet,                       // 宠物
            Uses,                      // 次
            SearchProducts,            // 搜索商品
            ClearSearch,               // 清空搜索
            NoSearchResults,           // 无搜索结果
            MallTypeSubscription,      // 订阅
            MallTypeExperience,        // 经验
            MallTypeEquipment,         // 装备
            MallTypeOther,             // 其他
            MallTypePack,              // 礼包
            
            // Monthly card benefits description
            BasicMonthlyCardBenefits,  // 基础月卡权益说明
            PremiumMonthlyCardBenefits, // 高级月卡权益说明
            SubscriptionActivated,     // 订阅激活
            Recharge,                  // 充值
            
            // Monthly Exp
            MonthlyExp,                // 月卡经验
            ClaimMonthlyExp,           // 领取月卡经验
            MonthlyExpClaimed,         // 领取了月卡经验
            LevelUp,                   // 升级提示
            BlessingEffects,           // 祝福效果

            CannotWalkWhileUnconscious
        }

        #region Singleton

        private static Text instance;
        public static Text Instance { get { if (instance == null) { instance = new Text(); } return instance; } }

        #endregion

        public Dictionary<int, Dictionary<Languages, string>> Multilingual { get; set; } = new Dictionary<int, Dictionary<Languages, string>>();

        public Dictionary<Labels, int[]> Label { get; private set; } = new Dictionary<Labels, int[]>();
        
        public Dictionary<string, int> CidToId { get; private set; } = new Dictionary<string, int>();

        public override void Init(params object[] args)
        {

            List<Dictionary<string, object>> dicts = Utils.Csv.LoadAsRows($"{Utils.Paths.Config}/Multilingual.csv");
            Dictionary<Labels, List<int>> temp = new();
            foreach (var dict in dicts)
            {
                int id = Convert.ToInt32(dict["id"]);
                string cid = Convert.ToString(dict["cid"]);
                string label = Convert.ToString(dict["label"]);
                Multilingual[id] = Enum.GetValues(typeof(Languages)).Cast<Languages>().ToDictionary(lang => lang, lang => Convert.ToString(dict[lang.ToString()]));
                
                // Build cid -> id mapping
                if (!string.IsNullOrEmpty(cid))
                {
                    CidToId[cid] = id;
                }
                
                if (!string.IsNullOrEmpty(label))
                {
                    Labels key = (Labels)Enum.Parse(typeof(Labels), label);
                    if (!temp.TryGetValue(key, out var list))
                    {
                        list = temp[key] = new List<int>();
                    }
                    list.Add(id);
                }
            }
            Label = temp.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
        }
    }
}
