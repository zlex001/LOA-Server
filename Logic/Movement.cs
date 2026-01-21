using Basic;


namespace Logic
{
    public class Movement : Ability, ITag
    {

        public enum Effect
        {
            None,
            Incise,     // 锐伤（动词：切割、切开）
            Contuse,     // 钝伤（动词：造成挫伤/钝击伤）
            Dodge,      // 闪避
            Block,      // 格挡
            Steal,      // 偷窃
            Charm,      // 魅惑
            Cook,       // 烹饪
            Compound,   //制药
            Alchemize,       // 炼金
            Smith,      // 锻造
            Sew,        // 缝纫
            Cleave,     // 横扫群攻
            MultiHit,   // 多段连击
            Hunt,       // 狩猎

        }

        public enum TargetPattern
        {
            SingleTarget,   // 同一目标
            RandomTarget,   // 随机目标
        }

        public enum DamageModifierType
        {
            None,           // 无修正（每段100%）
            Decay,          // 递减修正
            Ratio,          // 固定比例
            Total,          // 总伤控制
        }

        public Config.Movement Config { get; private set; }
        public HashSet<Effect> Effects { get; private set; } = new();
        public int HitCount { get; set; } = 1;
        public int HitCountMin { get; private set; } = 1;
        public int HitCountMax { get; private set; } = 1;
        public string HitCountFormula { get; private set; } = null;
        public TargetPattern MultiHitTargetPattern { get; private set; } = TargetPattern.SingleTarget;
        public DamageModifierType DamageModifier { get; private set; } = DamageModifierType.None;
        public double DamageModifierValue { get; private set; } = 1.0;
        public int CurrentHitIndex { get; set; } = 0;
        public Dictionary<Part, int> PartHitIndexes { get; private set; } = new Dictionary<Part, int>();
        public int CleaveTargetCount { get; set; } = 3;
        public string CleaveTargetFormula { get; private set; } = null;
        public DamageModifierType CleaveDamageModifier { get; private set; } = DamageModifierType.None;
        public double CleaveDamageModifierValue { get; private set; } = 1.0;
        public System.DateTime LastCastTime { get; set; } = System.DateTime.MinValue;

        public override void Init(params object[] args)
        {
            Config = (Config.Movement)args[0];
            if (!string.IsNullOrEmpty(Config.effects))
            {
                foreach (var effectStr in Config.effects.Split(','))
                {
                    var trimmed = effectStr.Trim();
                    if (trimmed.Contains(':'))
                    {
                        var parts = trimmed.Split(':');
                        if (parts.Length >= 2 && System.Enum.TryParse(parts[0].Trim(), true, out Effect effect))
                        {
                            Effects.Add(effect);
                            
                            if (effect == Effect.MultiHit)
                            {
                                ParseMultiHit(parts);
                            }
                            else if (effect == Effect.Cleave)
                            {
                                ParseCleave(parts);
                            }
                        }
                    }
                    else if (System.Enum.TryParse(trimmed, true, out Effect result) && result != Effect.None)
                    {
                        Effects.Add(result);
                    }
                }
            }
        }

        private void ParseMultiHit(string[] parts)
        {
            int currentIndex = 1;
            
            var hitCountStr = parts[currentIndex].Trim();
            if (hitCountStr.Contains("Level"))
            {
                HitCountFormula = hitCountStr;
                HitCount = 1;
            }
            else if (hitCountStr.Contains('~'))
            {
                var rangeParts = hitCountStr.Split('~');
                if (rangeParts.Length == 2 
                    && int.TryParse(rangeParts[0].Trim(), out int min) 
                    && int.TryParse(rangeParts[1].Trim(), out int max))
                {
                    HitCountMin = min;
                    HitCountMax = max;
                    HitCount = Utils.Random.Range(min, max + 1);
                }
            }
            else if (int.TryParse(hitCountStr, out int count))
            {
                HitCount = count;
                HitCountMin = count;
                HitCountMax = count;
            }
            currentIndex++;
            
            if (parts.Length > currentIndex && System.Enum.TryParse(parts[currentIndex].Trim(), true, out TargetPattern pattern))
            {
                MultiHitTargetPattern = pattern;
                currentIndex++;
            }
            
            if (parts.Length > currentIndex && System.Enum.TryParse(parts[currentIndex].Trim(), true, out DamageModifierType modType))
            {
                DamageModifier = modType;
                currentIndex++;
                
                if (parts.Length > currentIndex && double.TryParse(parts[currentIndex].Trim(), out double modValue))
                {
                    DamageModifierValue = modValue;
                }
            }
        }

        private void ParseCleave(string[] parts)
        {
            if (parts.Length < 2) return;
            
            int currentIndex = 1;
            var targetCountStr = parts[currentIndex].Trim();
            
            if (targetCountStr.Contains("Level"))
            {
                CleaveTargetFormula = targetCountStr;
                CleaveTargetCount = 3;
            }
            else if (targetCountStr.Contains('~'))
            {
                var rangeParts = targetCountStr.Split('~');
                if (rangeParts.Length == 2 
                    && int.TryParse(rangeParts[0].Trim(), out int min) 
                    && int.TryParse(rangeParts[1].Trim(), out int max))
                {
                    CleaveTargetCount = Utils.Random.Range(min, max + 1);
                }
            }
            else if (int.TryParse(targetCountStr, out int count))
            {
                CleaveTargetCount = count;
            }
            currentIndex++;
            
            if (parts.Length > currentIndex && System.Enum.TryParse(parts[currentIndex].Trim(), true, out DamageModifierType modType))
            {
                CleaveDamageModifier = modType;
                currentIndex++;
                
                if (parts.Length > currentIndex && double.TryParse(parts[currentIndex].Trim(), out double modValue))
                {
                    CleaveDamageModifierValue = modValue;
                }
            }
        }

        public int CalculateHitCount(int skillLevel)
        {
            if (string.IsNullOrEmpty(HitCountFormula))
            {
                return HitCount;
            }

            try
            {
                string formula = HitCountFormula.Replace("Level", skillLevel.ToString());
                int result = (int)EvaluateFormula(formula);
                return System.Math.Max(1, result);
            }
            catch
            {
                return HitCount;
            }
        }

        public int CalculateCleaveTargetCount(int skillLevel)
        {
            if (string.IsNullOrEmpty(CleaveTargetFormula))
            {
                return CleaveTargetCount;
            }

            try
            {
                string formula = CleaveTargetFormula.Replace("Level", skillLevel.ToString());
                int result = (int)EvaluateFormula(formula);
                return System.Math.Max(1, result);
            }
            catch
            {
                return CleaveTargetCount;
            }
        }

        private double EvaluateFormula(string formula)
        {
            formula = formula.Trim();
            
            if (formula.Contains('+'))
            {
                var parts = formula.Split('+');
                return EvaluateFormula(parts[0].Trim()) + EvaluateFormula(parts[1].Trim());
            }
            
            if (formula.Contains('-'))
            {
                var parts = formula.Split('-');
                return EvaluateFormula(parts[0].Trim()) - EvaluateFormula(parts[1].Trim());
            }
            
            if (formula.Contains('/'))
            {
                var parts = formula.Split('/');
                return EvaluateFormula(parts[0].Trim()) / EvaluateFormula(parts[1].Trim());
            }
            
            if (formula.Contains('*'))
            {
                var parts = formula.Split('*');
                return EvaluateFormula(parts[0].Trim()) * EvaluateFormula(parts[1].Trim());
            }
            
            if (double.TryParse(formula, out double value))
            {
                return value;
            }
            
            return 0;
        }

        public int GetPartHitIndex(Part part)
        {
            if (part == null) return 0;
            if (!PartHitIndexes.ContainsKey(part))
            {
                PartHitIndexes[part] = 0;
            }
            return PartHitIndexes[part];
        }

        public void IncrementPartHitIndex(Part part)
        {
            if (part == null) return;
            if (!PartHitIndexes.ContainsKey(part))
            {
                PartHitIndexes[part] = 0;
            }
            PartHitIndexes[part]++;
        }

        public void ResetPartHitIndexes()
        {
            PartHitIndexes.Clear();
        }

        public IEnumerable<string> GetTags() => Config?.GetTags() ?? Enumerable.Empty<string>();
    }
}