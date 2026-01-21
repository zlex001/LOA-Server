using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    public static class Mathematics
    {
        private static readonly System.Random random = new System.Random();
        public const int BASE_CRITICAL_RATE = 0;
        public const int CRITICAL_FIX = 5;
        public static readonly List<int> OPVP_RANK_SCORE_RANGE = new List<int> { 0, 20, 40, 80, 160, 350, 650, 1350, 2700, 5500, 10000 };
        public static float ArithmeticSum(int n) => n * (n + 1) / 2f;
        public static Dictionary<string, float> DescendingWeight(IList<string> items, float totalValue)
        {
            var result = new Dictionary<string, float>();
            if (items == null || items.Count == 0) return result;

            int count = items.Count;
            float totalWeight = ArithmeticSum(count);

            for (int i = 0; i < count; i++)
            {
                float weight = count - i;
                float value = (weight / totalWeight) * totalValue;
                result[items[i]] = value;
            }

            return result;
        }
        public static int IntervalSearch(List<int> intervals, int goal)
        {
            List<int> ds = intervals.Select(z => System.Math.Abs(goal - z)).ToList();
            int min = ds.Min();
            int id = ds.IndexOf(min);
            return goal - intervals[id] >= 0 ? id : id - 1;
        }
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
            {
                return min;
            }
            else if (value.CompareTo(max) > 0)
            {
                return max;
            }
            else
            {
                return value;
            }
        }
        public static double Floatation(double value, double percentage)
        {
            double minPercentage = 1 - (percentage / 100.0);
            double maxPercentage = 1 + (percentage / 100.0);
            double minValue = value * minPercentage;
            double maxValue = value * maxPercentage;
            double randomValue = random.NextDouble() * (maxValue - minValue) + minValue;
            return randomValue;
        }

        public static int Level(double exp)
        {
            // 根据累计经验计算等级，使用四次方系统：n级所需经验 = n⁴
            if (exp <= 0) return 1;
            
            // 反向计算：从 exp = n⁴ 得到 n = exp^(1/4)
            int level = (int)System.Math.Pow(exp, 1.0 / 4.0);
            
            // 向上查找确切等级（处理浮点精度问题）
            while (level < 100 && System.Math.Pow(level + 1, 4) <= exp)
            {
                level++;
            }
            
            return Clamp(level, 1, 100);
        }
        public static double Limit(double x, double f = 1)
        {
            return x / (x + f);
        }
        public static double Ratio(dynamic a, dynamic b, double f = 1)
        {
            return Limit((double)a / (double)b, f);
        }
        public static int[] Exps(double exp)
        {
            int level = Level(exp);
            
            // 当前等级的经验门槛 = level⁴
            double expLimit = System.Math.Pow(level, 4);
            
            // 当前等级内的剩余经验
            double expRemain = exp - expLimit;
            
            // 升到下一级还需要的经验
            double nextExp = NeedExp(exp);
            
            return new int[] { (int)expRemain, (int)nextExp };
        }
        public static double NeedExp(double exp)
        {
            int level = Level(exp);
            if (level >= 100)
            {
                return 0;
            }
            else
            {
                int next = level + 1;
                double nextLevelExp = System.Math.Pow(next, 4);
                return nextLevelExp - exp;
            }
        }
        public static bool VersionAdapt(int[] clientVersion, int[] serverVersion)
        {
            for (int i = 0; i < 3; i++)
            {
                if (clientVersion[i] < serverVersion[i])
                {
                    return false;
                }
                else if (clientVersion[i] > serverVersion[i])
                {
                    return true;
                }
            }
            return true;
        }
        public static int EuclideanDistance(int[] start, int[] destination)
        {
            int result = int.MaxValue;
            if (start != null && destination != null && start.Length >= 2 && destination.Length >= 2)
            {
                int dx = start[0] - destination[0];
                int dy = start[1] - destination[1];
                int dz = (start.Length > 2 && destination.Length > 2) ? start[2] - destination[2] : 0;
                result = (int)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
            return result;
        }
        public static bool Probability(int numerator, int denominator)
        {
            if (denominator <= 0)
            {
                return true;
            }
            else if (numerator <= 0)
            {
                return false;
            }
            else
            {
                double probability = (double)numerator / denominator;
                double randomValue = random.NextDouble();
                return randomValue < probability;
            }
        }

        public static bool Probability(double probability)
        {
            if (probability <= 0)
            {
                return false;
            }
            else if (probability >= 1)
            {
                return true;
            }
            else
            {
                return random.NextDouble() < probability;
            }
        }

        public static bool Probability(int percentage)
        {
            return Probability(percentage, 100);
        }

        public static int AsInt(object value)
        {
            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is double d) return (int)d;
            if (value is float f) return (int)f;
            return 0;
        }
        public static double Gaussian(double x, double center = 0, double width = 1, double amplitude = double.NaN)
        {
            if (double.IsNaN(amplitude))
            {
                amplitude = 1.0 / Math.Sqrt(2.0 * Math.PI);
            }

            return amplitude * Math.Exp(-Math.Pow(x - center, 2) / (2.0 * width * width));
        }

    }
}
