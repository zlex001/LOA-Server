using Data;
using Newtonsoft.Json;

namespace Logic
{
    public class Settings
    {
        private static Settings instance;
        public static Settings Instance { get { if (instance == null) { instance = new Settings(); } return instance; } }
        public void Init()
        {
            // 旧式操作注册（向后兼容）


            // Settings - 新架构注册，清晰分离 - onClick/onConfirm 已合并到 RightPanel
            Logic.Display.Agent.Instance.Register(Option.RightPanel.Settings, build: BuildItems_SettingsRight, onClick: OnSettingsRightPanelClick, onConfirm: OnSettingsConfirm);     // 右侧 UI + 行为

            // Language - 补充完整的面板注册 - onClick 已合并到 RightPanel
            Logic.Display.Agent.Instance.Register(Option.LeftPanel.Language, build: BuildItems_LanguageLeft);       // 左侧 UI
            Logic.Display.Agent.Instance.Register(Option.RightPanel.Language, build: BuildItems_LanguageRight, onClick: OnLanguagePanelClick);     // 右侧 UI + 点击行为

            global::Data.Agent.Instance.Content.Add.Register(typeof(global::Data.Player), OnAddPlayer);
            global::Data.Agent.Instance.Content.Remove.Register(typeof(global::Data.Player), OnUnbundlePlayer);
        }

        private void OnAddPlayer(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[1];
            player.data.after.Register(global::Data.Player.Data.Language, OnAfterPlayerLanguageChanged);
            player.monitor.Register(global::Data.Option.RightPanel.Settings, OnSettingsOptionChild);
            player.monitor.Register(global::Data.Option.Types.Settings, OnSettingsOptionButton);
            player.monitor.Register(global::Data.Option.RightPanel.Language, OnLanguageOptionChild);
            player.monitor.Register(global::Data.Option.Types.Language, OnLanguageOptionButton);
        }
        private void OnUnbundlePlayer(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[1];
            player.data.after.Unregister(global::Data.Player.Data.Language, OnAfterPlayerLanguageChanged);
            player.monitor.Unregister(global::Data.Option.RightPanel.Settings, OnSettingsOptionChild);
            player.monitor.Unregister(global::Data.Option.Types.Settings, OnSettingsOptionButton);
            player.monitor.Unregister(global::Data.Option.RightPanel.Language, OnLanguageOptionChild);
            player.monitor.Unregister(global::Data.Option.Types.Language, OnLanguageOptionButton);
        }
    



        private object[] OnSettingsOptionChild(params object[] args)
        {
            Player sub = (Player)args[0];
            Player obj = (Player)args[1];
            int sceneScale = sub.ScreenUIAdaptation > 0 ? sub.ScreenUIAdaptation : 100;
            var screenSlider = new global::Data.Option.Item { type = global::Data.Option.Item.Type.Slider, data = new Dictionary<string, string> { { "Id", "ScreenAdaptation" }, { "Text", Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.SettingsScreenAdaptive, sub) }, { "SliderValues", JsonConvert.SerializeObject(new int[] { 50, sceneScale, 150 }) }, { "ValueColor", "WHT" } } };
            var fontSlider = new global::Data.Option.Item { type = global::Data.Option.Item.Type.Slider, data = new Dictionary<string, string> { { "Text", Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.SettingsFontScale, sub) }, { "Labels", BuildFontScaleLabels(sub) }, { "SliderValues", JsonConvert.SerializeObject(new int[] { 0, 1, 3 }) }, { "ValueColor", "FFFFFF" } } };
            var language = new global::Data.Option.Item(global::Data.Option.Item.Type.Button, Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.SettingsLanguage, sub));
            var backward = new global::Data.Option.Item(global::Data.Option.Item.Type.Button, Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.SettingsReturnToMenu, sub));
            var exit = new global::Data.Option.Item(global::Data.Option.Item.Type.Button, Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.SettingsQuitGame, sub));
            return new object[] { screenSlider, fontSlider, language, backward, exit }.ToArray();
        }

        private void OnSettingsOptionButton(params object[] args)
        {
            Player sub = (Player)args[0];
            int index = (int)args[1];
            // Skip first 2 sliders (ScreenUIAdaptation, FontScale), button index starts from 0
            int buttonIndex = index - 2;
            if (buttonIndex >= 0 && buttonIndex < 3)
            {
                switch (buttonIndex)
                {
                    case 0:
                        sub.Create<global::Data.Option>(global::Data.Option.Types.Language, sub, sub);
                        break;
                    case 1:
                        sub.monitor.Fire(Player.Event.QuitToMenu);
                        break;
                    case 2:
                        sub.monitor.Fire(Player.Event.QuitToDesktop);
                        break;
                }
            }
        }

        private object[] OnLanguageOptionChild(params object[] args)
        {
            Player player = (Player)args[0];
            
            var languageNames = new[]
            {
                "Dansk", "Nederlands", "English", "Suomi", "Français", "Deutsch",
                "Bahasa Indonesia", "Italiano", "日本語", "한국어", "Norsk", "Polski",
                "Português", "Русский", "Español", "Svenska", "ไทย", "Türkçe",
                "Українська", "Tiếng Việt", "简体中文", "繁体中文"
            };
            
            return player.GenerateOptionItems(global::Data.Option.Item.Type.Button, languageNames);
        }
        private void OnLanguageOptionButton(params object[] args)
        {
            Player player = (Player)args[0];
            int index = (int)args[1];
            
            var languages = new[]
            {
                global::Data.Text.Languages.Danish, global::Data.Text.Languages.Dutch, global::Data.Text.Languages.English,
                global::Data.Text.Languages.Finnish, global::Data.Text.Languages.French, global::Data.Text.Languages.German,
                global::Data.Text.Languages.Indonesian, global::Data.Text.Languages.Italian, global::Data.Text.Languages.Japanese,
                global::Data.Text.Languages.Korean, global::Data.Text.Languages.Norwegian, global::Data.Text.Languages.Polish,
                global::Data.Text.Languages.Portuguese, global::Data.Text.Languages.Russian, global::Data.Text.Languages.Spanish,
                global::Data.Text.Languages.Swedish, global::Data.Text.Languages.Thai, global::Data.Text.Languages.Turkish,
                global::Data.Text.Languages.Ukrainian, global::Data.Text.Languages.Vietnamese, global::Data.Text.Languages.ChineseSimplified,
                global::Data.Text.Languages.ChineseTraditional
            };
            
            if (index >= 0 && index < languages.Length)
            {
                player.Language = languages[index];
                player.Remove<global::Data.Option>();
            }
        }
        private void OnAfterPlayerLanguageChanged(params object[] args)
        {
            global::Data.Text.Languages languages = (global::Data.Text.Languages)args[0];
            global::Data.Player player = (global::Data.Player)args[1];
            var walkableArea = Move.Walk.Area(player);
            
            // 使用统一的资源显示API
            var resources = Display.Agent.GetAllResourcesForDisplay(player);
            
            // 创建场景信息
            var scene = CreateScene(player, player.Map);
            
            // 创建角色信息
            var charactersData = Display.Agent.GetCharactersForDisplay(player);
            var characters = new Net.Protocol.Characters(charactersData);
            
            // 创建UI本地化数据
            var ui = CreateHomeUI(player);
            
            Net.Tcp.Instance.Send(player, new Net.Protocol.Home(resources, scene, characters, walkableArea, ui));
            
            // 单独发送Characters协议确保角色列表更新
            Net.Tcp.Instance.Send(player, characters);
            
            // 推送世界地图数据
            var worldMap = Display.Agent.CreateWorldMap(player);
            Net.Tcp.Instance.Send(player, worldMap);
            
            var uiTexts = Logic.Text.Agent.Instance.GetUITexts(player.Language);
            Net.Tcp.Instance.Send(player, new Net.Protocol.Texts(uiTexts));
            var startSettingsTexts = Logic.Text.Agent.Instance.GetStartSettingsTexts(player.Language);
            Net.Tcp.Instance.Send(player, new Net.Protocol.StartSettingsTexts(startSettingsTexts));
        }

        private static Net.Protocol.HomeUI CreateHomeUI(global::Data.Player player)
        {
            var resourceLabels = new Dictionary<string, string>
            {
                { "Hp", Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.Hp, player) },
                { "Mp", Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.Mp, player) },
                { "Lp", Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.Lp, player) }
            };

            var channels = new List<string>
            {
                Logic.Text.Agent.Instance.Get((int)global::Data.Channel.System, player),
                Logic.Text.Agent.Instance.Get((int)global::Data.Channel.Private, player),
                Logic.Text.Agent.Instance.Get((int)global::Data.Channel.Local, player),
                Logic.Text.Agent.Instance.Get((int)global::Data.Channel.All, player),
                Logic.Text.Agent.Instance.Get((int)global::Data.Channel.Rumor, player),
                Logic.Text.Agent.Instance.Get((int)global::Data.Channel.Automation, player)
            };

            var chatPlaceholder = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.ChatPlaceholder, player);

            return new Net.Protocol.HomeUI(resourceLabels, channels, chatPlaceholder);
        }
        
        private static Net.Protocol.Scene CreateScene(global::Data.Player player, global::Data.Map map)
        {
            var pos = map.Database.pos;
            var maps = new List<Net.Protocol.Map>();
            string sceneName = "";
            
            var scene = map?.Scene;
            if (scene != null)
            {
                sceneName = Logic.Text.Agent.Instance.Get(scene.Config.Name, player);
                
                foreach (global::Data.Map m in scene.Content.Gets<global::Data.Map>(m => !(m.Copy != null)))
                {
                    if (m != null)
                    {
                        var name = Logic.Text.Name.Map(m, player);
                        var mapPos = m.Database.pos;
                        var baseColor = Net.Protocol.MapColorHelper.GetMapTypeColor(m.Type);
                        var color = Lighting.Instance.ApplyWorldLighting(baseColor);
                        maps.Add(new Net.Protocol.Map(name, mapPos, color));
                    }
                }
            }
            else
            {
                int startX = map.Database.pos[0] - 1;
                int endX = map.Database.pos[0] + 1;
                int startY = map.Database.pos[1] - 1;
                int endY = map.Database.pos[1] + 1;
                
                for (int x = startX; x <= endX; x++)
                {
                    for (int y = startY; y <= endY; y++)
                    {
                        string name = (x == map.Database.pos[0] && y == map.Database.pos[1]) ? 
                            (map.Scene != null ? 
                                Logic.Text.Agent.Instance.Get(map.Scene.Config.Name, player) 
                                : "") 
                            : " ";
                        var baseColor = Net.Protocol.MapColorHelper.GetMapTypeColor(global::Data.Map.Types.Default);
                        var color = Lighting.Instance.ApplyWorldLighting(baseColor);
                        maps.Add(new Net.Protocol.Map(name, new int[] { x, y, map.Database.pos[2] }, color));
                    }
                }
            }
            
            return new Net.Protocol.Scene(pos, maps, sceneName);
        }



        #region Settings New Architecture Methods

        private List<Option.Item> BuildItems_SettingsRight(Player player, Ability target)
        {
            var items = new List<Option.Item>();

            int sceneScale = player.ScreenUIAdaptation > 0 ? player.ScreenUIAdaptation : 100;
            var screenSlider = new Option.Item 
            { 
                type = Option.Item.Type.Slider, 
                data = new Dictionary<string, string> 
                { 
                    { "Id", "ScreenAdaptation" },
                    { "Text", Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.SettingsScreenAdaptive, player) }, 
                    { "SliderValues", JsonConvert.SerializeObject(new int[] { 50, sceneScale, 150 }) }, 
                    { "ValueColor", "WHT" } 
                } 
            };
            items.Add(screenSlider);

            var fontSlider = new Option.Item 
            { 
                type = Option.Item.Type.Slider, 
                data = new Dictionary<string, string> 
                { 
                    { "Text", Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.SettingsFontScale, player) },
                    { "Labels", BuildFontScaleLabels(player) },
                    { "SliderValues", JsonConvert.SerializeObject(new int[] { 0, 1, 3 }) },
                    { "ValueColor", "FFFFFF" }
                } 
            };
            items.Add(fontSlider);

            items.Add(new Option.Item(Option.Item.Type.Button, Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.SettingsLanguage, player)));
            items.Add(new Option.Item(Option.Item.Type.Button, Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.SettingsReturnToMenu, player)));
            items.Add(new Option.Item(Option.Item.Type.Button, Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.SettingsQuitGame, player)));

            return items;
        }

        private string BuildFontScaleLabels(Player player)
        {
            var small = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.SettingsFontSmall, player);
            var standard = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.SettingsFontStandard, player);
            var large = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.SettingsFontLarge, player);
            var extraLarge = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.SettingsFontExtraLarge, player);
            return $"{small}|{standard}|{large}|{extraLarge}";
        }

        private void OnSettingsConfirm(Player player, Ability target, int index)
        {
            // Settings 的确认操作（如果需要的话）
        }

        private void OnSettingsPanelClick(Player player, Ability target, int index)
        {
            // 跳过前两个滑块（屏幕自适应、字体大小），从按钮开始计算
            int buttonIndex = index - 2;
            
            if (buttonIndex >= 0)
            {
                switch (buttonIndex)
                {
                    case 0: // 语言
                        player.Create<global::Data.Option>(global::Data.Option.Types.Language, player, player);
                        break;
                    case 1: // 返回主界面
                        player.monitor.Fire(Player.Event.QuitToMenu);
                        break;
                    case 2: // 退出江湖
                        player.monitor.Fire(Player.Event.QuitToDesktop);
                        break;
                    default:
                        break;
                }
            }
        }

        private void OnSettingsRightPanelClick(Player player, Ability target, int index)
        {
            // 右侧面板点击逻辑与通用面板点击相同
            OnSettingsPanelClick(player, target, index);
        }

        #endregion

        #region Language Panel Methods

        private List<Option.Item> BuildItems_LanguageLeft(Player player, Ability target)
        {
            var items = new List<Option.Item>();
            var currentLangLabel = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.SettingsCurrentLanguage, player);
            items.Add(new Option.Item($"{currentLangLabel}: {GetLanguageName(player.Language)}"));
            return items;
        }

        private List<Option.Item> BuildItems_LanguageRight(Player player, Ability target)
        {
            var items = new List<Option.Item>();
            
            var languages = new[]
            {
                (global::Data.Text.Languages.Danish, "Dansk"),
                (global::Data.Text.Languages.Dutch, "Nederlands"),
                (global::Data.Text.Languages.English, "English"),
                (global::Data.Text.Languages.Finnish, "Suomi"),
                (global::Data.Text.Languages.French, "Français"),
                (global::Data.Text.Languages.German, "Deutsch"),
                (global::Data.Text.Languages.Indonesian, "Bahasa Indonesia"),
                (global::Data.Text.Languages.Italian, "Italiano"),
                (global::Data.Text.Languages.Japanese, "日本語"),
                (global::Data.Text.Languages.Korean, "한국어"),
                (global::Data.Text.Languages.Norwegian, "Norsk"),
                (global::Data.Text.Languages.Polish, "Polski"),
                (global::Data.Text.Languages.Portuguese, "Português"),
                (global::Data.Text.Languages.Russian, "Русский"),
                (global::Data.Text.Languages.Spanish, "Español"),
                (global::Data.Text.Languages.Swedish, "Svenska"),
                (global::Data.Text.Languages.Thai, "ไทย"),
                (global::Data.Text.Languages.Turkish, "Türkçe"),
                (global::Data.Text.Languages.Ukrainian, "Українська"),
                (global::Data.Text.Languages.Vietnamese, "Tiếng Việt"),
                (global::Data.Text.Languages.ChineseSimplified, "简体中文"),
                (global::Data.Text.Languages.ChineseTraditional, "繁体中文"),
            };
            
            for (int i = 0; i < languages.Length; i++)
            {
                items.Add(new Option.Item(Option.Item.Type.Button, languages[i].Item2));
            }
            
            return items;
        }

        private void OnLanguagePanelClick(Player player, Ability target, int index)
        {
            var languages = new[]
            {
                global::Data.Text.Languages.Danish,
                global::Data.Text.Languages.Dutch,
                global::Data.Text.Languages.English,
                global::Data.Text.Languages.Finnish,
                global::Data.Text.Languages.French,
                global::Data.Text.Languages.German,
                global::Data.Text.Languages.Indonesian,
                global::Data.Text.Languages.Italian,
                global::Data.Text.Languages.Japanese,
                global::Data.Text.Languages.Korean,
                global::Data.Text.Languages.Norwegian,
                global::Data.Text.Languages.Polish,
                global::Data.Text.Languages.Portuguese,
                global::Data.Text.Languages.Russian,
                global::Data.Text.Languages.Spanish,
                global::Data.Text.Languages.Swedish,
                global::Data.Text.Languages.Thai,
                global::Data.Text.Languages.Turkish,
                global::Data.Text.Languages.Ukrainian,
                global::Data.Text.Languages.Vietnamese,
                global::Data.Text.Languages.ChineseSimplified,
                global::Data.Text.Languages.ChineseTraditional,
            };
            
            if (index >= 0 && index < languages.Length)
            {
                player.Language = languages[index];
                player.Remove<global::Data.Option>();
            }
        }

        private string GetLanguageName(global::Data.Text.Languages language)
        {
            return language switch
            {
                global::Data.Text.Languages.ChineseSimplified => "简体中文",
                global::Data.Text.Languages.ChineseTraditional => "繁体中文",
                global::Data.Text.Languages.English => "English",
                global::Data.Text.Languages.Japanese => "日本語",
                global::Data.Text.Languages.Korean => "한국어",
                global::Data.Text.Languages.French => "Français",
                global::Data.Text.Languages.German => "Deutsch",
                global::Data.Text.Languages.Spanish => "Español",
                global::Data.Text.Languages.Portuguese => "Português",
                global::Data.Text.Languages.Russian => "Русский",
                global::Data.Text.Languages.Turkish => "Türkçe",
                global::Data.Text.Languages.Thai => "ไทย",
                global::Data.Text.Languages.Indonesian => "Bahasa Indonesia",
                global::Data.Text.Languages.Vietnamese => "Tiếng Việt",
                global::Data.Text.Languages.Italian => "Italiano",
                global::Data.Text.Languages.Polish => "Polski",
                global::Data.Text.Languages.Dutch => "Nederlands",
                global::Data.Text.Languages.Swedish => "Svenska",
                global::Data.Text.Languages.Norwegian => "Norsk",
                global::Data.Text.Languages.Danish => "Dansk",
                global::Data.Text.Languages.Finnish => "Suomi",
                global::Data.Text.Languages.Ukrainian => "Українська",
                _ => "Unknown"
            };
        }

        #endregion
    }
}
