using Newtonsoft.Json;

namespace Logic.Design
{
    public class Life : Ability
    {
        public string name;
        public string age;
        public string parts;  // 部位配置（中文）：头,胸,手,背,腰,腿,脚,爪,尾,翼
        public string[] equipments;
        public string[] skills;
        public string function;
        public string chase;
        public int hp;
        public int atk;
        public int def;
        public int agi;
        public int mp;
        public string category;
        public string tags;
        public string gender;
        public string career;
        public int stage;
        public string literary;
        public string item;
        public List<string> plotors;


        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            id = Get<int>(dict, "id");
            cid = Get<string>(dict, "cid");
            name = Get<string>(dict, "name");
            age = Get<string>(dict, "age");
            parts = Get<string>(dict, "parts");  // 读取parts字段（中文）
            equipments = Get<string>(dict, "equipments")?.Split(',') ?? Array.Empty<string>();
            function = Get<string>(dict, "function");
            category = Get<string>(dict, "category");
            tags = Get<string>(dict, "tags");
            gender = Get<string>(dict, "gender");
            career = Get<string>(dict, "career");
            chase = Get<string>(dict, "chase");
            stage = Get<int>(dict, "stage");
            literary = Get<string>(dict, "literary");
            hp = Get<int>(dict, "hp");
            atk = Get<int>(dict, "atk");
            def = Get<int>(dict, "def");
            agi = Get<int>(dict, "agi");
            mp = Get<int>(dict, "mp");
            skills = Get<string>(dict, "skills")?.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray() ?? Array.Empty<string>();
            item = Get<string>(dict, "item");
            plotors = Get<string>(dict, "plotors")?.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
        }

        private static string ConvertPartsToEnglish(string partsInChinese)
        {
            if (string.IsNullOrEmpty(partsInChinese))
                return "[]";

            // 中英文映射表
            var partMapping = new Dictionary<string, string>
            {
                ["头"] = "Head",
                ["胸"] = "Chest",
                ["手"] = "Hand",
                ["背"] = "Back",
                ["腰"] = "Waist",
                ["腿"] = "Leg",
                ["脚"] = "Foot",
                ["爪"] = "Claw",
                ["尾"] = "Tail",
                ["翼"] = "Wing"
            };

            var chineseParts = partsInChinese.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
            var englishParts = new List<string>();

            foreach (var part in chineseParts)
            {
                if (partMapping.ContainsKey(part))
                {
                    englishParts.Add(partMapping[part]);
                }
                else
                {
                    // 如果找不到映射，报错并保留原值
                    Utils.Debug.Log.Error("DESIGN", $"Unknown part name: {part}");
                    englishParts.Add(part);
                }
            }

            return JsonConvert.SerializeObject(englishParts);
        }

        private static string ConvertMaterialToEnglish(string chineseMaterial)
        {
            return chineseMaterial switch
            {
                "稻米" => "Rice",
                "小麦" => "Wheat",
                "谷物" => "Grain",
                "肉" => "Meat",
                "瓜果" => "Fruit",
                "蔬菜" => "Vegetable",
                "治疗" => "Healing",
                _ => chineseMaterial
            };
        }

        private static string ConvertTagsBehaviorTreeCidToId(string originalTags)
        {
            if (string.IsNullOrEmpty(originalTags))
                return "[]";

            var tags = Utils.Tag.ParseTagsFromString(originalTags);
            var convertedTags = new List<string>();

            foreach (var tag in tags)
            {
                if (tag.StartsWith("行为树:"))
                {
                    var behaviorTreeCid = tag.Substring(4).Trim();
                    var behaviorTreeConfig = Agent.Instance.Content.Get<BehaviorTree>(x => x.cid == behaviorTreeCid);
                    if (behaviorTreeConfig == null)
                    {
                        Utils.Debug.Log.Error("DESIGN", $"BehaviorTree not found: {behaviorTreeCid}");
                        continue;
                    }
                    convertedTags.Add($"BehaviorTree:{behaviorTreeConfig.id}");
                }
                else if (tag.StartsWith("断肢:"))
                {
                    var parts = tag.Split(':');
                    if (parts.Length >= 3)
                        {
                        string materialCid = parts[1].Trim();
                        string dropConfig = parts[2].Trim();
                            
                            var itemConfig = Agent.Instance.Content.Get<Item>(c => c.cid == materialCid);
                            if (itemConfig != null)
                            {
                            convertedTags.Add($"Dismember:{itemConfig.id}:{dropConfig}");
                            }
                            else
                            {
                                Utils.Debug.Log.Error("DESIGN", $"Item not found for Dismember tag: {materialCid}");
                            convertedTags.Add(tag);
                            }
                        }
                    }
                else if (tag.StartsWith("喜好:"))
                {
                    var chineseMaterial = tag.Substring(3).Trim();
                    var englishMaterial = ConvertMaterialToEnglish(chineseMaterial);
                    convertedTags.Add($"Favorite:{englishMaterial}");
                }
                else if (tag.StartsWith("传授"))
                {
                    convertedTags.Add($"Teach:{tag.Substring(2)}");
                }
                else if (tag == "肉食性")
                {
                    convertedTags.Add("Carnivorous");
                }
                else
                {
                    convertedTags.Add(tag);
                }
            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(convertedTags);
        }

        public static void Convert()
        {
            List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
            foreach (Life config in Agent.Instance.Content.Gets<Life>())
            {
                Dictionary<string, List<string>> text = new Dictionary<string, List<string>>();
                if (config.name != default)
                {
                    text["Name"] = config.name.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                }
        
                if (config.chase != default)
                {
                    text["Chase"] = config.chase.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                }
                Dictionary<string, int> attribute = new Dictionary<string, int> { ["Hp"] = config.hp, ["Atk"] = config.atk, ["Def"] = config.def, ["Agi"] = config.agi, ["Mp"] = config.mp, };
                Dictionary<string, int> item = config.item?.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).Where(i => i.Contains(':')).ToDictionary(i => i.Split(':')[0], i => System.Convert.ToInt32(i.Split(':')[1])) ?? new Dictionary<string, int>();

                Dictionary<string, List<string>> function = new Dictionary<string, List<string>>();
                if (config.function != null)
                {
                    foreach (string i in config.function.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)))
                    {
                        if (i.Contains(':'))
                        {
                            function[i.Split(':')[0]] = i.Split(':')[1].Split(',').ToList();
                        }
                    }
                }
                int[] skills = config.skills.Select(e => Agent.Instance.Content.Get<Skill>(c => c.cid == e).id).ToArray();
                int[] equipments = config.equipments.Select(e => Agent.Instance.Content.Get<Item>(c => c.cid == e).id).ToArray();

                List<int> plotors = config.plotors.Select(p => Agent.Instance.Content.Get<Plot>(x => x.cid == p).id).ToList();

                // 转换tags中的BehaviorTree cid为id
                string convertedTags = ConvertTagsBehaviorTreeCidToId(config.tags);
                
                // 转换parts中文为英文
                string convertedParts = ConvertPartsToEnglish(config.parts);

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    {"id", config.id },
                    {"name", Agent.Instance.Content.Get<Multilingual>(m=>m.cid==config.name).id },
                    {"category", config.category },
                    {"parts", convertedParts },  // 添加parts字段
                    {"tags", convertedTags },
                    {"gender", config.gender },
                    {"career", config.career },
                    {"stage", config.stage },
                    {"age", config.age },
                    {"function", JsonConvert.SerializeObject(function)},
                    {"skills", JsonConvert.SerializeObject(skills)},
                    {"text",JsonConvert.SerializeObject(text) },
                    {"equipments", JsonConvert.SerializeObject(equipments)},
                    {"attribute",JsonConvert.SerializeObject(attribute) },
                    {"item",JsonConvert.SerializeObject(item) },
                    {"plotors", JsonConvert.SerializeObject(plotors)},
                };
                datas.Add(data);
            }

            string path = $"{Utils.Paths.Library}/Config/Life.csv";
            Utils.FileManager.Instance.DeleteFile(path);
            Utils.Csv.SaveByRows(datas, path);
        }
    }
}

