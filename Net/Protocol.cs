using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Logic;

namespace Net.Protocol
{
    public class Option : Base
    {
        public Option() { }

        public List<Logic.Option.Item> lefts;
        public List<Logic.Option.Item> rights;
    }


    public class OptionButton : Base
    {
        public int index;
        public int side;

        public override void Processed(Client client)
        {
            Net.Manager.Instance.monitor.Fire(Net.Manager.Event.OptionButton, client.Player, side, index);
        }
    }



    public class Base : Basic.Element
    {
        public byte[] Encode()
        {
            string jsonStr = JsonConvert.SerializeObject(this);
            return Encoding.UTF8.GetBytes(jsonStr);
        }

        public byte[] EncodeName()
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(GetType().Name);
            Int16 len = (Int16)nameBytes.Length;
            byte[] bytes = new byte[2 + len];
            BitConverter.GetBytes(len).CopyTo(bytes, 0);
            nameBytes.CopyTo(bytes, 2);
            return bytes;
        }

        public virtual void Processed(Client client)
        {
            Net.Tcp.Instance.Remove(client);
        }
    }


    public class Ping : Base
    {
        public DateTime dateTime;
        public override void Processed(Client client)
        {
            DateTime now = DateTime.Now;
            client.Send(new Pong(now));
        }
    }
    public class Pong : Base
    {
        public DateTime dateTime;
        public Pong(DateTime dateTime)
        {
            this.dateTime = dateTime;
        }

    }
    public class Login : Base
    {
        public string id;
        public string password;
        public string platform;
        public string version;
        public string device;
        public string language;

        public override void Processed(Client client)
        {
            client.monitor.Fire(Client.Event.Login, client, id, password, device, version, platform, language);
        }
    }

    public class LoginResponse : Base
    {
        public enum Code
        {
            Success,
            PasswordError,
            AppVersionUnfit,
            UnsafeAccount
        }

        public int code;
        public string message;

        public LoginResponse(Code code, string message = "")
        {
            this.code = (int)code;
            this.message = message;
        }
    }

    public class InitializeRandom : Base
    {
        public override void Processed(Client client)
        {
            client.monitor.Fire(Client.Event.InitializeRandom, client);
        }
    }

    public class Initialize : Base
    {
        public class UI
        {
            public string namePlaceholder;
            public string randomButton;
            public string confirmButton;
            public string errorNameEmpty;
            public string errorNameUnsafe;
        }

        public Initialize(string description, Dictionary<string, int> grade, UI ui)
        {
            this.description = description;
            this.grade = grade;
            this.ui = ui;
        }
        public string description;
        public Dictionary<string, int> grade;
        public UI ui;
    }

    public class InitializeConfirm : Base
    {
        public string name;

        public override void Processed(Client client)
        {
            Net.Manager.Instance.monitor.Fire(Client.Event.InitializeConfirm, client, name);
        }
    }

    public class InitialResponse : Base
    {
        public enum Code
        {
            Success,
            Empty,
            TooLong,
            Unsafe,
            AlreadyExsit,
        }

        public int code;
        public string message;

        public InitialResponse(Code code, string message = "")
        {
            this.code = (int)code;
            this.message = message;
        }
    }


    #region Map and Scene Classes

    public static class SceneColorHelper
    {
        public static string GetSceneTypeColor(string sceneType)
        {
            return sceneType switch
            {
                "平原" => "#7FD858FF",      // 青翠绿
                "丘陵" => "#C8A76EFF",      // 土黄色
                "盆地" => "#8BA870FF",      // 橄榄绿
                "山脉" => "#8B8680FF",      // 山石灰
                "沙漠" => "#EDC967FF",      // 沙漠金
                "火山" => "#E74C3CFF",      // 岩浆红
                "高原" => "#B8956AFF",      // 高原褐
                "湿地" => "#6A8C69FF",      // 沼泽绿
                "冰川" => "#A5D8DDFF",      // 冰雪蓝
                "岛屿" => "#4ECDC4FF",      // 海岛青
                "海岸" => "#5DADE2FF",      // 海岸蓝
                "峡谷" => "#A0522DFF",      // 峡谷棕
                "城市" => "#E8E8E8FF",      // 建筑灰白
                "迷宫" => "#9B59B6FF",      // 神秘紫
                "其他" => "#FFFFFFFF",      // 白色
                _ => "#FFFFFFFF"            // 默认白色
            };
        }
    }

    public static class MapColorHelper
    {
        public static string GetMapTypeColor(Logic.Map.Types type)
        {
            var baseColor = type switch
            {
                // 功能建筑类型
                Logic.Map.Types.Default => "#FFFFFFFF",           // 白色
                Logic.Map.Types.Teleport => "#00CED1FF",          // 青色
                Logic.Map.Types.Bank => "#FFD700FF",              // 金色
                Logic.Map.Types.Market => "#32CD32FF",            // 绿色

                // 服务功能区
                Logic.Map.Types.Guild => "#FFA07AFF",              // 浅橙色
                Logic.Map.Types.FoodShop => "#FFA07AFF",          // 浅橙色
                Logic.Map.Types.PotionShop => "#FFA07AFF",        // 浅橙色
                Logic.Map.Types.MagicShop => "#FFA07AFF",         // 浅橙色

                // 装备店区
                Logic.Map.Types.LightGearShop => "#B0B0B0FF",     // 浅灰
                Logic.Map.Types.HeavyGearShop => "#B0B0B0FF",     // 浅灰

                // 居住区
                Logic.Map.Types.Room => "#F5DEB3FF",              // 米色

                // 执法区
                Logic.Map.Types.PoliceStation => "#4169E1FF",     // 皇室蓝
                Logic.Map.Types.Prison => "#4169E1FF",            // 皇室蓝

                // 生活服务区
                Logic.Map.Types.Restaurant => "#FF6B6BFF",        // 浅红色
                Logic.Map.Types.Hospital => "#FF6B6BFF",          // 浅红色

                // 居住建筑区
                Logic.Map.Types.FarmerHouse => "#D2B48CFF",       // 棕褐色
                Logic.Map.Types.MinerHouse => "#D2B48CFF",        // 棕褐色

                // 特殊建筑区
                Logic.Map.Types.Ruins => "#8B8B83FF",             // 暗灰绿

                // 自然地形类型
                Logic.Map.Types.Road => "#808080FF",              // 灰色
                Logic.Map.Types.Forest => "#1C981CFF",            // 深绿
                Logic.Map.Types.Cave => "#444444FF",              // 暗灰
                Logic.Map.Types.Shore => "#489291FF",             // 青绿
                Logic.Map.Types.Slope => "#F3EA9DFF",             // 浅黄
                Logic.Map.Types.Grass => "#6FC665FF",             // 草绿
                Logic.Map.Types.Snow => "#E0FFFFFF",              // 冰蓝白
                Logic.Map.Types.Swamp => "#5D5026FF",             // 褐色
                Logic.Map.Types.Sand => "#F0CE8EFF",              // 沙黄
                Logic.Map.Types.Bridge => "#8B7355FF",            // 木桥棕
                Logic.Map.Types.Mountain => "#696969FF",          // 山石灰

                // 农业区域类型
                Logic.Map.Types.VegetableGarden => "#7CFC00FF",   // 翠绿
                Logic.Map.Types.Farm => "#9ACD32FF",              // 黄绿
                Logic.Map.Types.Orchard => "#90EE90FF",           // 浅绿
                Logic.Map.Types.HerbGarden => "#3CB371FF",        // 海绿
                Logic.Map.Types.Ranch => "#8B7355FF",             // 棕绿
                Logic.Map.Types.RicePaddy => "#C1E1C1FF",         // 浅绿（稻田）
                Logic.Map.Types.WheatField => "#F4E4B7FF",        // 麦黄
                Logic.Map.Types.MelonField => "#FFB347FF",        // 瓜橙
                Logic.Map.Types.VegetableField => "#90C695FF",    // 菜绿

                // 建筑结构类型
                Logic.Map.Types.Wall => "#F2F2F2FF",              // 浅灰

                // 水体与设施类型
                Logic.Map.Types.Well => "#1E90FFFF",              // 道奇蓝
                Logic.Map.Types.Pond => "#489291FF",              // 青绿

                // 特殊功能类型
                Logic.Map.Types.MazeEntrance => "#00FF00FF",      // 亮绿

                _ => "#FFFFFFFF"                                  // 默认白色
            };

            return baseColor;
        }
    }

    public class Map : Base
    {
        // 已移除业务逻辑构造函数，业务逻辑转移至Domain.ProtocolService

        public Map(string name, int[] pos)
        {
            this.name = name;
            this.pos = pos;
            color = MapColorHelper.GetMapTypeColor(Logic.Map.Types.Default);
        }

        public Map(string name, int[] pos, string color)
        {
            this.name = name;
            this.pos = pos;
            this.color = color;
        }

        public int[] pos;
        public string name;
        public string color;
    }



    public class Scene : Base
    {
        // Net层只负责数据传输，不处理业务逻辑
        // 业务逻辑已移至Domain.ProtocolService.CreateScene
        public Scene(int[] pos, List<Map> maps, string sceneName = "")
        {
            this.pos = pos;
            this.maps = maps;
            this.sceneName = sceneName;
        }

        public int[] pos;
        public List<Map> maps = new List<Map>();
        public string sceneName;

    }

    public class WorldMap : Base
    {
        public class SceneInfo
        {
            public int[] pos;
            public string sceneName;
            public string color;
            public string type;

            public SceneInfo(int[] pos, string sceneName, string color, string type)
            {
                this.pos = pos;
                this.sceneName = sceneName;
                this.color = color;
                this.type = type;
            }
        }

        public List<SceneInfo> scenes;

        public WorldMap(List<SceneInfo> scenes)
        {
            this.scenes = scenes;
        }
    }
    public class Pos : Base
    {
        public int[] content;
        public List<int[]> area = new List<int[]>();

        public Pos(int[] pos, List<int[]> walkableArea)
        {
            content = pos;
            area = walkableArea;
        }
    }
    public class Characters : Base
    {
        public class CharacterData
        {
            public string name;
            public double progress;
            public int hash;
            public int configId;
            
            public CharacterData(string name, double progress, int hash, int configId = 0)
            {
                this.name = name;
                this.progress = progress;
                this.hash = hash;
                this.configId = configId;
            }
        }
        
        public Characters(List<CharacterData> data)
        {
            content = data;
        }

        public List<CharacterData> content = new();
    }

    public class BattleProgress : Base
    {
        public BattleProgress(List<Characters.CharacterData> data)
        {
            content = data;
        }

        public List<Characters.CharacterData> content = new();
    }



    public class HomeUI
    {
        public Dictionary<string, string> resourceLabels;
        public List<string> channels;
        public string chatPlaceholder;

        public HomeUI(Dictionary<string, string> resourceLabels, List<string> channels, string chatPlaceholder)
        {
            this.resourceLabels = resourceLabels;
            this.channels = channels;
            this.chatPlaceholder = chatPlaceholder;
        }
    }

    public class Home : Base
    {
        public Scene scene;
        public Characters characters;
        public Dictionary<string, (int CurrentValue, int MaxValue, string Color)> resouse;
        public List<int[]> area = new List<int[]>();
        public HomeUI ui;

        public Home(Dictionary<string, (int CurrentValue, int MaxValue, string Color)> resources, Scene scene, Characters characters, List<int[]> walkableArea, HomeUI ui)
        {
            this.resouse = resources;
            this.scene = scene;
            this.characters = characters;
            this.area = walkableArea;
            this.ui = ui;
        }
    }

    public class ClickMap : Base
    {
        public int[] pos;
        public override void Processed(Client client)
        {
            Utils.Debug.Log.Info("CLICK", $"[ClickMap.Processed] client={client?.Name}, player={client?.Player}, pos=[{string.Join(",", pos ?? new int[0])}]");
            Logic.Agent.Instance.monitor.Fire(Logic.Player.Click.Map, client.Player, pos);
        }
    }

    public class QueryScene : Base
    {
        public int[] scenePos;
        public override void Processed(Client client)
        {
            Logic.Agent.Instance.monitor.Fire(Logic.Player.Click.Scene, client.Player, scenePos);
        }
    }

    public class ClickCharacter : Base
    {
        public int hash;

        public override void Processed(Client client)
        {
            var characters = GetCharacterListForPlayer(client.Player);
            var target = characters.FirstOrDefault(c => c.GetHashCode() == hash);
            if (target != null)
            {
                Logic.Agent.Instance.monitor.Fire(Logic.Player.Click.Character, client.Player, target);
                return;
            }
            
            // Fallback: Character left view range due to network latency, search globally
            target = FindCharacterByHash(hash);
            if (target != null)
            {
                Logic.Agent.Instance.monitor.Fire(Logic.Player.Click.Character, client.Player, target);
            }
        }

        private Logic.Character FindCharacterByHash(int hash)
        {
            foreach (var life in Logic.Agent.Instance.Content.Gets<Logic.Life>())
            {
                if (life.GetHashCode() == hash) return life;
            }
            foreach (var item in Logic.Agent.Instance.Content.Gets<Logic.Item>())
            {
                if (item.GetHashCode() == hash) return item;
            }
            return null;
        }

        private List<Logic.Character> GetCharacterListForPlayer(Logic.Player player)
        {
            if (player.Parent is Logic.Item container)
            {
                return container.Content.Gets<Logic.Character>().Where(c => c is Logic.Life || c is Logic.Item).ToList();
            }
            else if (player.Parent is Logic.Map)
            {
                if (Logic.Player.GetVisibleCharacters != null)
                {
                    return Logic.Player.GetVisibleCharacters(player);
                }
                return player.Map.Content.Gets<Logic.Character>().ToList();
            }
            return new List<Logic.Character>();
        }
    }



    #endregion

    #region Communication and Messaging

    public class Information : Base
    {
        public Information(Logic.Channel channel, string message)
        {
            this.channel = channel;
            this.message = message;
        }

        public string message;
        public Logic.Channel channel;
    }

    // 🎯 性能监控协议
    public class Performance : Base
    {
        public Performance(long responseTime, double mainLoopFreq, long maxUpdateTime, long slowUpdates)
        {
            this.responseTime = responseTime;
            this.mainLoopFreq = mainLoopFreq;
            this.maxUpdateTime = maxUpdateTime;
            this.slowUpdates = slowUpdates;
            this.timestamp = DateTime.Now;
        }

        public long responseTime;      // 响应时间(ms)
        public double mainLoopFreq;    // 主循环频率(/s)  
        public long maxUpdateTime;     // 最大Update耗时(ms)
        public long slowUpdates;       // 慢更新次数
        public DateTime timestamp;     // 时间戳
    }

    public class Chat : Base
    {
        public string content;

        public override void Processed(Client client)
        {
            Logic.Player player = client.Player;
            player.monitor.Fire(Logic.Player.Event.Chat, player, content);
        }
    }

    #endregion

    #region UI Options



    public class OptionReturn : Base
    {
        public override void Processed(Client client)
        {
            Logic.Player player = client.Player;
            Utils.Debug.Log.Info("OPTION", $"[OptionReturn] Received, player.Option={(player.Option == null ? "null" : player.Option.Type.ToString())}");
            if (player.Option != null)
            {
                player.OptionBackward();
            }
        }
    }





    public class OptionInput : Base
    {
        public string text;

        public override void Processed(Client client)
        {
            Logic.Player player = client.Player;
            if (player.Option != null)
            {
                player.Option.Setting.Input = text;
                player.monitor.Fire(Logic.Option.Event.Refresh, player);
            }
        }
    }

    public class OptionConfirm : Base
    {
        public int index;
        public int side;

        public override void Processed(Client client)
        {
            Net.Manager.Instance.monitor.Fire(Net.Manager.Event.OptionConfirm, client.Player, side, index);
        }
    }

    public class OptionFilter : Protocol.Base
    {
        public int id;
        public string text;

        public override void Processed(Client client)
        {
            Logic.Player player = client.Player;
            if (player.Option != null)
            {
                player.Option.Setting.Filter = text;
                player.monitor.Fire(Logic.Option.Event.Refresh, player);
            }
        }
    }

    public class OptionSlider : Protocol.Base
    {
        public int id;
        public int value;

        public override void Processed(Client client)
        {
            Logic.Player player = client.Player;
            
            // Settings panel has special sliders: id=0 is ScreenUIAdaptation, id=1 is FontScale
            bool isSettingsPanel = player.Option?.Type == Logic.Option.Types.Settings;
            
            if (isSettingsPanel && id == 0)
            {
                if (value >= 50 && value <= 150)
                {
                    player.ScreenUIAdaptation = value;
                    Net.Tcp.Instance.Send(player, new ScreenAdaptation(value));
                    if (player.Option != null)
                    {
                        player.monitor.Fire(Logic.Option.Event.Refresh, player);
                    }
                }
                return;
            }
            
            if (isSettingsPanel && id == 1)
            {
                // FontScale is client-local storage, no server-side persistence needed
                return;
            }
            
            if (player.Option != null)
            {
                if (value >= player.Option.Setting.SliderMin && value <= player.Option.Setting.SliderMax)
                {
                    player.Option.Setting.SliderValue = value;
                    player.monitor.Fire(Logic.Option.Event.Refresh, player);
                }
            }
        }
    }

    public class OptionAmount : Protocol.Base
    {
        public int id;
        public int amount;

        public override void Processed(Client client)
        {
            Logic.Player player = client.Player;
            if (player.Option != null)
            {
                if (amount >= 0)
                {
                    player.Option.Setting.Amount = amount;
                }
            }
        }
    }

    public class OptionToggle : Protocol.Base
    {
        public string id;
        public bool value;

        public override void Processed(Client client)
        {
            Logic.Player player = client.Player;
            if (player.Option != null && player.Option.Setting != null)
            {
                player.Option.Setting.ToggleGroup = player.Option.Setting.ToggleGroup.ToDictionary(kvp => kvp.Key, kvp => kvp.Key == id ? value : false);
                player.monitor.Fire(Logic.Option.Event.Refresh, player);
            }
        }
    }

    #endregion

    #region Payment and IAP

    public class Purchase : Protocol.Base
    {
        public string receipt;

        public override void Processed(Client client)
        {
            if (client.Player.monitor.Check(Logic.Player.Event.Purchase, receipt))
            {
                client.Send(this);
            }
            else
            {
                Net.Tcp.Instance.Remove(client);
            }
        }
    }

    public class IAPReceipts : Protocol.Base
    {
        public List<string> contents;

        public override void Processed(Client client)
        {

        }
    }

    public class AlipayOrder : Base
    {
        /// <summary>
        /// 支付宝订单字符串，客户端可直接用此字符串调起支付宝SDK
        /// </summary>
        public string body;

        public AlipayOrder(string body)
        {
            this.body = body;
        }

        /// <summary>
        /// 客户端处理接收到的支付宝订单信息
        /// </summary>
        /// <param name="client">客户端连接</param>
        public override void Processed(Client client)
        {
        }
    }

    public class RequestIAPReceipts : Base
    {
    }

    #endregion

    #region UI Components

    public class FlyTip : Base
    {
        public FlyTip(string text)
        {
            this.text = text;
        }

        public string text;
    }

    public class NumericalTip : Base
    {
        public NumericalTip(string text, int start, int end)
        {
            this.text = text;
            this.start = start;
            this.end = end;
        }

        public string text;
        public int start;
        public int end;
    }

    public class Story : Base
    {
        public class Line
        {
            public string character;  // 发言者名称，空字符串表示旁白（可以是生物、物品等）
            public string words;      // 对白内容
        }

        public Story(List<Line> dialogues)
        {
            this.dialogues = dialogues;
        }
        public List<Line> dialogues = new List<Line>();
    }

    public class ScreenAdaptation : Base
    {
        public ScreenAdaptation(int value)
        {
            this.value = value;
        }

        public int value;
    }

    public class InputConfirm : Protocol.Base
    {
        public string text;

        public override void Processed(Client client)
        {
            client.Player.monitor.Fire(Logic.Player.Event.CardPurchase, text);
        }
    }

    public class Input : Base
    {
        public string title;
        public string placeholder;
    }

    public class Dark : Base
    {
        public string text;
    }

    #endregion

    public class DataPair : Base
    {
        public enum Type
        {
            Hp,
            Mp,
            Lp
        }
        public Type type;
        public int[] value;
        public string color;

        public DataPair(Type type, int[] value) : this(type, value, null)
        {
        }

        public DataPair(Type type, int[] value, string color)
        {
            this.type = type;
            this.value = value;
            this.color = color;
        }
    }
    public class Data : Base
    {
        public enum Type
        {
            WalkScale,
            State,
        }
        public Type type;
        public int value;
        public Data(Type type, int value)
        {
            this.type = type;
            this.value = value;
        }
    }
    public class QuitToDesktop : Base
    {

    }
    public class Tutorial : Base
    {
        public int step;
        public int targetType;     // 1=UI, 2=Map, 3=Creature, 4=Item
        public int targetId;       // config ID (creature/item)
        public string targetPath;  // UI element path (when targetType == 1)
        public int[] targetPos;    // map position [x,y,z] (when targetType == 2)
        public string hint;

        public Tutorial(int step, int targetType, int targetId, string targetPath, int[] targetPos, string hint)
        {
            this.step = step;
            this.targetType = targetType;
            this.targetId = targetId;
            this.targetPath = targetPath;
            this.targetPos = targetPos;
            this.hint = hint;
        }
    }

    /// <summary>
    /// Upstream protocol: Client sends when Story UI is closed
    /// </summary>
    public class StoryComplete : Base
    {
        public override void Processed(Client client)
        {
            client.Player?.monitor.Fire(Logic.Player.Event.StoryComplete, client.Player);
        }
    }

    public class LanguageChanged : Base
    {
        public string language;

        public LanguageChanged(Logic.Text.Languages lang)
        {
            language = lang.ToString();
        }
    }
}