using Logic;
using Net;
using Net.Protocol;

namespace Domain.Authentication
{
    public static class Login
    {
        private const LoginResponse.Code AccountNotFound = (LoginResponse.Code)(-1);

        public static void QuickStart(Client client, string device, string version, string platform, string language, DateTime startTime = default)
        {
            Utils.Debug.Log.Info("AUTH", $"[QuickStart] Start - device={device}, version={version}");
            Logic.Text.Languages lang = ParseLanguage(language);
            client.data.raw[Client.Data.Language] = lang;
            
            if (!Utils.Mathematics.VersionAdapt(Utils.Text.Version(version), Logic.Config.Agent.Instance.ClientVersion))
            {
                Utils.Debug.Log.Info("AUTH", $"[QuickStart] Version check failed");
                string errorMessage = GetErrorMessage(LoginResponse.Code.AppVersionUnfit, lang);
                client.Send(new LoginResponse(LoginResponse.Code.AppVersionUnfit, errorMessage));
                return;
            }
            
            var boundAccountId = Device.GetBoundAccountId(device);
            
            if (!string.IsNullOrEmpty(boundAccountId))
            {
                Utils.Debug.Log.Info("AUTH", $"[QuickStart] Device already bound to account: {boundAccountId}, auto-login");
                var database = Logic.Database.Agent.Instance.Content.Get<Logic.Database.Player>(p => p.Id == boundAccountId);
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
            
            var timestamp = Utils.DateTime.CurrentTimeMillis();
            var random = Utils.Random.Int(1000, 9999);
            var guestId = $"Traveler{timestamp}{random}";
            var guestName = $"Traveler{random}";
            
            Utils.Debug.Log.Info("AUTH", $"[QuickStart] Creating new guest account: {guestId}");
            
            var newDatabase = new Logic.Database.Player(guestId, "");
            
            if (!string.IsNullOrEmpty(Logic.Agent.Instance.ServerId))
            {
                newDatabase.text["ServerId"] = Logic.Agent.Instance.ServerId;
                Utils.Debug.Log.Info("AUTH", $"[QuickStart] Guest {guestId} registered to server {Logic.Agent.Instance.ServerId}");
            }
            
            client.data.Change(Client.Data.Device, device);
            client.data.Change(Client.Data.AccountId, guestId);
            client.Add(newDatabase);
            
            Register.GenerateRandomCharacter(newDatabase);
            
            newDatabase.text["Name"] = guestName;
            newDatabase.time["Register"] = newDatabase.time["SignOut"] = DateTime.Now.ToString();
            Logic.Database.Agent.Instance.AddAsParent(newDatabase);
            
            var map = Logic.Agent.Instance.Content.Get<Logic.Map>(m => Enumerable.SequenceEqual(m.Database.pos, newDatabase.pos));
            if (map == null)
            {
                Utils.Debug.Log.Error("AUTH", $"[QuickStart] Cannot find map at position {string.Join(",", newDatabase.pos)} for guest {guestName}");
                string errorMessage = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.InitializeErrorNameEmpty, client.Language);
                client.Send(new LoginResponse(LoginResponse.Code.PasswordError, errorMessage));
                return;
            }
            
            client.Player = map.Create<Logic.Player>(newDatabase);
            client.Player.data.Full<int>(Life.Data.Mp);
            client.Player.data.Full<double>(Life.Data.Lp);
            foreach (Logic.Part part in client.Player.Content.Gets<Logic.Part>())
            {
                part.data.Full<int>(Logic.Part.Data.Hp);
            }
            
            Register.SyncPlayerDataToDatabase(client.Player);
            
            Logic.Database.Agent.Instance.Save(Logic.Config.MySQL.ConnectionString, newDatabase);
            
            Device.Bind(device, guestId);
            
            CompleteLogin(client, device, guestId);
            client.Send(new LoginResponse(LoginResponse.Code.Success, ""));
            
            Utils.Debug.Log.Info("AUTH", $"[QuickStart] Complete - guest account {guestId} created and logged in");
        }

        public static void Do(Client client, string device, string id, string pw, string version, string platform, string language, DateTime loginStartTime = default)
        {
            Utils.Debug.Log.Info("AUTH", $"[Login.Do] Start - id={id}, version={version}");
            Logic.Text.Languages lang = ParseLanguage(language);
            client.data.raw[Client.Data.Language] = lang;
            
            if (Can(id, pw, version, out LoginResponse.Code errorCode, out Logic.Database.Player database))
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
                Success(client, device, id);
            }
            else if (errorCode == AccountNotFound)
            {
                Utils.Debug.Log.Info("AUTH", $"[Login.Do] Account not found, starting registration for id={id}");
                Register.Satart(client, device, id, pw);
            }
            else
            {
                Utils.Debug.Log.Info("AUTH", $"[Login.Do] Login failed with error code: {errorCode}");
                string errorMessage = GetErrorMessage(errorCode, lang);
                client.Send(new LoginResponse(errorCode, errorMessage));
            }
            Utils.Debug.Log.Info("AUTH", $"[Login.Do] End - id={id}");
        }

        private static Logic.Text.Languages ParseLanguage(string language)
        {
            if (string.IsNullOrEmpty(language) || !Enum.TryParse<Logic.Text.Languages>(language, out var result))
            {
                return Logic.Text.Languages.ChineseSimplified;
            }
            return result;
        }

        private static bool Can(string id, string pw, string version, out LoginResponse.Code errorCode, out Logic.Database.Player database)
        {
            database = null;
            
            if (!Utils.Mathematics.VersionAdapt(Utils.Text.Version(version), Logic.Config.Agent.Instance.ClientVersion))
            {
                errorCode = LoginResponse.Code.AppVersionUnfit;
                return false;
            }
            
            if (!Logic.Database.Agent.Instance.Content.Has<Logic.Database.Player>(pl => pl.Id == id))
            {
                errorCode = AccountNotFound;
                return false;
            }
            
            database = Logic.Database.Agent.Instance.Content.Get<Logic.Database.Player>(p => p.Id == id);
            
            // 区服隔离验证：检查玩家是否属于当前区服
            if (!string.IsNullOrEmpty(Logic.Agent.Instance.ServerId))
            {
                var playerServerId = database.GetText("ServerId");
                if (playerServerId != Logic.Agent.Instance.ServerId)
                {
                    Utils.Debug.Log.Warning("AUTH", $"[Login] Player {id} belongs to server {playerServerId}, current server is {Logic.Agent.Instance.ServerId}");
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



        private static bool IsPasswordCorrect(Logic.Database.Player database, string pw)
        {
            return database.text["Pw"] == pw;
        }

        public static bool IsPlayerOnline(string id)
        {
            return Logic.Agent.Instance.Content.Has<Logic.Player>(p => p.Id == id);
        }

        private static void KickExistingPlayer(string id)
        {
            var player = Logic.Agent.Instance.Content.Get<Logic.Player>(p => p.Id == id);
            if (player.Leader != null)
            {
                Move.Follow.DoUnFollow(player);
            }
            player.Destroy();
        }

        private static bool IsValidPlayerPosition(int[] pos)
        {
            return Logic.Agent.Instance.Content.Has<Logic.Map>(m => Enumerable.SequenceEqual(m.Database.pos, pos));
        }

        private static void CreatePlayerAtPosition(Client client, Logic.Database.Player database, Logic.Text.Languages language)
        {
            var map = Logic.Agent.Instance.Content.Get<Logic.Map>(m => Enumerable.SequenceEqual(m.Database.pos, database.pos));
            client.Player = map.Create<Logic.Player>(database);
            client.Player.SignIn = DateTime.Now;
            client.Player.Language = language;
        }

        private static void CreatePlayerAtInitialMap(Client client, Logic.Database.Player database, Logic.Text.Languages language)
        {
            var map = Logic.SpawnPoint.GetRandomInitialMap();
            if (map == null) return;
            
            client.Player = map.Create<Logic.Player>(database);
            client.Player.SignIn = DateTime.Now;
            client.Player.Language = language;
            database.pos = map.Database.pos;
        }

        public static void Success(Client client, string device, string id)
        {
            CompleteLogin(client, device, id);
            client.Send(new LoginResponse(LoginResponse.Code.Success, ""));
        }

        private static string GetErrorMessage(LoginResponse.Code code, Logic.Text.Languages language)
        {
            Logic.Text.Labels label = code switch
            {
                LoginResponse.Code.PasswordError => Logic.Text.Labels.LoginPasswordError,
                LoginResponse.Code.AppVersionUnfit => Logic.Text.Labels.LoginAppVersionUnfit,
                LoginResponse.Code.UnsafeAccount => Logic.Text.Labels.LoginUnsafeAccount,
                _ => Logic.Text.Labels.LoginPasswordError
            };
            return Domain.Text.Agent.Instance.Get(label, language);
        }

        public static void CompleteLogin(Client client, string device, string id)
        {
            Device.Bind(device, id);
            
            RestoreCompanions(client.Player);
            
            // Tutorial: Initialize UI lock state before sending Home protocol
            // This ensures UILock protocol is sent before Home, so client knows which panels to show
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
            
            Net.Tcp.Instance.Send(client.Player, new Net.Protocol.Home(resources, scene, characters, walkableArea, ui));
            
            // 推送世界地图数据
            var worldMap = Display.Agent.CreateWorldMap(client.Player);
            Net.Tcp.Instance.Send(client.Player, worldMap);
            
            // 推送屏幕自适应值
            int screenAdaptation = client.Player.ScreenUIAdaptation > 0 ? client.Player.ScreenUIAdaptation : 100;
            Net.Tcp.Instance.Send(client.Player, new Net.Protocol.ScreenAdaptation(screenAdaptation));
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


        private static void UpdatePlayerAge(Logic.Player player, Logic.Database.Player database)
        {
            var lastSignOutTime = database.GetTime("SignOut");
            var currentTime = DateTime.Now;
            var offlineTime = currentTime - lastSignOutTime;
            var gameTimeElapsed = offlineTime.TotalSeconds * Domain.Time.Agent.Rate;
            var gameDaysElapsed = gameTimeElapsed / 86400.0; // 86400秒 = 1天
            database.record["Age"] = (int)(database.GetRecord("Age")+ gameDaysElapsed);

        }

        private static void RestoreCompanions(Logic.Player player)
        {
            if (player == null || player.Map == null) return;

            foreach (var comp in player.Database.companions.ToList())
            {
                if (comp.ExpireTime.HasValue && comp.ExpireTime.Value < DateTime.Now)
                {
                    player.Database.companions.Remove(comp);
                    continue;
                }

                var lifeConfig = Logic.Config.Agent.Instance.Content.Get<Logic.Config.Life>(l => l.Id == comp.LifeConfigId);
                if (lifeConfig != null)
                {
                    var pet = player.Map.Create<Logic.Life>(lifeConfig, comp.Level);
                    pet.Leader = player;
                    Domain.Relation.Do(player, pet, Relation.Reason.Help);
                    Domain.Relation.Do(pet, player, Relation.Reason.Help);
                }
            }
        }
    }
}

