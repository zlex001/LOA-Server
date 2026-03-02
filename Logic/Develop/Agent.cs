using Data;
using Utils;

namespace Logic.Develop
{
    public class Agent
    {
        public static void Init()
        {
            Upgrade.Init();
            global::Data.Agent.Instance.Content.Add.Register(typeof(Life), OnAddLife);
        }

       

        private static void OnLifeAgeChanged(params object[] args)
        {
            Life life = (Life)args[0];
            UpdateAttributes(life);
        }
        private static void OnAddLife(params object[] args)
        {
            Life life = (Life)args[1];
            UpdateAttributes(life);
            if (life is not global::Data.Player)
            {
                foreach (Part part in life.Content.Gets<Part>())
                {
                    part.data.Full<int>(Part.Data.Hp);
                }
                life.data.Full<int>(Life.Data.Mp);
                life.data.Full<double>(Life.Data.Lp);
            }
            else if (life.Hand != null)
            {
                life.Hand.Content.Add.Register(typeof(Item), (args) => OnPlayerHandContentChanged(life));
                life.Hand.Content.Remove.Register(typeof(Item), (args) => OnPlayerHandContentChanged(life));
            }
            // Register equipment change listeners for ViewScale updates
            foreach (Part part in life.Content.Gets<Part>())
            {
                part.Content.Add.Register(typeof(Item), (args) => OnEquipmentChanged(life));
                part.Content.Remove.Register(typeof(Item), (args) => OnEquipmentChanged(life));
            }
            life.monitor.Register(Life.Event.AgeChanged, OnLifeAgeChanged);
        }

        private static void OnPlayerHandContentChanged(Life life)
        {
            UpdateViewScale(life);
            if (life is Player player && player.Map != null)
            {
                Net.Tcp.Instance.Send(player, new Net.Protocol.Pos(player.Map.Database.pos, Move.Walk.Area(player)));
            }
        }

        private static void OnEquipmentChanged(Life life)
        {
            UpdateViewScale(life);
        }
        public static double GetAgeModifier(Life life)
        {
            var realAge = life.Age / Logic.Time.Agent.Rate;
            return Utils.Mathematics.Gaussian(realAge, 15, 12, 2);
        }

        public static void UpdateAttributes(Life life)
        {
            var ageModifier = GetAgeModifier(life);
            
            SetPartsMaxHp(life, ageModifier);
            life.MaxMp = Mathematics.Instance.AttributeValue(life.Grade, Life.Attributes.Mp, life.Level);
            life.data.raw[Life.Data.Atk] = Mathematics.Instance.AttributeValue(life.Grade, Life.Attributes.Atk, life.Level) * ageModifier;
            life.data.raw[Life.Data.Def] = Mathematics.Instance.AttributeValue(life.Grade, Life.Attributes.Def, life.Level) * ageModifier;
            life.data.raw[Life.Data.Agi] = Mathematics.Instance.AttributeValue(life.Grade, Life.Attributes.Agi, life.Level) * ageModifier;
            life.data.raw[Life.Data.Ine] = Mathematics.Instance.AttributeValue(life.Grade, Life.Attributes.Ine, life.Level) * ageModifier;
            life.data.raw[Life.Data.Con] = Mathematics.Instance.AttributeValue(life.Grade, Life.Attributes.Con, life.Level) * ageModifier;
            double totalAgi = life.Agi;
            double ratio = Utils.Mathematics.Ratio(totalAgi, 2000);
            life.WalkScale = Math.Max(1, ratio * 10);
            life.ViewScale = CalculateViewScale(life);
        }

        private static double CalculateViewScale(Life life)
        {
            double baseValue = global::Data.Constant.BaseViewScale;
            double bonus = GetEquipmentViewBonus(life);
            return baseValue + bonus;
        }

        private static double GetEquipmentViewBonus(Life life)
        {
            double totalBonus = 0;
            // Check all equipped items for ViewScale bonus
            foreach (var part in life.Content.Gets<Part>())
            {
                var equippedItem = part.Content.Get<Item>();
                if (equippedItem?.Config?.Tags == null) continue;
                
                // Check if item is equipped in its correct slot (EquipPart must match Part.Type)
                var equipPartValue = equippedItem.Config.Tags.GetValue("EquipPart");
                if (string.IsNullOrEmpty(equipPartValue) || equipPartValue != part.Type.ToString())
                {
                    continue; // Item not in its designated slot, skip
                }
                
                var viewValue = equippedItem.Config.Tags.GetValue("ViewScale");
                if (!string.IsNullOrEmpty(viewValue) && double.TryParse(viewValue, out var bonus))
                {
                    totalBonus += bonus;
                }
            }
            return totalBonus;
        }

        private static void UpdateViewScale(Life life)
        {
            double oldViewScale = life.ViewScale;
            life.ViewScale = CalculateViewScale(life);
            
            // If ViewScale changed, invalidate perception cache and update client
            if (Math.Abs(life.ViewScale - oldViewScale) > 0.01)
            {
                Perception.Agent.Instance.InvalidateCache(life);
                
                // If player, send updated data to client
                if (life is Player player && player.Map != null)
                {
                    // Send updated characters list
                    var data = Display.Agent.GetCharactersForDisplay(player);
                    Net.Tcp.Instance.Send(player, new Net.Protocol.Characters(data));
                    
                    // Send updated map highlight (Pos protocol with Area)
                    Net.Tcp.Instance.Send(player, new Net.Protocol.Pos(player.Map.Database.pos, Move.Walk.Area(player)));
                }
            }
        }
        public static void SetPartsMaxHp(Life life, double ageModifier = 1.0)
        {
            double maxHp = Mathematics.Instance.AttributeValue(life.Grade, Life.Attributes.Hp, life.Level) * ageModifier;
            List<Part> parts = life.Content.Gets<Part>();
            var weights = parts.Select(p => p.GetHpWeight()).ToList();
            var baseValues = weights.Select(w => (int)Math.Floor(maxHp * w)).ToList();
            int sumBase = baseValues.Sum();
            int remainder = (int)maxHp - sumBase;
            for (int i = 0; i < parts.Count; i++)
            {
                int hp = baseValues[i] + (i < remainder ? 1 : 0);
                hp = Math.Max(1, hp);
                parts[i].data.raw[Part.Data.MaxHp] = hp;
                parts[i].Hp = Math.Min(parts[i].Hp, hp);
            }
        }
    }
}
