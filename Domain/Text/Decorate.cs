using Logic;

namespace Domain.Text
{
    public static class Decorate
    {
        public static string Life(Life life, Player observer)
        {
            if (life == null || observer == null) return "";
            
            var color = GetLifeHateColor(life, observer);
            string name = Name.Life(life, observer);
            
            return Utils.Text.Color(color, $"「{name}」");
        }
        
        public static string Player(Player player, Player observer)
        {
            if (player == null || observer == null) return "";
            
            var name = player.Name ?? "???";
            
            return Utils.Text.Color(Utils.Text.Colors.EntityPlayer, name);
        }
        
        public static string Item(Item item, Player player, int? specificCount = null)
        {
            if (item?.Config == null || player == null) return "";
            
            var color = GetItemValueColor(item.Config.value);
            var name = Agent.Instance.Get(item.Config.Name, player);
            
            int count = specificCount ?? item.Count;
            var displayName = count > 1 ? $"{name}×{count}" : name;
            
            return Utils.Text.Color(color, $"【{displayName}】");
        }
        
        public static string Skill(Skill skill, Player player)
        {
            if (skill?.Config == null || player == null) return "";
            
            var name = Name.Skill(skill, player);
            
            return name;
        }
        
        public static string Movement(Movement movement, Player player)
        {
            if (movement?.Config == null || player == null) return "";
            
            var name = Name.Movement(movement, player);
            
            return Utils.Text.Color(Utils.Text.Colors.EntityMovement, $"〖{name}〗");
        }
        
        public static string Part(Part part, Player player)
        {
            if (part == null || player == null) return "";
            
            var name = Domain.Text.Agent.Instance.Get((int)part.Type, player);
            
            return Utils.Text.Color(Utils.Text.Colors.EntityItem, $"〈{name}〉");
        }
        
        public static string Map(Map map, Player player)
        {
            if (map?.Config == null || player == null) return "";
            
            var name = Name.Map(map, player);
            
            return Utils.Text.Color(Utils.Text.Colors.EntityMap, $"《{name}》");
        }
        
        public static Utils.Text.Colors GetLifeColor(Life life, Player observer)
        {
            return GetLifeHateColor(life, observer);
        }
        
        private static Utils.Text.Colors GetLifeHateColor(Life life, Player observer)
        {
            if (life == null || observer == null) return Utils.Text.Colors.EntityNPC;
            
            double hate = observer.Relation.TryGetValue(life, out var value) ? value : 0;
            return hate switch
            {
                0 => Utils.Text.Colors.EntityNPC,
                > 0 and <= 5 => Utils.Text.Colors.Quality1,
                > 5 and <= 15 => Utils.Text.Colors.EntityMovement,
                > 15 and <= 50 => Utils.Text.Colors.Quality5,
                _ => Utils.Text.Colors.DamageCritical
            };
        }

        public static Utils.Text.Colors GetItemColor(Item item)
        {
            if (item?.Config == null) return Utils.Text.Colors.EntityItem;
            
            return GetItemValueColor(item.Config.value);
        }

        public static Utils.Text.Colors GetItemValueColor(int value)
        {
            return value switch
            {
                <= 10 => Utils.Text.Colors.Quality0,
                <= 50 => Utils.Text.Colors.Quality1,
                <= 200 => Utils.Text.Colors.Quality2,
                <= 500 => Utils.Text.Colors.Quality3,
                <= 1000 => Utils.Text.Colors.Quality4,
                <= 2000 => Utils.Text.Colors.Quality5,
                _ => Utils.Text.Colors.Quality6
            };
        }
    }
}
