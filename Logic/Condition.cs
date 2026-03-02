using System;
using System.Collections.Generic;
using System.Linq;
using Utils;
using Data.Config;

namespace Logic
{
    public static class Condition
    {
        public enum Type
        {
            Level,
            Event,
            Body,
            Part,
            Target,
            Skill,
            Item,
            Handle,
            Weapon
        }

        private static Dictionary<Type, Basic.Monitor.Condition> checkers = new Dictionary<Type, Basic.Monitor.Condition>();

        static Condition()
        {
            RegisterChecker(Type.Level, CheckLevel);
            RegisterChecker(Type.Event, CheckPlotor);
            RegisterChecker(Type.Body, CheckPart);
            RegisterChecker(Type.Part, CheckPart);      // Both Body and Part are supported
            RegisterChecker(Type.Target, CheckTarget);
            RegisterChecker(Type.Skill, CheckSkill);
            RegisterChecker(Type.Item, CheckItem);
            RegisterChecker(Type.Handle, CheckHandle);
            RegisterChecker(Type.Weapon, CheckHandle);  // Weapon is alias for Handle
        }

        public static void RegisterChecker(Type prefix, Basic.Monitor.Condition checker)
        {
            checkers[prefix] = checker;
        }

        public static bool Check(object target, List<string> requireList)
        {
            var (result, _) = CheckWithReason(target, requireList);
            return result;
        }

        public static (bool success, string failureReason) CheckWithReason(object target, List<string> requireList)
        {
            if (requireList == null || requireList.Count == 0)
                return (true, null);

            foreach (string condition in requireList)
            {
                var (success, reason) = CheckSingleWithReason(target, condition);
                if (!success)
                    return (false, reason);
            }

            return (true, null);
        }

        private static (bool success, string failureReason) CheckSingleWithReason(object target, string condition)
        {
            Type? prefix = GetPrefix(condition);
            if (prefix.HasValue && checkers.ContainsKey(prefix.Value))
            {
                if (prefix.Value == Type.Skill)
                {
                    return CheckSkillWithReason(target, condition);
                }
                else
                {
                    bool result = checkers[prefix.Value](target, condition);
                    return (result, result ? null : $"{prefix.Value} condition not satisfied: {condition}");
                }
            }
            else
            {
                return (false, $"Unknown condition type: {prefix?.ToString() ?? "null"}");
            }
        }

        public static bool Check(global::Data.Ability element, List<string> conditions, object[] eventArgs)
        {
            if (eventArgs == null || eventArgs.Length == 0)
            {
                return Check(element, conditions);
            }

            if (conditions == null || conditions.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < conditions.Count; i++)
            {
                string condition = conditions[i];
                bool result = Check(element, condition, eventArgs);
                if (!result)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Check(global::Data.Ability element, string condition, object[] eventArgs)
        {
            if (string.IsNullOrEmpty(condition))
                return true;

            Type? prefix = GetPrefix(condition);

            if (prefix.HasValue && checkers.TryGetValue(prefix.Value, out var checker))
            {
                if (prefix.Value == Type.Target)
                {
                    return checker(element, condition, eventArgs);
                }
                else
                {
                    return checker(element, condition);
                }
            }

            return false;
        }

        public static bool Check(object target, string condition)
        {
            if (string.IsNullOrEmpty(condition))
                return true;

            Type? prefix = GetPrefix(condition);

            if (prefix.HasValue && checkers.TryGetValue(prefix.Value, out var checker))
            {
                return checker(target, condition);
            }

            return false;
        }

        private static Type? GetPrefix(string condition)
        {
            string prefixStr = GetPrefixString(condition);

            if (Enum.TryParse<Type>(prefixStr, true, out Type result))
                return result;

            return null;
        }

        private static string GetPrefixString(string condition)
        {
            int index = condition.IndexOf(':');

            if (index == -1)
            {
                if (condition.StartsWith("Level"))
                    return "Level";
                return condition;
            }
            return condition.Substring(0, index);
        }

        #region 内置条件检查器

        private static bool CheckLevel(params object[] args)
        {
            if (args.Length < 2 || !(args[0] is global::Data.Life) || !(args[1] is string))
                return false;

            global::Data.Life life = args[0] as global::Data.Life;
            string condition = args[1] as string;

            if (condition.StartsWith("Level"))
            {
                string op = GetOperator(condition);
                int reqLevel = GetRequiredValue(condition, op);
                int lifeLevel = life.Level;

                return CompareValues(lifeLevel, op, reqLevel);
            }
            return false;
        }

        private static bool CheckPlotor(params object[] args)
        {
            global::Data.Ability element = (global::Data.Ability)args[0];
            string condition = (string)args[1];

            string[] parts = condition.Split(':');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int id))
            {
                bool hasQuest = element.Content.Has<global::Data.Quest>(s => s.Config.Id == id);


                return hasQuest;
            }
            return false;
        }

        private static bool CheckPart(params object[] args)
        {
            if (args.Length < 2 || !(args[0] is global::Data.Ability) || !(args[1] is string))
            {
                return false;
            }

            global::Data.Ability element = (global::Data.Ability)args[0];
            string condition = (string)args[1];

            string[] parts = condition.Split(':');
            if (parts.Length < 2)
            {
                return false;
            }

            // 获取要求的身体部位
            string requiredPart = parts[1].Trim();

            // 检查元素是否有身体部位组件
            if (!(element is global::Data.Life life))
            {
                return false;
            }

            // 将中文部位名称转换为英文枚举值
            global::Data.Part.Types partType;
            if (Enum.TryParse(requiredPart, true, out partType))
            {
                // 检查生命体是否有指定类型的身体部位
                return life.Content.Has<global::Data.Part>(p => p.Type == partType);
            }
            else
            {
                return false;
            }
        }

        private static bool CheckTarget(params object[] args)
        {
            if (args.Length < 3 || !(args[0] is global::Data.Ability) || !(args[1] is string) || !(args[2] is object[]))
            {
                return false;
            }

            global::Data.Ability element = (global::Data.Ability)args[0];
            string condition = (string)args[1];
            object[] eventArgs = (object[])args[2];

            if (eventArgs.Length < 2)
            {
                return false;
            }

            string[] parts = condition.Split(':');
            if (parts.Length < 2)
            {
                return false;
            }

            object targetObj = eventArgs[0];

            if (!(targetObj is global::Data.Character character))
            {
                return false;
            }

            int targetId;
            if (character is global::Data.Life life && life is not global::Data.Player)
            {
                targetId = life.Config.Id;
            }
            else if (character is global::Data.Item item)
            {
                targetId = item.Config.Id;
            }
            else
            {
                return false;
            }

            if (!int.TryParse(parts[1], out int expectedId))
            {
                return false;
            }

            bool result = targetId == expectedId;

            return result;
        }

        private static bool CheckSkill(params object[] args)
        {
            var (result, _) = CheckSkillWithReason(args);
            return result;
        }

        private static (bool success, string failureReason) CheckSkillWithReason(params object[] args)
        {
            if (args.Length < 2 || !(args[0] is global::Data.Life) || !(args[1] is string))
                return (false, "参数无效");

            global::Data.Life life = args[0] as global::Data.Life;
            string condition = args[1] as string;

            string[] parts = condition.Split(':');
            if (parts.Length < 2)
                return (false, "条件格式错误");

            string skillInfo = parts[1].Trim();

            string op = GetOperator(skillInfo);
            if (!string.IsNullOrEmpty(op))
            {
                string skillIdentifier = skillInfo.Substring(0, skillInfo.IndexOf(op)).Trim();
                int reqLevel = GetRequiredValue(skillInfo, op);

                // 首先尝试作为int ID解析
                if (int.TryParse(skillIdentifier, out int skillId))
                {
                    // 使用int ID匹配技能（从Life和所有Part中查找）
                    var skill = life.GetAllSkills().FirstOrDefault(s => s.Config.Id == skillId);

                    if (skill == null)
                        return (false, $"技能未找到:ID={skillId}");

                    bool levelCheck = CompareValues(skill.Level, op, reqLevel);
                    if (!levelCheck)
                        return (false, $"等级不足:ID={skillId}({skill.Level}{op}{reqLevel})");

                    return (true, null);
                }
                else
                {
                    // 如果不是数字ID，说明转换有问题
                    return (false, $"技能ID格式错误:{skillIdentifier}");
                }
            }
            else
            {
                // 无等级要求，只检查是否拥有该技能
                if (int.TryParse(skillInfo, out int skillId))
                {
                    // 使用int ID匹配技能（从Life和所有Part中查找）
                    bool hasSkill = life.GetAllSkills().Any(s => s.Config.Id == skillId && s.Level > 0);

                    if (!hasSkill)
                        return (false, $"技能未拥有:ID={skillId}");

                    return (true, null);
                }
                else
                {
                    // 如果不是数字ID，说明转换有问题
                    return (false, $"技能ID格式错误:{skillInfo}");
                }
            }
        }

        private static bool CheckItem(params object[] args)
        {
            if (args.Length < 2 || !(args[0] is global::Data.Life) || !(args[1] is string))
                return false;

            global::Data.Life life = args[0] as global::Data.Life;
            string condition = args[1] as string;

            string[] parts = condition.Split(':');
            if (parts.Length < 2)
                return false;

            string itemIdStr = parts[1].Trim();

            // 尝试解析为数字ID
            if (!int.TryParse(itemIdStr, out int requiredItemId))
                return false;


            // 检查是否有匹配的道具（按ID匹配）
            foreach (var item in Exchange.Agent.GetEquipments(life))
            {
                if (item.Config.Id == requiredItemId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CheckHandle(params object[] args)
        {
            if (args.Length < 2 || !(args[0] is global::Data.Life) || !(args[1] is string))
                return false;

            global::Data.Life life = args[0] as global::Data.Life;
            string condition = args[1] as string;

            string[] parts = condition.Split(':');
            if (parts.Length < 2)
                return false;

            global::Data.Item handleItem = Exchange.Agent.GetHandleItem(life);
            if (handleItem == null)
                return false;

            string checkContent = parts[1].Trim();

            string op = GetOperator(checkContent);
            if (!string.IsNullOrEmpty(op))
            {
                string fieldName = checkContent.Substring(0, checkContent.IndexOf(op)).Trim();
                int requiredValue = GetRequiredValue(checkContent, op);

                int actualValue = fieldName switch
                {
                    "体积" => handleItem.Config.volume,
                    "重量" => handleItem.Config.weight,
                    "价值" => handleItem.Config.value,
                    _ => 0
                };

                return CompareValues(actualValue, op, requiredValue);
            }
            else
            {
                return handleItem.Config.Tags?.Contains(checkContent) ?? false;
            }
        }

        #endregion

        #region 辅助方法

        public static string GetOperator(string condition)
        {
            if (condition.Contains(">=")) return ">=";
            if (condition.Contains(">")) return ">";
            if (condition.Contains("==")) return "==";
            if (condition.Contains("<=")) return "<=";
            if (condition.Contains("<")) return "<";
            return string.Empty;
        }

        public static int GetRequiredValue(string condition, string op)
        {
            if (string.IsNullOrEmpty(op))
                return 0;

            int value;
            string valueStr = condition.Substring(condition.IndexOf(op) + op.Length);
            if (int.TryParse(valueStr, out value))
                return value;
            return 0;
        }

        public static bool CompareValues(int value1, string op, int value2)
        {
            switch (op)
            {
                case ">=": return value1 >= value2;
                case ">": return value1 > value2;
                case "==": return value1 == value2;
                case "<=": return value1 <= value2;
                case "<": return value1 < value2;
                default: return false;
            }
        }

        #endregion
    }
}
