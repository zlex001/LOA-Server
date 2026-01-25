using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Basic;

namespace Logic.Design
{
    
    public class Movement :Ability
    {
        public string require;
        public string effects;
        public string description;
        public string cid;
        public HashSet<string> tags;

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            id = Get<int>(dict, "id");
            cid = Get<string>(dict, "cid");
            require = Get<string>(dict, "require");
            effects = Get<string>(dict, "effects");
            description = Get<string>(dict, "description");
            tags = Utils.Tag.ParseTagsFromString(Get<string>(dict, "tags"));
        }

        public static void Convert()
        {
            List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
            foreach (Movement config in Agent.Instance.Content.Gets<Movement>())
            {
                var multilingual = Agent.Instance.Content.Get<Multilingual>(m => m.cid == config.description);
                if (multilingual == null)
                {
                    continue;
                }

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    {"id", config.id },
                    {"description", multilingual.id },
                    {"require", ConvertRequireToJson(config.require) },
                    {"target", ConvertTargetFromTags(config.tags) },
                    {"effects", config.effects },
                    {"tags", JsonConvert.SerializeObject(config.tags?.ToList() ?? new List<string>()) },
                    {"text", JsonConvert.SerializeObject(new Dictionary<string, List<string>>()) },
                };
                datas.Add(data);
            }

            string path = $"{Utils.Paths.Library}/Config/Movement.csv";
            Utils.FileManager.Instance.DeleteFile(path);
            Utils.Csv.SaveByRows(datas, path);
        }

        private static string ConvertRequireToJson(string requireExpression)
        {
            if (string.IsNullOrEmpty(requireExpression))
                return null;

            try
            {
                // 解析表达式为ConditionNode树
                var parser = new ConditionExpressionParser();
                var conditionTree = parser.Parse(requireExpression);
                
                if (conditionTree == null)
                    return null;

                // 转换树中的条件（将中文转换为标准条件格式）
                ConvertRequireConditionTreeReferences(conditionTree);

                // 序列化为JSON
                return JsonConvert.SerializeObject(conditionTree);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void ConvertRequireConditionTreeReferences(Logic.Config.ConditionNode node)
        {
            if (node == null)
                return;

            if (node is Logic.Config.SingleCondition single)
            {
                single.Value = ConvertRequireCondition(single.Value);
            }
            else if (node is Logic.Config.AndCondition and)
            {
                ConvertRequireConditionTreeReferences(and.Left);
                ConvertRequireConditionTreeReferences(and.Right);
            }
            else if (node is Logic.Config.OrCondition or)
            {
                ConvertRequireConditionTreeReferences(or.Left);
                ConvertRequireConditionTreeReferences(or.Right);
            }
        }

        private static string ConvertRequireCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition))
                return condition;

            var parts = condition.Split(':');
            if (parts.Length < 2)
                return condition;
            
            string prefix = parts[0];
            string value = parts[1];

            return prefix switch
            {
                "身体" => ConvertBodyPartCondition(value),
                "武器" => ConvertWeaponCondition(value),
                _ => condition
            };
        }

        private static string ConvertBodyPartCondition(string partName)
        {
            string englishPart = partName switch
            {
                "手" => "Hand",
                "爪子" => "Claw", 
                "背" => "Back",
                "腿" => "Leg",
                _ => partName
            };
            return $"Part:{englishPart}";
        }

        private static string ConvertWeaponCondition(string weaponCondition)
        {
            if (weaponCondition.Contains("×"))
            {
                var parts = weaponCondition.Split('×');
                if (parts.Length == 2)
                {
                    string attribute = parts[0];
                    string amount = parts[1];
                    
                    string englishAttribute = attribute switch
                    {
                        "体积" => "Volume",
                        _ => attribute
                    };
                    
                    return $"Weapon:{englishAttribute}>={amount}";
                }
            }
            return $"Weapon:{weaponCondition}";
        }

        private static string ConvertTargetFromTags(HashSet<string> tags)
        {
            if (tags == null || tags.Count == 0)
            {
                var allPartTypes = Enum.GetValues<Logic.Part.Types>();
                return JsonConvert.SerializeObject(allPartTypes);
            }

            var targetParts = new List<string>();
            foreach (var tag in tags)
            {
                if (tag.StartsWith("Target:"))
                {
                    var partName = tag.Substring(7);
                    targetParts.Add(partName);
                }
            }

            if (targetParts.Count == 0)
            {
                var allPartTypes = Enum.GetValues<Logic.Part.Types>();
                return JsonConvert.SerializeObject(allPartTypes);
            }

            var targetList = new List<Logic.Part.Types>();
            foreach (var partName in targetParts)
            {
                if (Enum.TryParse<Logic.Part.Types>(partName, true, out var partType))
                {
                    targetList.Add(partType);
                }
            }

            return JsonConvert.SerializeObject(targetList.ToArray());
        }
    }
}


