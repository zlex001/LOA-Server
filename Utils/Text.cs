using System.Text;
using System.Text.RegularExpressions;

namespace Utils
{
    public static class Text
    {
        #region Constants
        public static readonly List<string> INDENTS = new List<string> { $"\u00A0•\u00A0", $"\u00A0·\u00A0" };
        public static readonly List<string> MASTER_STAGE = new List<string> { "基本", "初阶", "中阶", "高阶", "绝学" };
        #endregion

        #region Enums
        public enum Colors
        {
            Default,
            Muted,
            Disabled,
            Success,
            Warning,
            Error,
            Info,
            Highlight,
            EntityPlayer,
            EntityNPC,
            EntityItem,
            EntitySkill,
            EntityMap,
            EntityMovement,
            ValueHealth,
            ValueMana,
            ValueStamina,
            ValueSpirit,
            ValueExp,
            ChannelSystem,
            ChannelLocal,
            ChannelPrivate,
            ChannelAll,
            ChannelRumor,
            ChannelAutomation,
            Quality0,
            Quality1,
            Quality2,
            Quality3,
            Quality4,
            Quality5,
            Quality6,
            DamageLight,
            DamageModerate,
            DamageHeavy,
            DamageSevere,
            DamageCritical,
        }
        #endregion

        #region Core Utilities
        public static string Format(string template, params string[] kvs)
        {
            var replacer = new Replacer(template);
            for (int i = 0; i < kvs.Length - 1; i += 2)
            {
                replacer.Add(kvs[i], kvs[i + 1]);
            }
            return replacer.Execute();
        }

        public static Enum ParseEnum(string full)
        {
            if (string.IsNullOrWhiteSpace(full)) return null;

            int lastDot = full.LastIndexOf('.');
            if (lastDot <= 0) return null;

            string typeName = full[..lastDot];
            string enumName = full[(lastDot + 1)..];

            Type enumType = Type.GetType(typeName) ?? Type.GetType($"{typeName}, Data");
            return (Enum)Enum.Parse(enumType, enumName);
        }

        public static string GetFullName(Enum e)
        {
            return $"{e.GetType().FullName}.{e}";
        }

        public static int[] Version(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return new int[] { 0, 0, 0 };
            }

            string[] parts = version.Split('.');
            int[] numbers = new int[3]; // 固定长度为3

            for (int i = 0; i < 3; i++)
            {
                if (i < parts.Length && int.TryParse(parts[i], out int number))
                {
                    numbers[i] = number;
                }
                else
                {
                    numbers[i] = 0;
                }
            }

            return numbers;
        }
        #endregion

        #region Text Formatting
        public static string Indent(int level)
        {
            // Clamp level to valid range, use last style for deeper levels
            int styleIndex = Math.Min(level, INDENTS.Count - 1);
            string title = INDENTS[styleIndex];

            // Insert non-breaking spaces based on level
            string indentation = new string('\u00A0', level * 3);

            return indentation + title;
        }

        public static string SignNum(int num) => (num >= 0) ? $"+{num}" : num.ToString();



        public static string GetFirstString(Dictionary<string, List<string>> dictionary, string key)
        {
            return dictionary.TryGetValue(key, out var list) && list.Count > 0 ? list[0] : null;
        }
        #endregion

        #region Color Management
        public static readonly Dictionary<Colors, string> color = new()
        {
            { Colors.Default, "FFFFFFFF" },
            { Colors.Muted, "808080FF" },
            { Colors.Disabled, "606060FF" },
            { Colors.Success, "6A9955FF" },
            { Colors.Warning, "CE9178FF" },
            { Colors.Error, "F48771FF" },
            { Colors.Info, "569CD6FF" },
            { Colors.Highlight, "DCDCAAFF" },
            { Colors.EntityPlayer, "9CDCFEFF" },
            { Colors.EntityNPC, "D69D85FF" },
            { Colors.EntityItem, "4EC9B0FF" },
            { Colors.EntitySkill, "DCDCAAFF" },
            { Colors.EntityMap, "7CA5A0FF" },
            { Colors.EntityMovement, "EFB839FF" },
            { Colors.ValueHealth, "A52A2AFF" },
            { Colors.ValueMana, "CC7A00FF" },
            { Colors.ValueStamina, "469955FF" },
            { Colors.ValueSpirit, "3D88C9FF" },
            { Colors.ValueExp, "6A9955FF" },
            { Colors.ChannelSystem, "D7BA7DFF" },
            { Colors.ChannelLocal, "FFFFFFFF" },
            { Colors.ChannelPrivate, "C586C0FF" },
            { Colors.ChannelAll, "BBA08CFF" },
            { Colors.ChannelRumor, "BD63C5FF" },
            { Colors.ChannelAutomation, "9CDCFEFF" },
            { Colors.Quality0, "C8C8C8FF" },
            { Colors.Quality1, "00FF00FF" },
            { Colors.Quality2, "00D8FFFF" },
            { Colors.Quality3, "C77EFFFF" },
            { Colors.Quality4, "FF8C00FF" },
            { Colors.Quality5, "FF4040FF" },
            { Colors.Quality6, "FFD700FF" },
            { Colors.DamageLight, "FF9999FF" },
            { Colors.DamageModerate, "FF6666FF" },
            { Colors.DamageHeavy, "FF3333FF" },
            { Colors.DamageSevere, "FF0000FF" },
            { Colors.DamageCritical, "CC0000FF" },
        };

        private static string StripColorTags(string input)
        {
            return Regex.Replace(input, @"<\/?color(=[^>]*)?>", "", RegexOptions.IgnoreCase);
        }

        public static string Color(Colors key, string text)
        {
            string cleanText = StripColorTags(text);
            return $"<color=#{(color.TryGetValue(key, out var hex) ? hex : "FFFFFFFF")}>{cleanText}</color>";
        }

        public static string LevelColor(int stage, string text)
        {
            var qualityKey = stage switch
            {
                0 => Colors.Quality0,
                1 => Colors.Quality1,
                2 => Colors.Quality2,
                3 => Colors.Quality3,
                4 => Colors.Quality4,
                5 => Colors.Quality5,
                6 => Colors.Quality6,
                _ => Colors.Default
            };
            return Color(qualityKey, text);
        }



        /// <summary>
        /// 根据伤害比例获取对应的伤害颜色
        /// </summary>
        /// <param name="damageRatio">伤害/最大生命值的比例</param>
        /// <returns>颜色枚举值</returns>
        public static Colors GetDamageColor(double damageRatio)
        {
            return damageRatio switch
            {
                >= 0.70 => Colors.DamageCritical,  // 70%+ 致命伤害
                >= 0.50 => Colors.DamageSevere,    // 50-70% 严重伤害
                >= 0.30 => Colors.DamageHeavy,     // 30-50% 重伤
                >= 0.15 => Colors.DamageModerate,  // 15-30% 中伤
                _ => Colors.DamageLight             // 0-15% 轻伤
            };
        }

        /// <summary>
        /// 根据使用率获取统一的危险度颜色（通用于背包承重、Part Hp等）
        /// </summary>
        /// <param name="used">已使用数值</param>
        /// <param name="max">最大数值</param>
        /// <returns>颜色十六进制字符串</returns>
        public static string GetDangerRatioColor(int used, int max)
        {
            if (max <= 0) return color[Colors.Info];

            float ratio = (float)used / max;

            if (ratio >= 1.0f)
                return color[Colors.Error];
            else if (ratio >= 0.8f)
                return color[Colors.Warning];
            else if (ratio >= 0.5f)
                return color[Colors.Info];
            else
                return color[Colors.Success];
        }

        /// <summary>
        /// 根据使用率获取统一的危险度颜色（double版本，用于Hp百分比等）
        /// </summary>
        /// <param name="ratio">使用比例 (0.0 - 1.0)</param>
        /// <returns>颜色十六进制字符串</returns>
        public static string GetDangerRatioColor(double ratio)
        {
            double dangerRatio = 1.0 - ratio;
            
            if (dangerRatio >= 0.8)
                return color[Colors.Error];
            else if (dangerRatio >= 0.5)
                return color[Colors.Warning];
            else if (dangerRatio >= 0.2)
                return color[Colors.Info];
            else
                return color[Colors.Success];
        }
        #endregion

        #region Validation and Security
        public static bool SafeAccount(string str, int minLength, int maxLength)
        {
            string pattern = "^[a-zA-Z0-9]{" + minLength + "," + maxLength + "}$";
            return Regex.IsMatch(str, pattern);
        }

        public static bool SafeName(string str)
        {
            string pattern = @"^[\u4e00-\u9fa5a-zA-Z0-9]*$";
            return Regex.IsMatch(str, pattern);
        }
        #endregion

        #region Number Conversion
        public static string Chinese(int num)
        {
            if (num < 0)
            {
                return "?";
            }
            if (num == 0)
            {
                return "零";
            }

            string[] intArr = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            string[] strArr = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            string[] Chinese = { "", "十", "百", "千", "万", "十", "百", "千", "亿" };


            try
            {
                // 将整数转换为字符数组
                char[] tmpArr = num.ToString().ToArray();
                StringBuilder sb = new StringBuilder();
                // 逐位转换为中文数字并添加对应的单位
                for (int i = 0; i < tmpArr.Length; i++)
                {
                    sb.Append(strArr[tmpArr[i] - 48]); // ASCII编码 0为48
                    sb.Append(Chinese[tmpArr.Length - 1 - i]); // 根据对应的位数插入对应的单位
                }

                // 处理中文数字字符串中的零和单位组合
                string tmpVal = sb.ToString()
                    .Replace("零万", "万")
                    .Replace("零千", "")
                    .Replace("零百", "")
                    .Replace("零十", "零")
                    .Replace("零零零", "零")
                    .Replace("零零", "零");

                // 特殊情况处理，若开头是"一十"，则简化为"十"
                if (tmpVal.Length >= 2 && tmpVal.Substring(0, 2) == "一十")
                {
                    tmpVal = "十" + tmpVal.Substring(2);
                }

                // 移除字符串末尾多余的零
                while (tmpVal.Last() == '零')
                {
                    tmpVal = tmpVal.Substring(0, tmpVal.Length - 1);
                }

                return tmpVal;

            }
            catch (Exception)
            {
                return num.ToString();
            }
        }
        #endregion

        #region Helper Classes
        public class Replacer
        {
            public string Template { get; set; }
            public Dictionary<string, Func<string>> placeholders;

            public Replacer(string template)
            {
                Template = template;
                placeholders = new Dictionary<string, Func<string>>();
            }

            public void Add(string placeholder, string value)
            {
                placeholders[placeholder] = () => value;
            }

            public void Add(string placeholder, string value, Func<string, string> formatter)
            {
                placeholders[placeholder] = () => formatter(value);
            }

            public void Add(string placeholder, Func<string> valueGenerator)
            {
                placeholders[placeholder] = valueGenerator;
            }

            public string Execute()
            {
                string result = Template;

                // Step 1: 替换占位符
                foreach (var placeholder in placeholders)
                {
                    string pattern = "{" + Regex.Escape(placeholder.Key) + "}";
                    result = Regex.Replace(result, pattern, placeholder.Value());
                }

                // Step 2: 替换 [颜色][文本]
                result = Regex.Replace(result, @"\[(\w+)\](\[[^\]]+\])", match =>
                {
                    string colorName = match.Groups[1].Value;
                    string text = match.Groups[2].Value;

                    if (Enum.TryParse<Colors>(colorName, out var colorEnum) && color.TryGetValue(colorEnum, out string colorCode))
                    {
                        return $"<color=#{colorCode}>{text.Substring(1, text.Length - 2)}</color>";
                    }

                    return match.Value;
                });

                return result;
            }

        }
        #endregion
    }
}
