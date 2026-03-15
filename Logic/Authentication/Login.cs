using Data;
using Net;
using Net.Protocol;

namespace Logic.Authentication
{
    public static class Login
    {
        private const LoginResponse.Code AccountNotFound = (LoginResponse.Code)(-1);

        public static void QuickStart(Client client, string device, string version, string platform, string language, DateTime startTime = default)
        {
            Utils.Debug.Log.Info("AUTH", $"[QuickStart] Start - device={device}, version={version}");
            global::Data.Text.Languages lang = ParseLanguage(language);
            client.data.raw[Client.Data.Language] = lang;
            
            if (!Utils.Mathematics.VersionAdapt(Utils.Text.Version(version), global::Data.Config.Agent.Instance.ClientVersion))
            {
                Utils.Debug.Log.Info("AUTH", $"[QuickStart] Version check failed");
                client.Send(new LoginResponse(LoginResponse.Code.AppVersionUnfit));
                return;
            }
            
            var boundAccountId = Device.GetBoundAccountId(device);
            
            if (!string.IsNullOrEmpty(boundAccountId))
            {
                Utils.Debug.Log.Info("AUTH", $"[QuickStart] Device already bound to account: {boundAccountId}, auto-login");
                var database = global::Data.Database.Agent.Instance.Content.Get<global::Data.Database.Player>(p => p.Id == boundAccountId);
                if (database != null)
                {
                    Do(client, device, boundAccountId, database.text["Pw"], version, platform, language, startTime);
                    return;
                }
                else
                {
                    Utils.Debug.Log.Warning("AUTH", $"[QuickStart] Bound account {boundAccountId} not found in database, creating new account");
                }
            }
            
            var timestamp = DateTime.Now.Ticks;
            var random = Utils.Random.Range(1000, 10000);
            var guestId = $"Traveler{timestamp}{random}";
            var guestName = $"Traveler{random}";
            
            Utils.Debug.Log.Info("AUTH", $"[QuickStart] Creating new guest account: {guestId}");
            
            var newDatabase = new global::Data.Database.Player(guestId, "");
            
            if (!string.IsNullOrEmpty(global::Data.Agent.Instance.ServerId))
            {
                var serverId = global::Data.Agent.Instance.ServerId;
                newDatabase.text["ServerId"] = serverId;
                Utils.Debug.Log.Info("AUTH", $"[QuickStart] Guest {guestId} registered to server {serverId}");
            }
            
            client.data.Change(Client.Data.Device, device);
            client.data.Change(Client.Data.AccountId, guestId);
            client.Add(newDatabase);
            
            Register.GenerateRandomCharacter(newDatabase);
            newDatabase.record["TutorialPhase"] = 1;   // New account: start in tutorial step 1
            Utils.Debug.Log.Info("AUTH", $"[Login.QuickStart] Set TutorialPhase=1 for guest id={guestId}");

            newDatabase.text["Name"] = guestName;
            newDatabase.time["Register"] = newDatabase.time["SignOut"] = DateTime.Now.ToString();
            global::Data.Database.Agent.Instance.AddAsParent(newDatabase);
            
            var map = global::Data.Agent.Instance.Content.Get<global::Data.Map>(m => Enumerable.SequenceEqual(m.Database.pos, newDatabase.pos));
            if (map == null)
            {
                Utils.Debug.Log.Error("AUTH", $"[QuickStart] Cannot find map at position {string.Join(",", newDatabase.pos)} for guest {guestName}");
                client.Send(new LoginResponse(LoginResponse.Code.PasswordError));
                return;
            }
            
            var player = Activator.CreateInstance<global::Data.Player>();
            player.Init(newDatabase);
            client.Player = player;
            map.AddAsParent(player);
            client.Player.data.Full<int>(Life.Data.Mp);
            client.Player.data.Full<double>(Life.Data.Lp);
            foreach (global::Data.Part part in client.Player.Content.Gets<global::Data.Part>())
            {
                part.data.Full<int>(global::Data.Part.Data.Hp);
            }
            
            Register.SyncPlayerDataToDatabase(client.Player);
            
            global::Data.Database.Agent.Instance.Save(global::Data.Config.MySQL.ConnectionString, newDatabase);
            
            Device.Bind(device, guestId);
            
            CompleteLogin(client, device, guestId);
            client.Send(new LoginResponse(LoginResponse.Code.Success, guestId, "", isGuest: true, isNewAccount: true));
            
            Utils.Debug.Log.Info("AUTH", $"[QuickStart] Complete - guest account {guestId} created and logged in");
        }

        public static void Do(Client client, string device, string id, string pw, string version, string platform, string language, DateTime loginStartTime = default)
        {
            Utils.Debug.Log.Info("AUTH", $"[Login.Do] Start - id={id}, version={version}");
            global::Data.Text.Languages lang = ParseLanguage(language);
            client.data.raw[Client.Data.Language] = lang;
            
            if (Can(id, pw, version, out LoginResponse.Code errorCode, out global::Data.Database.Player database))
            {
                Utils.Debug.Log.Info("AUTH", $"[Login.Do] Can() returned true, existing player found");
                if (IsPlayerOnline(id))
                {
                    Utils.Debug.Log.Info("AUTH", $"[Login.Do] Player already online, kicking");
                    KickExistingPlayer(id);
                }
                UpdatePlayerAge(client.Player, database);
                if (IsValidPlayerPosition(database.pos))
                {
                    CreatePlayerAtPosition(client, database, lang);
                }
                else
                {
                    CreatePlayerAtInitialMap(client, database, lang);
                }
                Success(client, device, id, pw);
            }
            else if (errorCode == AccountNotFound)
            {
                Utils.Debug.Log.Info("AUTH", $"[Login.Do] Account not found, starting registration for id={id}");
                Register.Satart(client, device, id, pw);
            }
            else
            {
                Utils.Debug.Log.Info("AUTH", $"[Login.Do] Login failed with error code: {errorCode}");
                client.Send(new LoginResponse(errorCode));
            }
            Utils.Debug.Log.Info("AUTH", $"[Login.Do] End - id={id}");
        }

        private static global::Data.Text.Languages ParseLanguage(string language)
        {
            if (string.IsNullOrEmpty(language) || !Enum.TryParse<global::Data.Text.Languages>(language, out var result))
            {
                return global::Data.Text.Languages.ChineseSimplified;
            }
            return result;
        }

        private static bool Can(string id, string pw, string version, out LoginResponse.Code errorCode, out global::Data.Database.Player database)
        {
            database = null;
            
            if (!Utils.Mathematics.VersionAdapt(Utils.Text.Version(version), global::Data.Config.Agent.Instance.ClientVersion))
            {
                errorCode = LoginResponse.Code.AppVersionUnfit;
                return false;
            }
            
            if (!global::Data.Database.Agent.Instance.Content.Has<global::Data.Database.Player>(pl => pl.Id == id))
            {
                errorCode = AccountNotFound;
                return false;
            }
            
            database = global::Data.Database.Agent.Instance.Content.Get<global::Data.Database.Player>(p => p.Id == id);
            
            // 区服隔离验证：检查玩家是否属于当前区服
            if (!string.IsNullOrEmpty(global::Data.Agent.Instance.ServerId))
            {
                var playerServerId = database.GetText("ServerId");
                if (playerServerId != global::Data.Agent.Instance.ServerId)
                {
                    var currentServerId = global::Data.Agent.Instance.ServerId;
                    Utils.Debug.Log.Warning("AUTH", $"[Login] Player {id} belongs to server {playerServerId}, current server is {currentServerId}");
                    errorCode = LoginResponse.Code.PasswordError;
                    return false;
                }
            }
            
            if (!IsPasswordCorrect(database, pw))
            {
                errorCode = LoginResponse.Code.PasswordError;
                return false;
            }
            
            errorCode = LoginResponse.Code.Success;
            return true;
        }



        private static bool IsPasswordCorrect(global::Data.Database.Player database, string pw)
        {
            return database.text["Pw"] == pw;
        }

        public static bool IsPlayerOnline(string id)
        {
            return global::Data.Agent.Instance.Content.Has<global::Data.Player>(p => p.Id == id);
        }

        private static void KickExistingPlayer(string id)
        {
            var player = global::Data.Agent.Instance.Content.Get<global::Data.Player>(p => p.Id == id);
            if (player.Leader != null)
            {
                Move.Follow.DoUnFollow(player);
            }
            player.Destroy();
        }

        private static bool IsValidPlayerPosition(int[] pos)
        {
            return global::Data.Agent.Instance.Content.Has<global::Data.Map>(m => Enumerable.SequenceEqual(m.Database.pos, pos));
        }

        private static void CreatePlayerAtPosition(Client client, global::Data.Database.Player database, global::Data.Text.Languages language)
        {
            var map = global::Data.Agent.Instance.Content.Get<global::Data.Map>(m => Enumerable.SequenceEqual(m.Database.pos, database.pos));
            var player = Activator.CreateInstance<global::Data.Player>();
            player.Init(database);
            client.Player = player;
            map.AddAsParent(player);
            client.Player.SignIn = DateTime.Now;
            client.Player.Language = language;
        }

        private static void CreatePlayerAtInitialMap(Client client, global::Data.Database.Player database, global::Data.Text.Languages language)
        {
            var map = global::Data.SpawnPoint.GetRandomInitialMap();
            if (map == null) return;

            var player = Activator.CreateInstance<global::Data.Player>();
            player.Init(database);
            client.Player = player;
            map.AddAsParent(player);
            client.Player.SignIn = DateTime.Now;
            client.Player.Language = language;
            database.pos = map.Database.pos;
        }

        public static void Success(Client client, string device, string id, string pw)
        {
            CompleteLogin(client, device, id);
            bool isGuest = id.StartsWith("Traveler");
            client.Send(new LoginResponse(LoginResponse.Code.Success, id, pw, isGuest));
        }

        public static void CompleteLogin(Client client, string device, string id)
        {
            Device.Bind(device, id);
            
            RestoreCompanions(client.Player);

            client.Player.Language = client.Language;

            var p = client.Player;
            var hasTutorial = p.Database.record.TryGetValue("TutorialPhase", out int tutorialVal);
            Utils.Debug.Log.Info("AUTH", $"[CompleteLogin] Before Tutorial.Start playerId={p.Id} record.TutorialPhase present={hasTutorial} value={tutorialVal}");
            // Tutorial: Initialize UI lock state before sending Home protocol
            Tutorial.Instance.Start(client.Player);
            
            var walkableArea = Move.Walk.Area(client.Player);
            
            // 使用统一的资源显示API
            var resources = Display.Agent.GetAllResourcesForDisplay(client.Player);
            
            // 创建场景信息
            var scene = CreateScene(client.Player, client.Player.Map);
            
            // 创建角色信息
            var charactersData = Display.Agent.GetCharactersForDisplay(client.Player);
            var characters = new Net.Protocol.Characters(charactersData);
            
            // 创建UI本地化数据
            var ui = CreateHomeUI(client.Player);
            
            Net.Tcp.Instance.Send(client, new Net.Protocol.Home(resources, scene, characters, walkableArea, ui));

            Tutorial.Instance.SendCurrentPhaseHintIfNeeded(client.Player);

            var uiTexts = Logic.Text.Agent.Instance.GetUITexts(client.Player.Language);
            Net.Tcp.Instance.Send(client, new Net.Protocol.Texts(uiTexts));
            var startSettingsTexts = Logic.Text.Agent.Instance.GetStartSettingsTexts(client.Player.Language);
            Net.Tcp.Instance.Send(client, new Net.Protocol.StartSettingsTexts(startSettingsTexts));

            // 推送世界地图数据
            var worldMap = Display.Agent.CreateWorldMap(client.Player);
            Net.Tcp.Instance.Send(client, worldMap);

            // 推送屏幕自适应值
            int screenAdaptation = client.Player.ScreenUIAdaptation > 0 ? client.Player.ScreenUIAdaptation : 100;
            Net.Tcp.Instance.Send(client, new Net.Protocol.ScreenAdaptation(screenAdaptation));
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


        private static void UpdatePlayerAge(global::Data.Player player, global::Data.Database.Player database)
        {
            var lastSignOutTime = database.GetTime("SignOut");
            var currentTime = DateTime.Now;
            var offlineTime = currentTime - lastSignOutTime;
            var gameTimeElapsed = offlineTime.TotalSeconds * Logic.Time.Agent.Rate;
            var gameDaysElapsed = gameTimeElapsed / 86400.0; // 86400秒 = 1天
            database.record["Age"] = (int)(database.GetRecord("Age")+ gameDaysElapsed);

        }

        private static void RestoreCompanions(global::Data.Player player)
        {
            if (player == null || player.Map == null) return;

            foreach (var comp in player.Database.companions.ToList())
            {
                if (comp.ExpireTime.HasValue && comp.ExpireTime.Value < DateTime.Now)
                {
                    player.Database.companions.Remove(comp);
                    continue;
                }

                var lifeConfig = global::Data.Config.Agent.Instance.Content.Get<global::Data.Config.Life>(l => l.Id == comp.LifeConfigId);
                if (lifeConfig != null)
                {
                    var pet = player.Map.Create<global::Data.Life>(lifeConfig, comp.Level);
                    pet.Leader = player;
                    Logic.Relation.Do(player, pet, Relation.Reason.Help);
                    Logic.Relation.Do(pet, player, Relation.Reason.Help);
                }
            }
        }
    }
}

