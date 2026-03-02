using Data;

namespace Logic.Text
{
    public static class Description
    {

        public enum Players
        {
            Female0 = 5000,
            Female1 = 5001,
            Female7 = 5002,
            Female15 = 5003,
            Female30 = 5004,
            Female60 = 5005,
            Female100 = 5006,

            Male0 = 6000,
            Male1 = 6001,
            Male7 = 6002,
            Male15 = 6003,
            Male30 = 6004,
            Male60 = 6005,
            Male100 = 6006,
        }
       

        public static Players Player(global::Data.Life.Genders gender, double age)
        {
            if (gender == global::Data.Life.Genders.Female)
            {
                if (age < 1) return Players.Female0;
                if (age < 7) return Players.Female1;
                if (age < 15) return Players.Female7;
                if (age < 30) return Players.Female15;
                if (age < 60) return Players.Female30;
                if (age < 100) return Players.Female60;
                return Players.Female100;
            }
            else
            {
                if (age < 1) return Players.Male0;
                if (age < 7) return Players.Male1;
                if (age < 15) return Players.Male7;
                if (age < 30) return Players.Male15;
                if (age < 60) return Players.Male30;
                if (age < 100) return Players.Male60;
                return Players.Male100;
            }
        }

        /// <summary>
        /// 根据年龄获取年龄标签
        /// </summary>
        private static global::Data.Text.Labels GetAgeLabel(double age)
        {
            if (age < 1) return global::Data.Text.Labels.Infant;
            if (age < 7) return global::Data.Text.Labels.Child;
            if (age < 15) return global::Data.Text.Labels.Adolescent;
            if (age < 30) return global::Data.Text.Labels.Young;
            if (age < 60) return global::Data.Text.Labels.Adult;
            if (age < 100) return global::Data.Text.Labels.Elderly;
            return global::Data.Text.Labels.Centenarian;
        }

        private static string GetCategoryText(global::Data.Life.Categories category, Player sub)
        {
            var label = category switch
            {
                global::Data.Life.Categories.Lemurian => global::Data.Text.Labels.Lemurian,
                global::Data.Life.Categories.Atlantean => global::Data.Text.Labels.Atlantean,
                global::Data.Life.Categories.Druk => global::Data.Text.Labels.Druk,
                global::Data.Life.Categories.Beastman => global::Data.Text.Labels.Beastman,
                global::Data.Life.Categories.Demon => global::Data.Text.Labels.Demon,
                global::Data.Life.Categories.Animal => global::Data.Text.Labels.Animal,
                _ => global::Data.Text.Labels.Lemurian  // 默认值
            };
            return Logic.Text.Agent.Instance.Get(label, sub);
        }




        public static string Life(Life obj, Player sub)
        {
            if (obj == null) return "";
            if (sub == null) return "";

            var template = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.DescriptionTemplate, sub);
            
            var state = obj.State?.CurrentKey ?? global::Data.Life.States.Normal;
            string stateText;
            if (state == global::Data.Life.States.Unconscious)
            {
                var baseStateText = Logic.Text.Agent.Instance.Get((int)state, sub);
                var remaining = obj.WakeUpTime - DateTime.Now;
                var seconds = Math.Max(0, (int)remaining.TotalSeconds);
                stateText = $"{baseStateText}（{seconds}）";
            }
            else if (state == global::Data.Life.States.Normal && obj.CurrentExecutingNode != null)
            {
                stateText = Logic.Text.Agent.Instance.Get(obj.CurrentExecutingNode.Config.Name, sub);
            }
            else
            {
                stateText = Logic.Text.Agent.Instance.Get((int)state, sub);
            }

            var punishment = obj.Content.Get<global::Data.Punishment>();
            var crimesText = "";
            if (punishment?.Crimes != null && punishment.Crimes.Count > 0)
            {
                var crimeNames = punishment.Crimes.Select(crime => Logic.Text.Agent.Instance.Get((int)crime, sub)).ToList();
                crimesText = $"（{string.Join("、", crimeNames)}）";
            }

            var categoryText = GetCategoryText(obj.Category, sub);

            var age = (int)(obj.Age / Logic.Time.Agent.Rate);
            string groupText;
            if (obj is Player player)
            {
                groupText = Logic.Text.Agent.Instance.Get((int)Player(player.Gender, (int)(player.Age / Logic.Time.Agent.Rate)), sub);
            }
            else
            {
                var ageLabel = GetAgeLabel(age);
                var ageLabelText = Logic.Text.Agent.Instance.Get(ageLabel, sub);
                var configName = obj.Config?.Name != null 
                    ? Logic.Text.Agent.Instance.Get(obj.Config.Name, sub) 
                    : "";
                groupText = $"{ageLabelText}{configName}";
            }

            var result = Utils.Text.Format(template, "state", stateText, "crimes", crimesText, "category", categoryText, "group", groupText);

            if (obj == sub)
            {
                var ageText = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.Age, sub);
                var ageModifier = Logic.Develop.Agent.GetAgeModifier(obj);
                var ageColor = ageModifier < 1.0 ? Utils.Text.Colors.Error : Utils.Text.Colors.Success;
                var ageDisplay = Utils.Text.Color(ageColor, $"[{age}{ageText}]");
                result = $"{ageDisplay}{result}";
            }

            return result;
        }

        public static string Item(Item item, Player player)
        {
            if (item?.Config == null || player == null) return "";

            try
            {
                return Agent.Instance.Get(item.Config.Description, player);
            }
            catch
            {
                return item.Config.Description.ToString();
            }
        }

        public static string Skill(Skill skill, Player player)
        {
            if (skill?.Config == null || player == null) return "";

            // Skill的描述通过text字典获取
            if (skill.Config.text != null && skill.Config.text.TryGetValue("Description", out var descriptions) && descriptions.Any())
            {
                return descriptions.First();
            }

            return "";
        }

        public static string Movement(Movement movement, Player player)
        {
            if (movement?.Config == null || player == null) return "";

            try
            {
                return Agent.Instance.Get(movement.Config.description, player);
            }
            catch
            {
                return movement.Config.description.ToString();
            }
        }
    }
}
