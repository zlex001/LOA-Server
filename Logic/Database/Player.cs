using Newtonsoft.Json;
using System.Linq;

namespace Logic.Database
{


    public class Player : Basic.MySql.Data
    {
        public int[] pos = new int[0];
        public List<Item> equipments = new();
        public List<Skill> skills = new();
        public List<Payment> payments = new();
        public List<Warehouse> warehouses = new();
        public List<Part> parts = new();
        public Dictionary<string, int> quest = new();
        public Dictionary<string, string> text = new();
        public Dictionary<string, int> record = new();
        public Dictionary<string, string> time = new();
        public Dictionary<Logic.Life.Attributes, int> grade = new();
        public Queue<string> activitys = new();
        public List<int> signs = new();
        public List<MerchandiseItem> merchandises = new();
        public List<Companion> companions = new();

        public override Dictionary<string, object> ToDictionary => new()
        {
            ["Id"] = Id,
            ["Pos"] = JsonConvert.SerializeObject(pos),
            ["Record"] = JsonConvert.SerializeObject(record),
            ["Text"] = JsonConvert.SerializeObject(text),
            ["Grade"] = JsonConvert.SerializeObject(grade.ToDictionary(k => k.Key.ToString(), v => v.Value)),
            ["Equipments"] = JsonConvert.SerializeObject(equipments.Select(e => e.ToDictionary).ToList()),
            ["Quest"] = JsonConvert.SerializeObject(quest),
            ["Skills"] = JsonConvert.SerializeObject(skills.Select(s => s.ToDictionary).ToList()),
            ["Payments"] = JsonConvert.SerializeObject(payments.Select(s => s.ToDictionary).ToList()),
            ["Warehouses"] = JsonConvert.SerializeObject(warehouses.Select(s => s.ToDictionary).ToList()),
            ["Parts"] = JsonConvert.SerializeObject(parts.Select(p => p.ToDictionary).ToList()),
            ["Time"] = JsonConvert.SerializeObject(time),
            ["Activitys"] = JsonConvert.SerializeObject(activitys),
            ["Signs"] = JsonConvert.SerializeObject(signs),
            ["Merchandises"] = JsonConvert.SerializeObject(merchandises.Select(m => m.ToDictionary).ToList()),
            ["Companions"] = JsonConvert.SerializeObject(companions.Select(c => c.ToDictionary).ToList()),
        };

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;

            Id = Get<string>(dict, "Id");
            pos = Utils.Json.Deserialize<int[]>(Get<string>(dict, "Pos")) ?? new int[0];
            record = Utils.Json.Deserialize<string, int>(Get<string>(dict, "Record"));
            text = Utils.Json.Deserialize<string, string>(Get<string>(dict, "Text"));
            time = Utils.Json.Deserialize<string, string>(Get<string>(dict, "Time"));
            grade = Utils.Json.Deserialize<Logic.Life.Attributes, int>(Get<string>(dict, "Grade"));
            quest = Utils.Json.Deserialize<string, int>(Get<string>(dict, "Quest"));
            
            equipments = DeserializeItems(Get<string>(dict, "Equipments"));
            
            var skillsJson = Get<string>(dict, "Skills");

            // 打印字典中所有包含"skill"的键
            var skillKeys = dict.Keys.Where(k => k.ToLower().Contains("skill")).ToArray();

            skills = DeserializeSkills(skillsJson);
            payments = DeserializePayments(Get<string>(dict, "Payments"));
            warehouses = DeserializeWarehouses(Get<string>(dict, "Warehouses"));
            parts = DeserializeParts(Get<string>(dict, "Parts"));
            activitys = Utils.Json.Deserialize<Queue<string>>(Get<string>(dict, "Activitys")) ?? new();
            signs = Utils.Json.Deserialize<List<int>>(Get<string>(dict, "Signs")) ?? new();
            merchandises = DeserializeMerchandises(Get<string>(dict, "Merchandises"));
            companions = DeserializeCompanions(Get<string>(dict, "Companions"));
        }

        public Player() { }

        public Player(string id, string pw)
        {
            Id = id;
            text["Pw"] = pw;
            record["Level"] = 1;
        }

        public string GetText(string key) => text.TryGetValue(key, out var value) ? value : default;
        public int GetRecord(string key) => record.TryGetValue(key, out var value) ? value : default;
        public DateTime GetTime(string key)
        {
            return time.TryGetValue(key, out var str) && DateTime.TryParse(str, out var dt) ? dt : DateTime.Now;
        }
        private List<Item> DeserializeItems(string json)
        {
            var items = new List<Item>();

            if (!string.IsNullOrEmpty(json))
            {
                var itemDicts = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                if (itemDicts != null)
                {
                    foreach (var dict in itemDicts)
                    {
                        var item = new Item();
                        item.Init(dict);
                        items.Add(item);
                    }
                }
            }

            return items;
        }


        private List<Skill> DeserializeSkills(string json)
        {
            var skills = new List<Skill>();

            if (!string.IsNullOrEmpty(json))
            {
                var skillDicts = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                if (skillDicts != null)
                {
                    foreach (var dict in skillDicts)
                    {
                        var skill = new Skill();
                        skill.Init(dict);
                        skills.Add(skill);
                    }
                }
            }

            return skills;
        }


        /// <summary>
        /// 正确反序列化Logic.Database.Payment列表
        /// </summary>
        private List<Payment> DeserializePayments(string json)
        {
            if (string.IsNullOrEmpty(json)) return new();

            try
            {
                var paymentDicts = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                if (paymentDicts == null) return new();

                var payments = new List<Payment>();
                foreach (var paymentDict in paymentDicts)
                {
                    var payment = new Payment();
                    payment.Init(paymentDict);
                    payments.Add(payment);
                }
                return payments;
            }
            catch (Exception)
            {
                return new();
            }
        }

        private List<Warehouse> DeserializeWarehouses(string json)
        {
            var warehouses = new List<Warehouse>();

            if (!string.IsNullOrEmpty(json))
            {
                var dicts = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                if (dicts != null)
                {
                    foreach (var dict in dicts)
                    {
                        var warehouse = new Warehouse();
                        warehouse.Init(dict);
                        warehouses.Add(warehouse);
                    }
                }
            }

            return warehouses;
        }

        private List<MerchandiseItem> DeserializeMerchandises(string json)
        {
            var merchandises = new List<MerchandiseItem>();

            if (!string.IsNullOrEmpty(json))
            {
                var dicts = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                if (dicts != null)
                {
                    foreach (var dict in dicts)
                    {
                        var merchandise = new MerchandiseItem();
                        merchandise.Init(dict);
                        merchandises.Add(merchandise);
                    }
                }
            }

            return merchandises;
        }


        public bool New(DateTime dateTime) => GetTime("Register").Date == dateTime.Date;
        public bool NewPaying(DateTime dateTime) => New(dateTime) && GetRecord("CumulativeGem") > 0;
        public bool Active(DateTime dateTime) => !New(dateTime) && activitys.Any(a => DateTime.Parse(a).Date == dateTime.Date);

        private List<Part> DeserializeParts(string json)
        {
            var parts = new List<Part>();

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var partList = Utils.Json.Deserialize<List<Dictionary<string, object>>>(json) ?? new();
                    foreach (var partDict in partList)
                    {
                        var part = new Part
                        {
                            Type = (Logic.Part.Types)Convert.ToInt32(partDict["Type"]),
                            Hp = Convert.ToInt32(partDict["Hp"]),
                            MaxHp = Convert.ToInt32(partDict["MaxHp"]),
                        };
                        parts.Add(part);
                    }
                }
                catch (Exception ex)
                {
                    Utils.Debug.Log.Error("DATABASE", $"Error deserializing Parts: {ex.Message}");
                }
            }

            return parts;
        }

        private List<Companion> DeserializeCompanions(string json)
        {
            var companions = new List<Companion>();

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var companionDicts = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                    if (companionDicts != null)
                    {
                        foreach (var dict in companionDicts)
                        {
                            var companion = new Companion
                            {
                                LifeConfigId = Convert.ToInt32(dict["LifeConfigId"]),
                                Level = Convert.ToInt32(dict["Level"]),
                                Source = dict["Source"]?.ToString() ?? "",
                                ExpireTime = string.IsNullOrEmpty(dict["ExpireTime"]?.ToString()) ? null : DateTime.Parse(dict["ExpireTime"].ToString())
                            };
                            companions.Add(companion);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.Debug.Log.Error("DATABASE", $"Error deserializing Companions: {ex.Message}");
                }
            }

            return companions;
        }
    }
}
