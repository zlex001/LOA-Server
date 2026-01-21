using System;
using System.Collections.Generic;
using System.Linq;
using Logic;

namespace Domain
{
    public class Mathematics
    {
        private static Mathematics instance;
        public static Mathematics Instance { get { if (instance == null) { instance = new Mathematics(); } return instance; } }

        // 属性基础值字典
        private static readonly Dictionary<Life.Attributes, double> BASE_ATTRIBUTE_VALUES = new Dictionary<Life.Attributes, double>
        {
            { Life.Attributes.Hp, 20 },
            { Life.Attributes.Mp, 20 },
            { Life.Attributes.Atk, 20 },
            { Life.Attributes.Def, 20 },
            { Life.Attributes.Agi, 20 },
            { Life.Attributes.Con, 100 },
            { Life.Attributes.Ine, 100 }
        };

        private static readonly Dictionary<Life.Attributes, Dictionary<Life.Attributes, double>> ABILITY_ATTRIBUTE = new Dictionary<Life.Attributes, Dictionary<Life.Attributes, double>>
        {
            {
                Life.Attributes.Hp, new Dictionary<Life.Attributes, double>
                {
                    { Life.Attributes.Hp, 8.0 },
                    { Life.Attributes.Mp, 1.0 },
                    { Life.Attributes.Atk, 0.2 },
                    { Life.Attributes.Def, 0.2 },
                    { Life.Attributes.Agi, 0.1 },
                    { Life.Attributes.Ine, -0.3 },
                    { Life.Attributes.Con, 0.8 }
                }
            },
            {
                Life.Attributes.Atk, new Dictionary<Life.Attributes, double>
                {
                    { Life.Attributes.Hp, 2.0 },
                    { Life.Attributes.Mp, 2.0 },
                    { Life.Attributes.Atk,2.7 },
                    { Life.Attributes.Def, 0.3 },
                    { Life.Attributes.Agi, 0.2 },
                    { Life.Attributes.Ine, -0.1 },
                    { Life.Attributes.Con, -0.1 }
                }
            },
            {
                Life.Attributes.Def, new Dictionary<Life.Attributes, double>
                {
                    { Life.Attributes.Hp, 3.0 },
                    { Life.Attributes.Mp, 2.0 },
                    { Life.Attributes.Atk,0.3 },
                    { Life.Attributes.Def, 3.0 },
                    { Life.Attributes.Agi, 0.2 },
                    { Life.Attributes.Ine, 0.2 },
                    { Life.Attributes.Con, -0.1 }
                }
            },
            {
                Life.Attributes.Agi, new Dictionary<Life.Attributes, double>
                {
                    { Life.Attributes.Hp, 3.0 },
                    { Life.Attributes.Mp, 2.0 },
                    { Life.Attributes.Atk,0.3 },
                    { Life.Attributes.Def, 0.3 },
                    { Life.Attributes.Agi, 2.0 },
                    { Life.Attributes.Ine, -0.1 },
                    { Life.Attributes.Con, 0.2 }
                }
            },
            {
                Life.Attributes.Mp, new Dictionary<Life.Attributes, double>
                {
                    { Life.Attributes.Hp, 1.0 },
                    { Life.Attributes.Mp, 10.0 },
                    { Life.Attributes.Atk,0.2 },
                    { Life.Attributes.Def, 0.2 },
                    { Life.Attributes.Agi, 0.1 },
                    { Life.Attributes.Ine, 0.8 },
                    { Life.Attributes.Con, -0.3 }
                }
            }

        };

        public Dictionary<Life.Attributes, double> AttributePoint(Dictionary<Life.Attributes, int> grade, int level)
        {
            Dictionary<Life.Attributes, double> final = new Dictionary<Life.Attributes, double>();
            double[] increments = [40, 40, 40, 45, 45];

            foreach (var g in grade)
            {
                double basic = g.Value * 0.2;
                double accumulation = 40;
                for (int i = 1; i < g.Value; i++)
                {
                    accumulation += increments[(i - 1) % 5];
                }
                accumulation /= 1000;
                final[g.Key] = basic + accumulation * (level - 1);
            }
            return final;
        }

        public double AttributeValue(Dictionary<Life.Attributes, int> grade, Life.Attributes attribute, int level)
        {
            var point = AttributePoint(grade, level);
            double basic = BASE_ATTRIBUTE_VALUES[attribute];
            return basic + point.Sum(p => p.Value * ABILITY_ATTRIBUTE[p.Key][attribute]);
        }

        public double GetMaxCarry(Life life)
        {
            var attributePoints = AttributePoint(life.Grade, life.Level);
            return attributePoints.Sum(p => p.Value) * 1000;
        }


    }
}
