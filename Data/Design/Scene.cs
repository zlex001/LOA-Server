using Newtonsoft.Json;
using System.Collections.Generic;

namespace Data.Design
{
    public class Scene : Ability
    {
        public string name;
        public string type;
        public string[] characters;

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            id = Get<int>(dict, "id");
            cid = Get<string>(dict, "cid");
            name = Get<string>(dict, "name");
            type = Get<string>(dict, "type") ?? "";
            characters = Get<string>(dict, "characters")?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToArray() ?? Array.Empty<string>();
            
            if (string.IsNullOrEmpty(type))
            {
                type = InferSceneType(cid);
            }
        }
        
        private static string InferSceneType(string sceneCid)
        {
            var typeMap = new Dictionary<string, string>
            {
                {"埃利都", "城市"},
                {"埃纳达平原", "平原"},
                {"沙玛尔", "山脉"},
                {"杜尔甘", "城市"},
                {"阿什卡尔山脉", "山脉"},
                {"夏尔库拉", "城市"},
                {"卡玛尔高原", "高原"},
                {"苏萨", "丘陵"},
                {"波塞迪亚", "岛屿"},
                {"阿撒拉", "岛屿"},
                {"伊什图安", "岛屿"},
                {"阿鲁沙", "城市"},
                {"阿撒鲁恩", "沙漠"},
                {"提兰", "盆地"},
                {"尼普尔", "湿地"}
            };
            
            return typeMap.TryGetValue(sceneCid, out string sceneType) ? sceneType : "";
        }
        private static string ConvertTypeToEnglish(string chineseType)
        {
            return chineseType switch
            {
                "城市" => "City",
                "平原" => "Plain",
                "丘陵" => "Hill",
                "盆地" => "Basin",
                "山脉" => "Mountain",
                "沙漠" => "Desert",
                "火山" => "Volcano",
                "高原" => "Plateau",
                "湿地" => "Wetland",
                "冰川" => "Glacier",
                "岛屿" => "Island",
                "海岸" => "Coast",
                "峡谷" => "Canyon",
                _ => ""
            };
        }
        
        public static void Convert()
        {
            List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
            foreach (Scene config in Agent.Instance.Content.Gets<Scene>())
            {
                List<(string cid, int? minLevel, int? maxLevel, int count, int? minCount, int? maxCount, double probability, int id)> characterTuples = new();

                foreach (string line in config.characters)
                {
                    string text = line.Trim();
                    if (string.IsNullOrEmpty(text)) continue;

                    string cid = text;
                    int? minLevel = null, maxLevel = null;
                    int? minCount = null, maxCount = null;
                    int count = 1;
                    double probability = 1.0;

                    if (cid.Contains(":"))
                    {
                        var colonParts = cid.Split(new[] { ':' }, 2);
                        cid = colonParts[0].Trim();
                        string configPart = colonParts[1].Trim();

                        if (configPart.Contains("%") && configPart.Contains("×"))
                        {
                            var percentIndex = configPart.IndexOf('%');
                            var multiplyIndex = configPart.IndexOf('×');
                            
                            string probStr = configPart.Substring(0, percentIndex);
                            if (double.TryParse(probStr, out double prob))
                            {
                                probability = prob / 100.0;
                            }

                            string countPart = configPart.Substring(multiplyIndex + 1);
                            
                            if (countPart.Contains(":"))
                            {
                                var countLevelParts = countPart.Split(new[] { ':' }, 2);
                                string countRange = countLevelParts[0].Trim();
                                string levelPart = countLevelParts[1].Trim();
                                
                                if (countRange.Contains("~"))
                                {
                                    var rangeParts = countRange.Split('~');
                                    if (rangeParts.Length == 2 &&
                                        int.TryParse(rangeParts[0], out int minCnt) &&
                                        int.TryParse(rangeParts[1], out int maxCnt))
                                    {
                                        minCount = minCnt;
                                        maxCount = maxCnt;
                                        count = minCnt;
                                    }
                                }
                                else
                                {
                                    int.TryParse(countRange, out count);
                                }
                                
                                if (levelPart.StartsWith("(") && levelPart.EndsWith(")"))
                                {
                                    string innerPart = levelPart.Substring(1, levelPart.Length - 2);
                                    if (innerPart.Contains("~"))
                                    {
                                        var range = innerPart.Split('~');
                                        if (range.Length >= 2)
                                        {
                                            int.TryParse(range[0].Trim(), out int minLv);
                                            int.TryParse(range[1].Trim(), out int maxLv);
                                            minLevel = minLv;
                                            maxLevel = maxLv;
                                        }
                                    }
                                }
                                else if (int.TryParse(levelPart, out int lv))
                                {
                                    minLevel = maxLevel = lv;
                                }
                            }
                            else
                            {
                                if (countPart.Contains("~"))
                                {
                                    var rangeParts = countPart.Split('~');
                                    if (rangeParts.Length == 2 &&
                                        int.TryParse(rangeParts[0], out int minCnt) &&
                                        int.TryParse(rangeParts[1], out int maxCnt))
                                    {
                                        minCount = minCnt;
                                        maxCount = maxCnt;
                                        count = minCnt;
                                    }
                                }
                                else
                                {
                                    int.TryParse(countPart, out count);
                                }
                            }
                        }
                        else if (configPart.Contains("×") && configPart.StartsWith("("))
                        {
                            var parts = configPart.Split('×');
                            if (parts.Length >= 2)
                            {
                                string levelPart = parts[0].Trim();
                                string countPart = parts[1].Trim();
                                
                                if (levelPart.StartsWith("(") && levelPart.EndsWith(")"))
                                {
                                    string innerPart = levelPart.Substring(1, levelPart.Length - 2);
                                    if (innerPart.Contains("~"))
                                    {
                                        var range = innerPart.Split('~');
                                        if (range.Length >= 2)
                                        {
                                            int.TryParse(range[0].Trim(), out int minLv);
                                            int.TryParse(range[1].Trim(), out int maxLv);
                                            minLevel = minLv;
                                            maxLevel = maxLv;
                                        }
                                    }
                                    else if (int.TryParse(innerPart, out int lv))
                                    {
                                        minLevel = maxLevel = lv;
                                    }
                                }
                                
                                if (countPart.Contains("~"))
                                {
                                    var rangeParts = countPart.Split('~');
                                    if (rangeParts.Length == 2 &&
                                        int.TryParse(rangeParts[0], out int minCnt) &&
                                        int.TryParse(rangeParts[1], out int maxCnt))
                                    {
                                        minCount = minCnt;
                                        maxCount = maxCnt;
                                        count = minCnt;
                                    }
                                }
                                else if (int.TryParse(countPart, out int cnt))
                                {
                                    count = cnt;
                                }
                            }
                        }
                        else if (configPart.StartsWith("(") && configPart.EndsWith(")"))
                        {
                            string innerPart = configPart.Substring(1, configPart.Length - 2);
                            if (innerPart.Contains("~"))
                            {
                                var range = innerPart.Split('~');
                                if (range.Length >= 2)
                                {
                                    int.TryParse(range[0].Trim(), out int minLv);
                                    int.TryParse(range[1].Trim(), out int maxLv);
                                    minLevel = minLv;
                                    maxLevel = maxLv;
                                }
                            }
                        }
                        else if (int.TryParse(configPart, out int lv))
                        {
                            minLevel = maxLevel = lv;
                        }
                    }
                    else if (cid.Contains("×"))
                    {
                        var parts = cid.Split('×');
                        if (parts.Length >= 2)
                        {
                            cid = parts[0].Trim();
                            int.TryParse(parts[1].Trim(), out count);
                        }
                    }

                    var character = Agent.Instance.Content.Get<global::Data.Design.Ability>(x => x.cid == cid);
                    characterTuples.Add((cid, minLevel, maxLevel, count, minCount, maxCount, probability, character.id));
                }

                var charactersExport = characterTuples.Select(t =>
                {
                    var d = new Dictionary<string, object>
                    {
                        ["id"] = t.id,
                        ["count"] = t.count,
                        ["probability"] = t.probability
                    };
                    if (t.minCount.HasValue) d["minCount"] = t.minCount.Value;
                    if (t.maxCount.HasValue) d["maxCount"] = t.maxCount.Value;
                    if (t.minLevel.HasValue) d["minLevel"] = t.minLevel.Value;
                    if (t.maxLevel.HasValue) d["maxLevel"] = t.maxLevel.Value;
                    return d;
                }).ToList();

                var nameMultilingual = Agent.Instance.Content.Get<Multilingual>(m => m.cid == config.name);

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    {"id", config.id },
                    {"name", nameMultilingual.id },
                    {"type", ConvertTypeToEnglish(config.type) },
                    {"characters", JsonConvert.SerializeObject(charactersExport) },
                };
                datas.Add(data);
            }
            string path = $"{Utils.Paths.Library}/Config/Scene.csv";
            Utils.FileManager.Instance.DeleteFile(path);
            Utils.Csv.SaveByRows(datas, path);
        }
    }


}


