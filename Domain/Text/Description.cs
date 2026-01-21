using Logic;

namespace Domain.Text
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
       

        public static Players Player(Logic.Life.Genders gender, double age)
        {
            if (gender == Logic.Life.Genders.Female)
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
        private static Logic.Text.Labels GetAgeLabel(double age)
        {
            if (age < 1) return Logic.Text.Labels.Infant;
            if (age < 7) return Logic.Text.Labels.Child;
            if (age < 15) return Logic.Text.Labels.Adolescent;
            if (age < 30) return Logic.Text.Labels.Young;
            if (age < 60) return Logic.Text.Labels.Adult;
            if (age < 100) return Logic.Text.Labels.Elderly;
            return Logic.Text.Labels.Centenarian;
        }

        private static string GetCategoryText(Logic.Life.Categories category, Player sub)
        {
            var label = category switch
            {
                Logic.Life.Categories.Lemurian => Logic.Text.Labels.Lemurian,
                Logic.Life.Categories.Atlantean => Logic.Text.Labels.Atlantean,
                Logic.Life.Categories.Druk => Logic.Text.Labels.Druk,
                Logic.Life.Categories.Beastman => Logic.Text.Labels.Beastman,
                Logic.Life.Categories.Demon => Logic.Text.Labels.Demon,
                Logic.Life.Categories.Animal => Logic.Text.Labels.Animal,
                _ => Logic.Text.Labels.Lemurian  // 默认值
            };
            return Domain.Text.Agent.Instance.Get(label, sub);
        }




        public static string Life(Life obj, Player sub)
        {
            if (obj == null) return "";
            if (sub == null) return "";

            var template = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.DescriptionTemplate, sub);
            
            var state = obj.State?.CurrentKey ?? Logic.Life.States.Normal;
            string stateText;
            if (state == Logic.Life.States.Unconscious)
            {
                var baseStateText = Domain.Text.Agent.Instance.Get((int)state, sub);
                var remaining = obj.WakeUpTime - DateTime.Now;
                var seconds = Math.Max(0, (int)remaining.TotalSeconds);
                stateText = $"{baseStateText}（{seconds}）";
            }
            else if (state == Logic.Life.States.Normal && obj.CurrentExecutingNode != null)
            {
                stateText = Domain.Text.Agent.Instance.Get(obj.CurrentExecutingNode.Config.Name, sub);
            }
            else
            {
                stateText = Domain.Text.Agent.Instance.Get((int)state, sub);
            }

            var punishment = obj.Content.Get<Logic.Punishment>();
            var crimesText = "";
            if (punishment?.Crimes != null && punishment.Crimes.Count > 0)
            {
                var crimeNames = punishment.Crimes.Select(crime => Domain.Text.Agent.Instance.Get((int)crime, sub)).ToList();
                crimesText = $"（{string.Join("、", crimeNames)}）";
            }

            var categoryText = GetCategoryText(obj.Category, sub);

            var age = (int)(obj.Age / Domain.Time.Agent.Rate);
            string groupText;
            if (obj is Player player)
            {
                groupText = Domain.Text.Agent.Instance.Get((int)Player(player.Gender, (int)(player.Age / Domain.Time.Agent.Rate)), sub);
            }
            else
            {
                var ageLabel = GetAgeLabel(age);
                var ageLabelText = Domain.Text.Agent.Instance.Get(ageLabel, sub);
                var configName = obj.Config?.Name != null 
                    ? Domain.Text.Agent.Instance.Get(obj.Config.Name, sub) 
                    : "";
                groupText = $"{ageLabelText}{configName}";
            }

            var result = Utils.Text.Format(template, "state", stateText, "crimes", crimesText, "category", categoryText, "group", groupText);

            if (obj == sub)
            {
                var ageText = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.Age, sub);
                var ageModifier = Domain.Develop.Agent.GetAgeModifier(obj);
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
