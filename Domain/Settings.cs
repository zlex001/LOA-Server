using Logic;
using Newtonsoft.Json;

namespace Domain
{
    public class Settings
    {
        private static Settings instance;
        public static Settings Instance { get { if (instance == null) { instance = new Settings(); } return instance; } }
        public void Init()
        {
            // 旧式操作注册（向后兼容）


            // Settings - 新架构注册，清晰分离 - onClick/onConfirm 已合并到 RightPanel
            Domain.Display.Agent.Instance.Register(Option.RightPanel.Settings, build: BuildItems_SettingsRight, onClick: OnSettingsRightPanelClick, onConfirm: OnSettingsConfirm);     // 右侧 UI + 行为

            // Language - 补充完整的面板注册 - onClick 已合并到 RightPanel
            Domain.Display.Agent.Instance.Register(Option.LeftPanel.Language, build: BuildItems_LanguageLeft);       // 左侧 UI
            Domain.Display.Agent.Instance.Register(Option.RightPanel.Language, build: BuildItems_LanguageRight, onClick: OnLanguagePanelClick);     // 右侧 UI + 点击行为

            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Player), OnAddPlayer);
            Logic.Agent.Instance.Content.Remove.Register(typeof(Logic.Player), OnUnbundlePlayer);
        }

        private void OnAddPlayer(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[1];
            player.data.after.Register(Logic.Player.Data.Language, OnAfterPlayerLanguageChanged);
            player.monitor.Register(Logic.Option.RightPanel.Settings, OnSettingsOptionChild);
            player.monitor.Register(Logic.Option.Types.Settings, OnSettingsOptionButton);
            player.monitor.Register(Logic.Option.RightPanel.Language, OnLanguageOptionChild);
            player.monitor.Register(Logic.Option.Types.Language, OnLanguageOptionButton);
        }
        private void OnUnbundlePlayer(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[1];
            player.data.after.Unregister(Logic.Player.Data.Language, OnAfterPlayerLanguageChanged);
            player.monitor.Unregister(Logic.Option.RightPanel.Settings, OnSettingsOptionChild);
            player.monitor.Unregister(Logic.Option.Types.Settings, OnSettingsOptionButton);
            player.monitor.Unregister(Logic.Option.RightPanel.Language, OnLanguageOptionChild);
            player.monitor.Unregister(Logic.Option.Types.Language, OnLanguageOptionButton);
        }
    



        private object[] OnSettingsOptionChild(params object[] args)
        {
            Player sub = (Player)args[0];
            Player obj = (Player)args[1];
            int sceneScale = sub.ScreenUIAdaptation > 0 ? sub.ScreenUIAdaptation : 100;
            var screenSlider = new Logic.Option.Item { type = Logic.Option.Item.Type.Slider, data = new Dictionary<string, string> { { "Id", "ScreenAdaptation" }, { "Text", Domain.Text.Agent.Instance.Get(Logic.Text.Labels.SettingsScreenAdaptive, sub) }, { "SliderValues", JsonConvert.SerializeObject(new int[] { 50, sceneScale, 150 }) }, { "ValueColor", "WHT" } } };
            var fontSlider = new Logic.Option.Item { type = Logic.Option.Item.Type.Slider, data = new Dictionary<string, string> { { "Text", Domain.Text.Agent.Instance.Get(Logic.Text.Labels.SettingsFontScale, sub) }, { "Labels", BuildFontScaleLabels(sub) }, { "SliderValues", JsonConvert.SerializeObject(new int[] { 0, 1, 3 }) }, { "ValueColor", "FFFFFF" } } };
            var language = new Logic.Option.Item(Logic.Option.Item.Type.Button, Domain.Text.Agent.Instance.Get(Logic.Text.Labels.SettingsLanguage, sub));
            var backward = new Logic.Option.Item(Logic.Option.Item.Type.Button, Domain.Text.Agent.Instance.Get(Logic.Text.Labels.SettingsReturnToMenu, sub));
            var exit = new Logic.Option.Item(Logic.Option.Item.Type.Button, Domain.Text.Agent.Instance.Get(Logic.Text.Labels.SettingsQuitGame, sub));
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
                        sub.Create<Logic.Option>(Logic.Option.Types.Language, sub, sub);
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
            
            return player.GenerateOptionItems(Logic.Option.Item.Type.Button, languageNames);
        }
        private void OnLanguageOptionButton(params object[] args)
        {
            Player player = (Player)args[0];
            int index = (int)args[1];
            
            var languages = new[]
            {
                Logic.Text.Languages.Danish, Logic.Text.Languages.Dutch, Logic.Text.Languages.English,
                Logic.Text.Languages.Finnish, Logic.Text.Languages.French, Logic.Text.Languages.German,
                Logic.Text.Languages.Indonesian, Logic.Text.Languages.Italian, Logic.Text.Languages.Japanese,
                Logic.Text.Languages.Korean, Logic.Text.Languages.Norwegian, Logic.Text.Languages.Polish,
                Logic.Text.Languages.Portuguese, Logic.Text.Languages.Russian, Logic.Text.Languages.Spanish,
                Logic.Text.Languages.Swedish, Logic.Text.Languages.Thai, Logic.Text.Languages.Turkish,
                Logic.Text.Languages.Ukrainian, Logic.Text.Languages.Vietnamese, Logic.Text.Languages.ChineseSimplified,
                Logic.Text.Languages.ChineseTraditional
            };
            
            if (index >= 0 && index < languages.Length)
            {
                player.Language = languages[index];
                player.Remove<Logic.Option>();
            }
        }
        private void OnAfterPlayerLanguageChanged(params object[] args)
        {
            Logic.Text.Languages languages = (Logic.Text.Languages)args[0];
            Logic.Player player = (Logic.Player)args[1];
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
            
            var uiTexts = Domain.Text.Agent.Instance.GetUITexts(player.Language);
            Net.Tcp.Instance.Send(player, new Net.Protocol.Texts(uiTexts));
            var startSettingsTexts = Domain.Text.Agent.Instance.GetStartSettingsTexts(player.Language);
            Net.Tcp.Instance.Send(player, new Net.Protocol.StartSettingsTexts(startSettingsTexts));
        }

        private static Net.Protocol.HomeUI CreateHomeUI(Logic.Player player)
        {
            var resourceLabels = new Dictionary<string, string>
            {
                { "Hp", Domain.Text.Agent.Instance.Get(Logic.Text.Labels.Hp, player) },
                { "Mp", Domain.Text.Agent.Instance.Get(Logic.Text.Labels.Mp, player) },
                { "Lp", Domain.Text.Agent.Instance.Get(Logic.Text.Labels.Lp, player) }
            };

            var channels = new List<string>
            {
                Domain.Text.Agent.Instance.Get((int)Logic.Channel.System, player),
                Domain.Text.Agent.Instance.Get((int)Logic.Channel.Private, player),
                Domain.Text.Agent.Instance.Get((int)Logic.Channel.Local, player),
                Domain.Text.Agent.Instance.Get((int)Logic.Channel.All, player),
                Domain.Text.Agent.Instance.Get((int)Logic.Channel.Rumor, player),
                Domain.Text.Agent.Instance.Get((int)Logic.Channel.Automation, player)
            };

            var chatPlaceholder = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.ChatPlaceholder, player);

            return new Net.Protocol.HomeUI(resourceLabels, channels, chatPlaceholder);
        }
        
        private static Net.Protocol.Scene CreateScene(Logic.Player player, Logic.Map map)
        {
            var pos = map.Database.pos;
            var maps = new List<Net.Protocol.Map>();
            string sceneName = "";
            
            var scene = map?.Scene;
            if (scene != null)
            {
                sceneName = Domain.Text.Agent.Instance.Get(scene.Config.Name, player);
                
                foreach (Logic.Map m in scene.Content.Gets<Logic.Map>(m => !(m.Copy != null)))
                {
                    if (m != null)
                    {
                        var name = Domain.Text.Name.Map(m, player);
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
                                Domain.Text.Agent.Instance.Get(map.Scene.Config.Name, player) 
                                : "") 
                            : " ";
                        var baseColor = Net.Protocol.MapColorHelper.GetMapTypeColor(Logic.Map.Types.Default);
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
                    { "Text", Domain.Text.Agent.Instance.Get(Logic.Text.Labels.SettingsScreenAdaptive, player) }, 
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
                    { "Text", Domain.Text.Agent.Instance.Get(Logic.Text.Labels.SettingsFontScale, player) },
                    { "Labels", BuildFontScaleLabels(player) },
                    { "SliderValues", JsonConvert.SerializeObject(new int[] { 0, 1, 3 }) },
                    { "ValueColor", "FFFFFF" }
                } 
            };
            items.Add(fontSlider);

            items.Add(new Option.Item(Option.Item.Type.Button, Domain.Text.Agent.Instance.Get(Logic.Text.Labels.SettingsLanguage, player)));
            items.Add(new Option.Item(Option.Item.Type.Button, Domain.Text.Agent.Instance.Get(Logic.Text.Labels.SettingsReturnToMenu, player)));
            items.Add(new Option.Item(Option.Item.Type.Button, Domain.Text.Agent.Instance.Get(Logic.Text.Labels.SettingsQuitGame, player)));

            return items;
        }

        private string BuildFontScaleLabels(Player player)
        {
            var small = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.SettingsFontSmall, player);
            var standard = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.SettingsFontStandard, player);
            var large = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.SettingsFontLarge, player);
            var extraLarge = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.SettingsFontExtraLarge, player);
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
                        player.Create<Logic.Option>(Logic.Option.Types.Language, player, player);
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
            var currentLangLabel = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.SettingsCurrentLanguage, player);
            items.Add(new Option.Item($"{currentLangLabel}: {GetLanguageName(player.Language)}"));
            return items;
        }

        private List<Option.Item> BuildItems_LanguageRight(Player player, Ability target)
        {
            var items = new List<Option.Item>();
            
            var languages = new[]
            {
                (Logic.Text.Languages.Danish, "Dansk"),
                (Logic.Text.Languages.Dutch, "Nederlands"),
                (Logic.Text.Languages.English, "English"),
                (Logic.Text.Languages.Finnish, "Suomi"),
                (Logic.Text.Languages.French, "Français"),
                (Logic.Text.Languages.German, "Deutsch"),
                (Logic.Text.Languages.Indonesian, "Bahasa Indonesia"),
                (Logic.Text.Languages.Italian, "Italiano"),
                (Logic.Text.Languages.Japanese, "日本語"),
                (Logic.Text.Languages.Korean, "한국어"),
                (Logic.Text.Languages.Norwegian, "Norsk"),
                (Logic.Text.Languages.Polish, "Polski"),
                (Logic.Text.Languages.Portuguese, "Português"),
                (Logic.Text.Languages.Russian, "Русский"),
                (Logic.Text.Languages.Spanish, "Español"),
                (Logic.Text.Languages.Swedish, "Svenska"),
                (Logic.Text.Languages.Thai, "ไทย"),
                (Logic.Text.Languages.Turkish, "Türkçe"),
                (Logic.Text.Languages.Ukrainian, "Українська"),
                (Logic.Text.Languages.Vietnamese, "Tiếng Việt"),
                (Logic.Text.Languages.ChineseSimplified, "简体中文"),
                (Logic.Text.Languages.ChineseTraditional, "繁体中文"),
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
                Logic.Text.Languages.Danish,
                Logic.Text.Languages.Dutch,
                Logic.Text.Languages.English,
                Logic.Text.Languages.Finnish,
                Logic.Text.Languages.French,
                Logic.Text.Languages.German,
                Logic.Text.Languages.Indonesian,
                Logic.Text.Languages.Italian,
                Logic.Text.Languages.Japanese,
                Logic.Text.Languages.Korean,
                Logic.Text.Languages.Norwegian,
                Logic.Text.Languages.Polish,
                Logic.Text.Languages.Portuguese,
                Logic.Text.Languages.Russian,
                Logic.Text.Languages.Spanish,
                Logic.Text.Languages.Swedish,
                Logic.Text.Languages.Thai,
                Logic.Text.Languages.Turkish,
                Logic.Text.Languages.Ukrainian,
                Logic.Text.Languages.Vietnamese,
                Logic.Text.Languages.ChineseSimplified,
                Logic.Text.Languages.ChineseTraditional,
            };
            
            if (index >= 0 && index < languages.Length)
            {
                player.Language = languages[index];
                player.Remove<Logic.Option>();
            }
        }

        private string GetLanguageName(Logic.Text.Languages language)
        {
            return language switch
            {
                Logic.Text.Languages.ChineseSimplified => "简体中文",
                Logic.Text.Languages.ChineseTraditional => "繁体中文",
                Logic.Text.Languages.English => "English",
                Logic.Text.Languages.Japanese => "日本語",
                Logic.Text.Languages.Korean => "한국어",
                Logic.Text.Languages.French => "Français",
                Logic.Text.Languages.German => "Deutsch",
                Logic.Text.Languages.Spanish => "Español",
                Logic.Text.Languages.Portuguese => "Português",
                Logic.Text.Languages.Russian => "Русский",
                Logic.Text.Languages.Turkish => "Türkçe",
                Logic.Text.Languages.Thai => "ไทย",
                Logic.Text.Languages.Indonesian => "Bahasa Indonesia",
                Logic.Text.Languages.Vietnamese => "Tiếng Việt",
                Logic.Text.Languages.Italian => "Italiano",
                Logic.Text.Languages.Polish => "Polski",
                Logic.Text.Languages.Dutch => "Nederlands",
                Logic.Text.Languages.Swedish => "Svenska",
                Logic.Text.Languages.Norwegian => "Norsk",
                Logic.Text.Languages.Danish => "Dansk",
                Logic.Text.Languages.Finnish => "Suomi",
                Logic.Text.Languages.Ukrainian => "Українська",
                _ => "Unknown"
            };
        }

        #endregion
    }
}
