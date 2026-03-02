using Basic;
using Data;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Logic.Display
{
    public class Information
    {
        public static object[] Item(params object[] args)
        {
            Player player = (Player)args[0];
            global::Data.Item target = (global::Data.Item)args[1];
            List<object> results = new List<object>();
            results.Add(new Option.Item(Logic.Text.Decorate.Item(target, player)));
            results.Add(new Option.Item($"{Utils.Text.Indent(0)}{Logic.Text.Description.Item(target, player)}"));

            if (target.MaxDurability > 0)
            {
                results.Add(new Option.Item(Option.Item.Type.ProgressWithValue, ("Text", $"{Utils.Text.Indent(1)}{Text.Agent.Instance.Get(global::Data.Text.Labels.Durability, player)}"), ("SliderValues", JsonConvert.SerializeObject(new int[] { target.Durability, target.MaxDurability })), ("ValueColor", Utils.Text.color[Utils.Text.Colors.Default])));
            }

            if (target.Container?.Count > 0)
            {
                var containerProgressItems = GetContainerProgressItems(target, player);
                results.AddRange(containerProgressItems);
            }
            else
            {
                // 非容器物品显示基本重量和体积信息
                results.Add(new Option.Item($"{Utils.Text.Indent(1)}{Text.Agent.Instance.Get(global::Data.Text.Labels.Weight, player)}：{target.Config.weight}"));
                results.Add(new Option.Item($"{Utils.Text.Indent(1)}{Text.Agent.Instance.Get(global::Data.Text.Labels.Volume, player)}：{target.Config.volume}"));
            }

            if (target.Lock)
            {
                results.Add(new Option.Item($"{Utils.Text.Indent(0)}{Text.Agent.Instance.Get(global::Data.Text.Labels.ItemLocked, player)}"));
            }
            foreach (Character character in target.Content.Gets<Character>())
            {

                if (character is Item item)
                {
                    results.Add(new Option.Item(Option.Item.Type.Button, ("Text", Logic.Text.Decorate.Item(item, player)), ("Action", $"ContainerItem_{character.GetHashCode()}")));
                }
                else if (character is Life life)
                {
                    results.Add(new Option.Item(Option.Item.Type.Button, ("Text", Logic.Text.Decorate.Life(life, player)), ("Action", $"ContainerItem_{character.GetHashCode()}")));
                }



            }


            return results.ToArray();
        }

        public static object[] Life(params object[] args)
        {

            var sub = (Player)args[0];
            var obj = (global::Data.Life)args[1];
            var finals = new List<object>();
            finals.Add(new Option.Item(Logic.Text.Decorate.Life(obj, sub)));
            finals.Add(new Option.Item($"{Utils.Text.Indent(0)}{Text.Description.Life(obj, sub)}"));
            var sortedParts = obj.Content.Gets<Part>().OrderBy(p => (int)p.Type).ToList();
            for (int i = 0; i < sortedParts.Count; i++)
            {
                Part part = sortedParts[i];
                var title = Text.Agent.Instance.Get((int)part.Type, sub);
                var button = part.Content.Has<global::Data.Item>() ? Logic.Text.Decorate.Item(part.Content.Get<global::Data.Item>(), sub) : "";
                finals.Add(new Option.Item(Option.Item.Type.TitleButtonWithProgress, ("Title", $"{Utils.Text.Indent(1)}{title}"), ("Button", button), ("Action", $"EquipmentSlot_{part.Type}"), ("PartIndex", i.ToString()), ("SliderValues", JsonConvert.SerializeObject(new int[] { part.Hp, part.MaxHp })), ("ValueColor", Utils.Text.GetDangerRatioColor(part.data.GetRatio<int>(global::Data.Part.Data.Hp)))));
            }
            return finals.ToArray();
        }

        public static object[] Player(params object[] args)
        {
            var sub = (global::Data.Player)args[0];
            var obj = (global::Data.Player)args[1];
            var finals = new List<object>();
            finals.Add(new Option.Item(Logic.Text.Decorate.Player(obj, sub)));
            finals.Add(new Option.Item($"{Utils.Text.Indent(0)}{Text.Description.Life(obj, sub)}"));
            if (sub == obj)
            {
                finals.Add(new Option.Item(Option.Item.Type.Progress, ("Text", $"{Utils.Text.Format(Text.Agent.Instance.Get(global::Data.Text.Labels.Level, sub), "level", obj.Level.ToString())}"), ("SliderValues", JsonConvert.SerializeObject(new[] { obj.Exp, obj.NextExp })), ("ValueColor", Utils.Text.color[Utils.Text.Colors.Default])));
            }
            var sortedParts = obj.Content.Gets<Part>().OrderBy(p => (int)p.Type).ToList();
            for (int i = 0; i < sortedParts.Count; i++)
            {
                Part part = sortedParts[i];
                var title = Text.Agent.Instance.Get((int)part.Type, sub);
                var button = part.Content.Has<global::Data.Item>() ? Logic.Text.Decorate.Item(part.Content.Get<global::Data.Item>(), sub) : "";
                finals.Add(new Option.Item(Option.Item.Type.TitleButtonWithProgress, ("Title", $"{Utils.Text.Indent(1)}{title}"), ("Button", button), ("Action", $"EquipmentSlot_{part.Type}"), ("PartIndex", i.ToString()), ("SliderValues", JsonConvert.SerializeObject(new int[] { part.Hp, part.MaxHp })), ("ValueColor", Utils.Text.GetDangerRatioColor(part.data.GetRatio<int>(global::Data.Part.Data.Hp)))));
            }
            if (sub == obj)
            {
                finals.Add(new Option.Item($"{Utils.Text.Indent(1)}{GetLifeAtkText(obj, sub)}"));
                finals.Add(new Option.Item($"{Utils.Text.Indent(1)}{GetLifeDefText(obj, sub)}"));
                finals.Add(new Option.Item($"{Utils.Text.Indent(1)}{GetLifeAgiText(obj, sub)}"));
                finals.Add(new Option.Item($"{Utils.Text.Indent(1)}{GetLifeIneText(obj, sub)}"));
                finals.Add(new Option.Item($"{Utils.Text.Indent(1)}{GetLifeConText(obj, sub)}"));
                foreach (Skill skill in obj.Content.Gets<Skill>())
                {
                    finals.Add(new Option.Item(Option.Item.Type.Progress, ("Text", $"{Logic.Text.Decorate.Skill(skill, sub)}\u00A0{skill.Level}"), ("SliderValues", JsonConvert.SerializeObject(Utils.Mathematics.Exps(skill.Exp))), ("ValueColor", Utils.Text.color[Utils.Text.Colors.Default])));
                }
            }

            return [.. finals];
        }


        private static List<Option.Item> GetContainerProgressItems(global::Data.Item item, global::Data.Player player)
        {
            var progressItems = new List<Option.Item>();
            foreach (var key in item.Container)
            {
                if (key.Key == "Capacity" && key.Value > 0)
                {
                    int contentVolume = Logic.Exchange.Load.GetContentVolume(item);
                    string colorName = GetProgressColorName(contentVolume, key.Value);
                    progressItems.Add(new Option.Item
                    {
                        type = Option.Item.Type.ProgressWithValue,
                        data = new Dictionary<string, string>
                        {
                            { "Text", $"{Utils.Text.Indent(1)}{Text.Agent.Instance.Get( global::Data.Text.Labels.Volume, player)}" },
                            { "SliderValues", JsonConvert.SerializeObject(new int[] { contentVolume, key.Value }) },
                            { "ValueColor", colorName }
                        }
                    });
                }
                else if (key.Key == "Carry" && key.Value > 0)
                {
                    int contentWeight = Logic.Exchange.Load.GetContentWeight(item);
                    string colorName = GetProgressColorName(contentWeight, key.Value);
                    progressItems.Add(new Option.Item
                    {
                        type = Option.Item.Type.ProgressWithValue,
                        data = new Dictionary<string, string>
                        {
                            { "Text", $"{Utils.Text.Indent(1)}{Text.Agent.Instance.Get( global::Data.Text.Labels.Weight, player)}" },
                            { "SliderValues", JsonConvert.SerializeObject(new int[] { contentWeight, key.Value }) },
                            { "ValueColor", colorName }
                        }
                    });
                }
            }

            return progressItems;
        }

        private static string GetProgressColorName(int used, int max)
        {
            return Utils.Text.GetDangerRatioColor(used, max);
        }

        private static string GetLifeAtkText(global::Data.Life life, global::Data.Player player)
        {
            return GetAttributeTextWithModifier(life, player, global::Data.Life.Attributes.Atk);
        }

        private static string GetLifeDefText(global::Data.Life life, global::Data.Player player)
        {
            return GetAttributeTextWithModifier(life, player, global::Data.Life.Attributes.Def);
        }

        private static string GetLifeAgiText(global::Data.Life life, global::Data.Player player)
        {
            return GetAttributeTextWithModifier(life, player, global::Data.Life.Attributes.Agi);
        }

        private static string GetLifeConText(global::Data.Life life, global::Data.Player player)
        {
            return GetAttributeTextWithModifier(life, player, global::Data.Life.Attributes.Con);
        }

        private static string GetLifeIneText(global::Data.Life life, global::Data.Player player)
        {
            return GetAttributeTextWithModifier(life, player, global::Data.Life.Attributes.Ine);
        }

        private static string GetAttributeTextWithModifier(global::Data.Life life, global::Data.Player player, global::Data.Life.Attributes attribute)
        {
            var attributeName = Text.Agent.Instance.Get(attribute, player);
            var modifiedValue = GetAttributeValue(life, attribute);
            var baseValue = GetBaseAttributeValue(life, attribute);
            var difference = modifiedValue - baseValue;

            if (difference == 0)
            {
                return $"{attributeName}\u00A0{modifiedValue}";
            }
            else
            {
                var diffColor = difference > 0 ? Utils.Text.Colors.Success : Utils.Text.Colors.Error;
                var diffText = Utils.Text.Color(diffColor, Utils.Text.SignNum(difference));
                return $"{attributeName}\u00A0{baseValue} {diffText}";
            }
        }

        private static int GetAttributeValue(global::Data.Life life, global::Data.Life.Attributes attribute)
        {
            return attribute switch
            {
                global::Data.Life.Attributes.Atk => (int)life.Atk,
                global::Data.Life.Attributes.Def => (int)life.Def,
                global::Data.Life.Attributes.Agi => (int)life.Agi,
                global::Data.Life.Attributes.Con => (int)life.Con,
                global::Data.Life.Attributes.Ine => (int)life.Ine,
                _ => 0
            };
        }

        private static int GetBaseAttributeValue(global::Data.Life life, global::Data.Life.Attributes attribute)
        {
            var baseValue = Logic.Mathematics.Instance.AttributeValue(life.Grade, attribute, life.Level);
            return (int)baseValue;
        }

        private static string GetCriminalDescription(global::Data.Life life, global::Data.Player player)
        {
            var punishment = life.Content.Get<global::Data.Punishment>();
            if (punishment == null || punishment.Crimes == null || punishment.Crimes.Count == 0)
                return "";

            var criminalTitles = punishment.Crimes
                .Select(crime => Text.Agent.Instance.Get((int)crime, player))
                .ToList();

            return $"（{string.Join("，", criminalTitles)}）";
        }

        private static string ConvertConditionFailureToDisplay(string condition, string failureReason)
        {
            if (string.IsNullOrEmpty(failureReason))
                return $"条件不满足: {condition}";

            string[] parts = condition.Contains('·') ? condition.Split('·') : condition.Split(':');
            if (parts.Length < 2)
                return failureReason;

            string prefix = parts[0].Trim();
            string content = parts[1].Trim();

            switch (prefix)
            {
                case "武学":
                    if (failureReason.Contains("技能未找到"))
                    {
                        return $"需要武学: {content}";
                    }
                    else if (failureReason.Contains("等级不足"))
                    {
                        var match = Regex.Match(failureReason, @"(\d+)(>=|<=|>|<|==)(\d+)");
                        if (match.Success)
                        {
                            int currentLevel = int.Parse(match.Groups[1].Value);
                            string op = match.Groups[2].Value;
                            int requiredLevel = int.Parse(match.Groups[3].Value);

                            string skillName = content;
                            if (int.TryParse(content.Split(new char[] { '>', '<', '=' }, StringSplitOptions.RemoveEmptyEntries)[0], out int skillId))
                            {
                                var skillConfig = global::Data.Config.Agent.Instance.Content.Get<global::Data.Config.Skill>(s => s.Id == skillId);
                                if (skillConfig != null)
                                {
                                    skillName = Utils.Text.GetFirstString(skillConfig.text, "Name") ?? $"武学{skillId}";
                                }
                            }

                            return $"{Utils.Text.Color(Utils.Text.Colors.EntitySkill, skillName)}需达{Utils.Text.Chinese(requiredLevel)}重（当前{Utils.Text.Chinese(currentLevel)}重）";
                        }
                        return $"武学等级不足: {content}";
                    }
                    else if (failureReason.Contains("技能未拥有"))
                    {
                        return $"需要学习武学: {content}";
                    }
                    return $"武学条件不满足: {content}";

                case "物品":
                    if (failureReason.Contains("物品不足"))
                    {
                        return $"需要物品: {content}";
                    }
                    return $"物品条件不满足: {content}";

                case "等级":
                    if (failureReason.Contains("等级不足"))
                    {
                        var match = Regex.Match(condition, @"等级(>=|<=|>|<|==)(\d+)");
                        if (match.Success)
                        {
                            string op = match.Groups[1].Value;
                            int requiredLevel = int.Parse(match.Groups[2].Value);
                            return $"需要等级{op}{requiredLevel}";
                        }
                    }
                    return $"等级条件不满足: {condition}";

                case "身体":
                    return $"需要身体部位: {content}";

                default:
                    return failureReason;
            }
        }
    }
}


