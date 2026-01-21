using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Logic.Design
{
    public class Map :Ability
    {
        public string scene;
        public string name;
        public string type;
        public string description;
        public string[] characters;
        public string translate;
        public string xiulian;
        public string[] plotors;
        public string function;

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            id = Get<int>(dict, "id");
            cid = Get<string>(dict, "cid");
            scene = Get<string>(dict, "scene");
            name = Get<string>(dict, "name");
            type = Get<string>(dict, "type");
            description = Get<string>(dict, "description");
            characters = Get<string>(dict, "characters")?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToArray() ?? Array.Empty<string>();
            translate = Get<string>(dict, "translate");
            xiulian = Get<string>(dict, "xiulian");
            plotors = Get<string>(dict, "plotors")?.Split(',') ?? Array.Empty<string>();
        }

        private static string ConvertTypeToEnglish(string chineseType)
        {
            return chineseType switch
            {
                "路" => "Road",
                "树林" => "Forest",
                "洞穴" => "Cave",
                "岸边" => "Shore",
                "山坡" => "Slope",
                "草地" => "Grass",
                "雪地" => "Snow",
                "沼泽" => "Swamp",
                "沙地" => "Sand",
                "桥" => "Bridge",
                "山地" => "Mountain",
                "菜园" => "VegetableGarden",
                "田园" => "Farm",
                "果园" => "Orchard",
                "药园" => "HerbGarden",
                "墙" => "Wall",
                "房间" => "Room",
                "井" => "Well",
                "牧场" => "Ranch",
                "池塘" => "Pond",
                "迷宫入口" => "MazeEntrance",
                "食品店" => "FoodShop",
                "药品店" => "PotionShop",
                "魔法店" => "MagicShop",
                "轻装店" => "LightGearShop",
                "重装店" => "HeavyGearShop",
                "警察局" => "PoliceStation",
                "牢房" => "Prison",
                "银行" => "Bank",
                "商会" => "Guild",
                "市场" => "Market",
                "传送点" => "Teleport",
                "餐馆" => "Restaurant",
                "医院" => "Hospital",
                "农夫的家" => "FarmerHouse",
                "矿工的家" => "MinerHouse",
                "遗迹" => "Ruins",
                "稻田" => "RicePaddy",
                "麦田" => "WheatField",
                "瓜地" => "MelonField",
                "菜地" => "VegetableField",
                _ => ""
            };
        }
        
        public static void Convert()
        {
            List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
            foreach (Map config in Agent.Instance.Content.Gets<Map>())
            {
                Dictionary<string, List<string>> information = new Dictionary<string, List<string>>();
                if (config.description != default)
                {
                    information["Description"] = config.description.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                }
                Dictionary<string, List<string>> function = new Dictionary<string, List<string>>
                {
                    ["Translate"] = config.translate?.Split(',').ToList() ?? new List<string>(),
                    ["Xiulian"] = config.xiulian?.Split(',').ToList() ?? new List<string>()
                };
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

                    var character = Agent.Instance.Content.Get<Logic.Design.Ability>(x => x.cid == cid);
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
                
                if (nameMultilingual == null)
                {
                    throw new System.Exception($"Map Convert Error: Cannot find multilingual for name='{config.name}', cid='{config.cid}', id={config.id}");
                }

                int[] plotors = config.plotors
                    .Select(p =>
                    {
                        var plot = Agent.Instance.Content.Get<Plot>(x => x.cid == p);
                        return plot.id;
                    })
                    .ToArray();

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    {"id",  config.id  },
                    {"type", ConvertTypeToEnglish(config.type) },
                    {"name",  nameMultilingual.id },
                    {"characters",JsonConvert.SerializeObject(charactersExport) },
                    {"information", JsonConvert.SerializeObject(information)},
                    {"function", JsonConvert.SerializeObject(function)},
                    {"plotors", JsonConvert.SerializeObject(plotors)},
                };
                datas.Add(data);
            }
            string path = $"{Utils.Paths.Library}/Config/Map.csv";
            Utils.FileManager.Instance.DeleteFile(path);
            Utils.Csv.SaveByRows(datas, path);
        }
    }
}


