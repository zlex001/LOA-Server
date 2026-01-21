using Newtonsoft.Json;
using System.Linq;

namespace Logic.Design
{
    public class Plot : Ability
    {
        public string trigger;
        public string copy;
        public string maze;
        public string dialogues;
        public string condition;
        public string repeatable;
        public List<string> reward;


        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            id = Get<int>(dict, "id");
            cid = Get<string>(dict, "cid");
            trigger = Get<string>(dict, "trigger");
            copy = Get<string>(dict, "copy");
            maze = Get<string>(dict, "maze");
            dialogues = Get<string>(dict, "dialogues");
            condition = Get<string>(dict, "condition") ?? "";
            repeatable = Get<string>(dict, "repeatable") ?? "";
            reward = Get<string>(dict, "reward")?.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
            
        }

        public static void Convert()
        {
            try
            {
                List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
                var plots = Agent.Instance.Content.Gets<Plot>().ToList();
                
                foreach (Plot config in plots)
                {
                List<int> dialogues = new List<int>();
                if (!string.IsNullOrEmpty(config.dialogues))
                {
                    // 逗号分隔的对白cid列表
                    var cidList = config.dialogues.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
                    foreach (var cid in cidList)
                    {
                        var dialogue = Agent.Instance.Content.Get<Dialogue>(d => d.cid == cid);
                        if (dialogue != null)
                        {
                            dialogues.Add(dialogue.id);
                        }
                    }
                }
                string condition = ConvertConditionToJson(config.condition);
                bool repeatable = ConvertRepeatable(config.repeatable);
                List<List<object>> reward = new();
                foreach (string line in config.reward)
                {
                    var rewardTuple = ConvertRewardToTuple(line);
                    if (rewardTuple != null)
                    {
                        reward.Add(rewardTuple);
                    }
                }


                string convertedTrigger = ConvertTrigger(config.trigger);
                // 解析并转换copy字段为结构化数据
                var copyData = ParseAndConvertCopyData(config.copy);
                
                // 转换maze字段的cid到id
                int mazeId = 0;
                if (!string.IsNullOrEmpty(config.maze))
                {
                    var mazeData = Agent.Instance.Content.Get<Maze>(m => m.cid == config.maze);
                    if (mazeData != null)
                    {
                        mazeId = mazeData.id;
                    }
                }
                
                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    {"id", config.id },
                    {"trigger", convertedTrigger },
                    {"dialogues", JsonConvert.SerializeObject(dialogues)},
                    {"condition", condition},
                    {"repeatable", repeatable},
                    {"copy", JsonConvert.SerializeObject(copyData) },
                    {"maze", mazeId },
                    { "reward", JsonConvert.SerializeObject(reward) }

                };
                
                var copyInfo = copyData?.characters?.Count > 0 ? $"scope:{copyData.scope}, maps:{copyData.characters.Count}" : "empty";
                datas.Add(data);
            }
            
            string path = $"{Utils.Paths.Library}/Config/Plot.csv";
            Utils.FileManager.Instance.DeleteFile(path);
            Utils.Csv.SaveByRows(datas, path);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private static List<object> ConvertRewardToTuple(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return null;

            string[] parts = raw.Trim().Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return null;

            string rewardType = parts[0].Trim();
            string rewardValue = parts[1].Trim();

            switch (rewardType)
            {
                case "物品":
                    var itemParts = rewardValue.Split('×');
                    string itemCid = itemParts[0].Trim();
                    int amount = itemParts.Length > 1 ? int.Parse(itemParts[1]) : 1;
                    
                    var item = Agent.Instance.Content.Get<Item>(i => i.cid == itemCid);
                    if (item == null)
                        return null;
                        
                    return new List<object> { "Item", item.id, amount };

                case "经验":
                    int expAmount = int.Parse(rewardValue);
                    return new List<object> { "Exp", 0, expAmount };

                case "技能":
                    string skillCid = rewardValue.Trim();
                    var skill = Agent.Instance.Content.Get<Skill>(s => s.cid == skillCid);
                    if (skill == null)
                        return null;
                        
                    return new List<object> { "Skill", skill.id, 1 };

                default:
                    return null;
            }
        }


        private static Config.Plot.Copy ParseAndConvertCopyData(string copyString)
        {
            var result = new Config.Plot.Copy
            {
                scope = 0,
                characters = new Dictionary<int, List<Config.Plot.Character>>()
            };

            if (string.IsNullOrEmpty(copyString)) return result;


            var lines = copyString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;


                // 解析scope
                if (trimmedLine.StartsWith("【") && trimmedLine.Contains("】"))
                {
                    var scopeStr = trimmedLine.Substring(1, trimmedLine.IndexOf("】") - 1);
                    if (int.TryParse(scopeStr, out var scope))
                    {
                        result.scope = scope;
                    }
                }

                // 解析地图和角色配置
                if (trimmedLine.Contains("=>"))
                {
                    var parts = trimmedLine.Split(new[] { "=>" }, StringSplitOptions.None);
                    if (parts.Length != 2) 
                    {
                        continue;
                    }

                    var mapPart = parts[0].Trim();
                    var characterPart = parts[1].Trim();


                    // 提取地图CID
                    var scopeEndIndex = mapPart.IndexOf("】");
                    if (scopeEndIndex == -1) 
                    {
                        continue;
                    }
                    
                    var mapCid = mapPart.Substring(scopeEndIndex + 1);
                    
                    // 通过CID查找地图ID
                    var designMap = Agent.Instance.Content.Get<Map>(m => m.cid == mapCid);
                    if (designMap == null) 
                    {
                        continue;
                    }


                    // 解析角色配置
                    var character = ParseCharacterData(characterPart);
                    if (character != null)
                    {
                        if (!result.characters.ContainsKey(designMap.id))
                            result.characters[designMap.id] = new List<Config.Plot.Character>();
                        
                        result.characters[designMap.id].Add(character);
                    }
                    else
                    {
                    }
                }
            }

            return result;
        }

        private static Config.Plot.Character ParseCharacterData(string config)
        {
            var character = new Config.Plot.Character();

            // 处理方括号内容 [loot配置] 或 [嵌套角色配置]
            var bracketStart = config.IndexOf('[');
            var bracketEnd = config.IndexOf(']');
            if (bracketStart >= 0 && bracketEnd > bracketStart)
            {
                var bracketContent = config.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
                
                // 判断是loot还是nested：如果包含"%"说明是loot，否则是nested
                if (bracketContent.Contains('%'))
                {
                    character.loot = ParseLootData(bracketContent);
                }
                else
                {
                    // 嵌套角色配置，递归解析
                    character.nested = new List<Config.Plot.Character>();
                    var nestedChar = ParseCharacterData(bracketContent);
                    if (nestedChar != null)
                    {
                        character.nested.Add(nestedChar);
                    }
                }
                config = config.Substring(0, bracketStart).Trim();
            }

            // 解析角色CID和数量 "角色CID:等级×数量" 或 "容器CID×数量"
            string characterCid;
            var multiplyIndex = config.LastIndexOf('×');
            if (multiplyIndex >= 0)
            {
                var countStr = config.Substring(multiplyIndex + 1);
                if (int.TryParse(countStr, out var count))
                {
                    character.count = count;
                }
                config = config.Substring(0, multiplyIndex);
            }
            else
            {
                character.count = 1;
            }

            // 解析等级范围 "角色CID:5~10" 或 "角色CID:5"
            var colonIndex = config.IndexOf(':');
            if (colonIndex >= 0)
            {
                characterCid = config.Substring(0, colonIndex);
                var levelStr = config.Substring(colonIndex + 1);
                
                if (levelStr.Contains('~'))
                {
                    var levelParts = levelStr.Split('~');
                    if (levelParts.Length == 2 && 
                        int.TryParse(levelParts[0], out var min) && 
                        int.TryParse(levelParts[1], out var max))
                    {
                        character.min = min;
                        character.max = max;
                    }
                }
                else if (int.TryParse(levelStr, out var fixedLevel))
                {
                    character.min = fixedLevel;
                    character.max = fixedLevel;
                }
            }
            else
            {
                characterCid = config;
            }

            // 查找角色或道具ID
            
            var designLife = Agent.Instance.Content.Get<Life>(l => l.cid == characterCid);
            if (designLife != null)
            {
                character.id = designLife.id;
                return character;
            }

            var designItem = Agent.Instance.Content.Get<Item>(i => i.cid == characterCid);
            if (designItem != null)
            {
                character.id = designItem.id;
                return character;
            }

            return null;
        }

        private static List<Config.Plot.Loot> ParseLootData(string lootStr)
        {
            var loot = new List<Config.Plot.Loot>();
            
            // 解析 "物品CID:概率%*数量"
            var parts = lootStr.Split(':');
            if (parts.Length != 2) 
            {
                return loot;
            }

            var itemCid = parts[0].Trim();
            var probAndCount = parts[1];

            var percentIndex = probAndCount.IndexOf('%');
            if (percentIndex == -1) 
            {
                return loot;
            }

            var probStr = probAndCount.Substring(0, percentIndex);
            var countPart = probAndCount.Substring(percentIndex + 1).TrimStart('×');

            if (!double.TryParse(probStr, out var probability)) 
            {
                return loot;
            }

            int minCount = 1, maxCount = 1;
            if (countPart.Contains('~'))
            {
                var countParts = countPart.Split('~');
                if (countParts.Length == 2 && 
                    int.TryParse(countParts[0], out minCount) && 
                    int.TryParse(countParts[1], out maxCount))
                {
                }
                else
                {
                }
            }
            else if (int.TryParse(countPart, out var fixedCount))
            {
                minCount = maxCount = fixedCount;
            }
            else
            {
            }

            var designItem = Agent.Instance.Content.Get<Item>(i => i.cid == itemCid);
            if (designItem != null)
            {
                loot.Add(new Config.Plot.Loot
                {
                    id = designItem.id,
                    probability = probability / 100.0, // 转换为0-1概率
                    minCount = minCount,
                    maxCount = maxCount
                });
            }
            else
            {
            }

            return loot;
        }

        private static string ConvertTrigger(string chineseTrigger)
        {
            return chineseTrigger switch
            {
                "抵达" => "Logic.Map+Event.Arrived",
                "交谈" => "Logic.Life+Event.Talked",
                "拾取" => "Logic.Item+Event.Picked",
                "给予" => "Logic.Character+Event.Given",
                "死亡" => "Logic.Life+Event.Die",
                "进入" => "Logic.Item+Event.Enter",
                _ => chineseTrigger // 保持原样，支持已有的英文格式
            };
        }

        private static bool ConvertRepeatable(string repeatableText)
        {
            return !string.IsNullOrEmpty(repeatableText) && repeatableText.Trim() == "是";
        }

        private static string ConvertCondition(string cond)
        {
            if (string.IsNullOrEmpty(cond))
                return cond;
                
            var parts = cond.Split(':');
            if (parts.Length < 2)
                return cond;
            
            string prefix = parts[0];
            string cid = parts[1];

            return prefix switch
            {
                "Event" or "事件" => $"Event:{GetIdOrRecord<Plot>(cid, p => p.cid == cid, "Plot")}",
                "Target" or "目标" => $"Target:{GetIdOrRecord<Ability>(cid, c => c.cid != null && c.cid == cid, "Ability")}",
                "Skill" or "技能" => $"Skill:{GetIdOrRecord<Skill>(cid, s => s.cid == cid, "Skill")}",
                "Item" or "道具" => $"Item:{GetIdOrRecord<Item>(cid, i => i.cid == cid, "Item")}",
                _ => cond
            };
        }
        
        private static int GetIdOrRecord<T>(string cid, Func<T, bool> predicate, string typeName) where T : class
        {
            var item = Agent.Instance.Content.Get<T>(predicate);
            if (item == null)
            {
                Logic.Validation.Agent.Instance.Create<Logic.Validation.Error>("Reference not found", "Design", "Plot", cid,
                    $"Plot condition references {typeName} [{cid}] which is not defined");
                return 0;
            }
            
            var idField = typeof(T).GetField("id");
            if (idField == null)
                return 0;
            
            return (int)idField.GetValue(item);
        }

        private static string ConvertConditionToJson(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                return null;

            try
            {
                // 第1步：解析表达式为ConditionNode树
                var parser = new ConditionExpressionParser();
                var conditionTree = parser.Parse(expression);
                
                if (conditionTree == null)
                    return null;

                // 第2步：转换树中的条件（将cid转换为id）
                ConvertConditionTreeReferences(conditionTree);

                // 第3步：序列化为JSON
                return Newtonsoft.Json.JsonConvert.SerializeObject(conditionTree);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private static void ConvertConditionTreeReferences(Logic.Config.ConditionNode node)
        {
            if (node == null)
                return;

            if (node is Logic.Config.SingleCondition single)
            {
                single.Value = ConvertCondition(single.Value);
            }
            else if (node is Logic.Config.AndCondition and)
            {
                ConvertConditionTreeReferences(and.Left);
                ConvertConditionTreeReferences(and.Right);
            }
            else if (node is Logic.Config.OrCondition or)
            {
                ConvertConditionTreeReferences(or.Left);
                ConvertConditionTreeReferences(or.Right);
            }
            else if (node is Logic.Config.NotCondition not)
            {
                ConvertConditionTreeReferences(not.Child);
            }
        }


    }
}