using Logic.BehaviorTree;
using Logic.Text;
using Data;
using Data.Database;
using Net;
using Net.Protocol;
using NPOI.SS.Formula.Functions;
using System;
using System.Linq;
using Utils;

namespace Logic.Authentication
{
    public static class Register
    {
        public static void Init()
        {
            Net.Manager.Instance.monitor.Register(Client.Event.InitializeConfirm, OnConfirm);

        }
        private static void OnConfirm(params object[] args)
        {
            Client client = (Client)args[0];
            string name = (string)args[1];

            Can(client, name, out InitialResponse.Code code, out global::Data.Database.Player database);
            if (code == InitialResponse.Code.Success)
            {
                CompleteRegistration(client, name, database);
            }
            string errorMessage = GetErrorMessage(code, client.Language);
            client.Send(new InitialResponse(code, errorMessage));
        }

        private static string GetErrorMessage(InitialResponse.Code code, global::Data.Text.Languages language)
        {
            if (code == InitialResponse.Code.Success)
                return "";

            global::Data.Text.Labels label = code switch
            {
                InitialResponse.Code.Empty => global::Data.Text.Labels.InitializeErrorNameEmpty,
                InitialResponse.Code.TooLong => global::Data.Text.Labels.InitializeErrorNameTooLong,
                InitialResponse.Code.Unsafe => global::Data.Text.Labels.InitializeErrorNameUnsafe,
                InitialResponse.Code.AlreadyExsit => global::Data.Text.Labels.InitializeErrorNameAlreadyExist,
                _ => global::Data.Text.Labels.InitializeErrorNameEmpty
            };
            return Logic.Text.Agent.Instance.Get(label, language);
        }

        private static void CompleteRegistration(Client client, string name, global::Data.Database.Player database)
        {
            database.text["Name"] = name;
            database.time["Register"] = database.time["SignOut"] = DateTime.Now.ToString();
            global::Data.Database.Agent.Instance.AddAsParent(database);
            
            var map = global::Data.Agent.Instance.Content.Get<global::Data.Map>(m => Enumerable.SequenceEqual(m.Database.pos, database.pos));
            if (map == null)
            {
                Utils.Debug.Log.Error("AUTH", $"[Register] Cannot find map at position {string.Join(",", database.pos)} for player {name}");
                client.Send(new InitialResponse(InitialResponse.Code.AlreadyExsit, Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.InitializeErrorNameEmpty, client.Language)));
                return;
            }
            
            client.Player = map.Create<global::Data.Player>(database);
            client.Player.data.Full<int>(Life.Data.Mp);
            client.Player.data.Full<double>(Life.Data.Lp);
            foreach (global::Data.Part part in client.Player.Content.Gets<global::Data.Part>())
            {
                part.data.Full<int>(global::Data.Part.Data.Hp);
            }
            
            SyncPlayerDataToDatabase(client.Player);
            
            global::Data.Database.Agent.Instance.Save(global::Data.Config.MySQL.ConnectionString, database);
            
            string device = client.data.Get<string>(Client.Data.Device);
            string id = client.data.Get<string>(Client.Data.AccountId);
            Login.CompleteLogin(client, device, id);
            // Tutorial is now started via CompleteLogin -> Tutorial.Instance.Start()
        }
        
        public static void SyncPlayerDataToDatabase(global::Data.Player player)
        {
            player.Database.pos = player.Map.Database.pos;
            player.Database.parts.Clear();
            foreach (var part in player.Content.Gets<global::Data.Part>())
            {
                player.Database.parts.Add(new global::Data.Database.Part
                {
                    Type = part.Type,
                    Hp = part.Hp,
                    MaxHp = part.MaxHp,
                });
            }
            player.Database.record["Mp"] = player.Mp;
            player.Database.record["Lp"] = (int)player.Lp;
        }

        private static bool Can(Client client, string name, out InitialResponse.Code errorCode, out global::Data.Database.Player database)
        {
            database = null;

            if (string.IsNullOrEmpty(name))
            {
                errorCode = InitialResponse.Code.Empty;
                return false;
            }

            if (!IsNameLengthValid(name))
            {
                errorCode = InitialResponse.Code.TooLong;
                return false;
            }

            if (!Utils.Text.SafeName(name))
            {
                errorCode = InitialResponse.Code.Unsafe;
                return false;
            }

            if (IsNameAlreadyExists(name))
            {
                errorCode = InitialResponse.Code.AlreadyExsit;
                return false;
            }

            if (!client.Content.Has<global::Data.Database.Player>())
            {
                errorCode = InitialResponse.Code.AlreadyExsit;
                return false;
            }

            database = client.Content.Get<global::Data.Database.Player>();
            errorCode = InitialResponse.Code.Success;
            return true;
        }

        private static bool IsNameLengthValid(string name)
        {
            return name.Length <= 10;
        }

        private static bool IsNameAlreadyExists(string name)
        {
            return global::Data.Database.Agent.Instance.Content
                .Has<global::Data.Database.Player>(p => p.text["Name"] == name);
        }


        private static bool Can(string id, string pw)
        {
            return Utils.Text.SafeAccount(id, 6, 20) && Utils.Text.SafeAccount(pw, 6, 20);
        }
        public static void Satart(Client client, string device, string id, string pw)
        {
            Utils.Debug.Log.Info("AUTH", $"[Register.Satart] Start - id={id}, device={device}");
            if (Can(id, pw))
            {
                Utils.Debug.Log.Info("AUTH", $"[Register.Satart] Can() passed, sending LoginResponse.Success");
                client.data.Change(Client.Data.Device, device);
                client.data.Change(Client.Data.AccountId, id);
                
                var deviceRecord = global::Data.Database.Agent.Instance.Content.Get<global::Data.Database.Device>(d => d.Id == device);
                if (deviceRecord != null)
                {
                    client.Language = deviceRecord.PreferredLanguage;
                }
                
                client.Send(new LoginResponse(LoginResponse.Code.Success, id, pw, isGuest: false, isNewAccount: true));
                Utils.Debug.Log.Info("AUTH", $"[Register.Satart] LoginResponse.Success sent, creating player database");
                global::Data.Database.Player database = new global::Data.Database.Player(id, pw);
                
                // Server isolation: set server ID for new player
                if (!string.IsNullOrEmpty(global::Data.Agent.Instance.ServerId))
                {
                    database.text["ServerId"] = global::Data.Agent.Instance.ServerId;
                    Utils.Debug.Log.Info("AUTH", $"[Register] New player {id} registered to server {database.text["ServerId"]}");
                }
                
                client.Add(database);
                Utils.Debug.Log.Info("AUTH", $"[Register.Satart] Calling Reset()");
                Reset(client);
                Utils.Debug.Log.Info("AUTH", $"[Register.Satart] Reset() completed");
            }
            else
            {
                Utils.Debug.Log.Info("AUTH", $"[Register.Satart] Can() failed - unsafe account id={id}");
                client.Send(new LoginResponse(LoginResponse.Code.UnsafeAccount));
            }
            Utils.Debug.Log.Info("AUTH", $"[Register.Satart] End - id={id}");
        }

        public static void GenerateRandomCharacter(global::Data.Database.Player database)
        {
            double age = (global::Data.Constant.InitialPlayerAge * Logic.Time.Agent.Rate);
            database.record["Age"] = (int)age;
            database.text["Gender"] = Utils.Random.Range(0, 2) == 0 ? Life.Genders.Female.ToString() : Life.Genders.Male.ToString();
            database.text["Category"] = Utils.Random.Range(0, 2) == 0 ? global::Data.Life.Categories.Atlantean.ToString() : global::Data.Life.Categories.Lemurian.ToString();
            int[] grades = Utils.Random.WithSum<int>(100, 5, 1, 25);
            for (int i = (int)Life.Attributes.Hp; i <= (int)Life.Attributes.Mp; i++)
            {
                int index = i - (int)Life.Attributes.Hp;
                database.grade[(Life.Attributes)i] = grades[index];
            }

            var initialMap = global::Data.SpawnPoint.GetRandomInitialMap();
            if (initialMap != null)
            {
                database.pos = initialMap.Database.pos;
            }
        }

        public static void Reset(Client client)
        {
            if (client.Content.Has<global::Data.Database.Player>())
            {
                global::Data.Database.Player database = client.Content.Get<global::Data.Database.Player>();
                GenerateRandomCharacter(database);
                
                double age = (global::Data.Constant.InitialPlayerAge * Logic.Time.Agent.Rate);
                var initialMap = global::Data.SpawnPoint.GetRandomInitialMap();

                // TODO: 临时注释，等待世界观迁移完成后恢复
                // var template = Logic.Text.Agent.Instance.Get(global::Data.Constant.PlayerInitializeDescriptionTemplate, client);
                // var prefix = Logic.Text.Agent.Instance.Get(global::Data.Constant.DescriptionPrefixHuman, client);
                
                var gender = Enum.Parse<global::Data.Life.Genders>(database.GetText("Gender"));
                var genderText = Logic.Text.Agent.Instance.Get((int)Logic.Text.Description.Player(gender, (int)(age / Logic.Time.Agent.Rate)), client.Language);

                var category = Enum.Parse<global::Data.Life.Categories>(database.GetText("Category"));
                var categoryText = category == global::Data.Life.Categories.Lemurian 
                    ? Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.Lemurian, client.Language)
                    : Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.Atlantean, client.Language);

                var sceneName = initialMap != null ? Logic.Text.Agent.Instance.Get(initialMap.Scene.Config.Name, client) : "";

                var template = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.InitializeDescription, client.Language);
                var description = $"{Utils.Text.Indent(0)}{Utils.Text.Format(template, "Scene", sceneName, "Category", categoryText, "Gender", genderText)}";

                Dictionary<string, int> grade = database.grade.ToDictionary(entry => Text.Agent.Instance.Get(entry.Key, client), entry => entry.Value);

                var ui = new Initialize.UI
                {
                    namePlaceholder = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.InitializeNamePlaceholder, client.Language),
                    randomButton = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.InitializeRandomButton, client.Language),
                    confirmButton = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.InitializeConfirmButton, client.Language),
                    errorNameEmpty = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.InitializeErrorNameEmpty, client.Language),
                    errorNameUnsafe = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.InitializeErrorNameUnsafe, client.Language)
                };

                client.Send(new Initialize(description, grade, ui));
            }
        }



    }
}
