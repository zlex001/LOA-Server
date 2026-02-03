using Domain.BehaviorTree;
using Domain.Text;
using Logic;
using Logic.Database;
using Net;
using Net.Protocol;
using NPOI.SS.Formula.Functions;
using System;
using System.Linq;
using Utils;

namespace Domain.Authentication
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

            Can(client, name, out InitialResponse.Code code, out Logic.Database.Player database);
            if (code == InitialResponse.Code.Success)
            {
                CompleteRegistration(client, name, database);
            }
            string errorMessage = GetErrorMessage(code, client.Language);
            client.Send(new InitialResponse(code, errorMessage));
        }

        private static string GetErrorMessage(InitialResponse.Code code, Logic.Text.Languages language)
        {
            if (code == InitialResponse.Code.Success)
                return "";

            Logic.Text.Labels label = code switch
            {
                InitialResponse.Code.Empty => Logic.Text.Labels.InitializeErrorNameEmpty,
                InitialResponse.Code.TooLong => Logic.Text.Labels.InitializeErrorNameTooLong,
                InitialResponse.Code.Unsafe => Logic.Text.Labels.InitializeErrorNameUnsafe,
                InitialResponse.Code.AlreadyExsit => Logic.Text.Labels.InitializeErrorNameAlreadyExist,
                _ => Logic.Text.Labels.InitializeErrorNameEmpty
            };
            return Domain.Text.Agent.Instance.Get(label, language);
        }

        private static void CompleteRegistration(Client client, string name, Logic.Database.Player database)
        {
            database.text["Name"] = name;
            database.time["Register"] = database.time["SignOut"] = DateTime.Now.ToString();
            Logic.Database.Agent.Instance.AddAsParent(database);
            
            var map = Logic.Agent.Instance.Content.Get<Logic.Map>(m => Enumerable.SequenceEqual(m.Database.pos, database.pos));
            if (map == null)
            {
                Utils.Debug.Log.Error("AUTH", $"[Register] Cannot find map at position {string.Join(",", database.pos)} for player {name}");
                client.Send(new InitialResponse(InitialResponse.Code.AlreadyExsit, Domain.Text.Agent.Instance.Get(Logic.Text.Labels.InitializeErrorNameEmpty, client.Language)));
                return;
            }
            
            client.Player = map.Create<Logic.Player>(database);
            client.Player.data.Full<int>(Life.Data.Mp);
            client.Player.data.Full<double>(Life.Data.Lp);
            foreach (Logic.Part part in client.Player.Content.Gets<Logic.Part>())
            {
                part.data.Full<int>(Logic.Part.Data.Hp);
            }
            
            SyncPlayerDataToDatabase(client.Player);
            
            Logic.Database.Agent.Instance.Save(Logic.Config.MySQL.ConnectionString, database);
            
            string device = client.data.Get<string>(Client.Data.Device);
            string id = client.data.Get<string>(Client.Data.AccountId);
            Login.CompleteLogin(client, device, id);
            // Tutorial is now started via CompleteLogin -> Tutorial.Instance.Start()
        }
        
        private static void SyncPlayerDataToDatabase(Logic.Player player)
        {
            player.Database.pos = player.Map.Database.pos;
            player.Database.parts.Clear();
            foreach (var part in player.Content.Gets<Logic.Part>())
            {
                player.Database.parts.Add(new Logic.Database.Part
                {
                    Type = part.Type,
                    Hp = part.Hp,
                    MaxHp = part.MaxHp,
                });
            }
            player.Database.record["Mp"] = player.Mp;
            player.Database.record["Lp"] = (int)player.Lp;
        }

        private static bool Can(Client client, string name, out InitialResponse.Code errorCode, out Logic.Database.Player database)
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

            if (!client.Content.Has<Logic.Database.Player>())
            {
                errorCode = InitialResponse.Code.AlreadyExsit;
                return false;
            }

            database = client.Content.Get<Logic.Database.Player>();
            errorCode = InitialResponse.Code.Success;
            return true;
        }

        private static bool IsNameLengthValid(string name)
        {
            return name.Length <= 10;
        }

        private static bool IsNameAlreadyExists(string name)
        {
            return Logic.Database.Agent.Instance.Content
                .Has<Logic.Database.Player>(p => p.text["Name"] == name);
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
                
                var deviceRecord = Logic.Database.Agent.Instance.Content.Get<Logic.Database.Device>(d => d.Id == device);
                if (deviceRecord != null)
                {
                    client.Language = deviceRecord.PreferredLanguage;
                }
                
                client.Send(new LoginResponse(LoginResponse.Code.Success, ""));
                Utils.Debug.Log.Info("AUTH", $"[Register.Satart] LoginResponse.Success sent, creating player database");
                Logic.Database.Player database = new Logic.Database.Player(id, pw);
                
                // Server isolation: set server ID for new player
                if (!string.IsNullOrEmpty(Logic.Agent.Instance.ServerId))
                {
                    database.text["ServerId"] = Logic.Agent.Instance.ServerId;
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
                string errorMessage = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.LoginUnsafeAccount, client.Language);
                client.Send(new LoginResponse(LoginResponse.Code.UnsafeAccount, errorMessage));
            }
            Utils.Debug.Log.Info("AUTH", $"[Register.Satart] End - id={id}");
        }
        public static void Reset(Client client)
        {
            if (client.Content.Has<Logic.Database.Player>())
            {
                double age =(Logic.Constant.InitialPlayerAge * Domain.Time.Agent.Rate);
                Logic.Database.Player database = client.Content.Get<Logic.Database.Player>();
                database.record["Age"] = (int)age;
                database.text["Gender"] = Utils.Random.Range(0, 2) == 0 ? Life.Genders.Female.ToString() : Life.Genders.Male.ToString();
                database.text["Category"] = Utils.Random.Range(0, 2) == 0 ? Logic.Life.Categories.Atlantean.ToString() : Logic.Life.Categories.Lemurian.ToString();
                int[] grades = Utils.Random.WithSum<int>(100, 5, 1, 25);
                for (int i = (int)Life.Attributes.Hp; i <= (int)Life.Attributes.Mp; i++)
                {
                    int index = i - (int)Life.Attributes.Hp;
                    database.grade[(Life.Attributes)i] = grades[index];
                }

                var initialMap = Logic.SpawnPoint.GetRandomInitialMap();
                if (initialMap != null)
                {
                    database.pos = initialMap.Database.pos;
                }

                // TODO: 临时注释，等待世界观迁移完成后恢复
                // var template = Domain.Text.Agent.Instance.Get(Logic.Constant.PlayerInitializeDescriptionTemplate, client);
                // var prefix = Domain.Text.Agent.Instance.Get(Logic.Constant.DescriptionPrefixHuman, client);
                
                var gender = Enum.Parse<Logic.Life.Genders>(database.GetText("Gender"));
                var genderText = Domain.Text.Agent.Instance.Get((int)Domain.Text.Description.Player(gender, (int)(age / Domain.Time.Agent.Rate)), client.Language);

                var category = Enum.Parse<Logic.Life.Categories>(database.GetText("Category"));
                var categoryText = category == Logic.Life.Categories.Lemurian 
                    ? Domain.Text.Agent.Instance.Get(Logic.Text.Labels.Lemurian, client.Language)
                    : Domain.Text.Agent.Instance.Get(Logic.Text.Labels.Atlantean, client.Language);

                var sceneName = initialMap != null ? Domain.Text.Agent.Instance.Get(initialMap.Scene.Config.Name, client) : "";

                var template = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.InitializeDescription, client.Language);
                var description = $"{Utils.Text.Indent(0)}{Utils.Text.Format(template, "Scene", sceneName, "Category", categoryText, "Gender", genderText)}";

                Dictionary<string, int> grade = database.grade.ToDictionary(entry => Text.Agent.Instance.Get(entry.Key, client), entry => entry.Value);

                var ui = new Initialize.UI
                {
                    namePlaceholder = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.InitializeNamePlaceholder, client.Language),
                    randomButton = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.InitializeRandomButton, client.Language),
                    confirmButton = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.InitializeConfirmButton, client.Language),
                    errorNameEmpty = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.InitializeErrorNameEmpty, client.Language),
                    errorNameUnsafe = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.InitializeErrorNameUnsafe, client.Language)
                };

                client.Send(new Initialize(description, grade, ui));
            }
        }



    }
}
