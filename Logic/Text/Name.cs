using Data;

namespace Logic.Text
{
    public static class Name
    {
        public static string Life(Life life, Player observer)
        {
            if (life == null || observer == null) return "";
            
            if (life is global::Data.Player player)
            {
                return player.Name ?? "???";
            }
            else if (life.Config?.Name != null)
            {
                try
                {
                return Agent.Instance.Get(life.Config.Name, observer);
                }
                catch
                {
                    return life.Config.Name.ToString();
                }
            }
            
            return "";
        }
        
        public static string Player(Player player, Player observer)
        {
            if (player == null || observer == null) return "";
            return player.Name ?? "???";
        }
        
        public static string Item(Item item, Player player, int? specificCount = null)
        {
            if (item?.Config == null || player == null) return "";
            
            var name = Agent.Instance.Get(item.Config.Name, player);
            
            int count = specificCount ?? item.Count;
            return count > 1 ? $"{name}×{count}" : name;
        }
        
        public static string Skill(Skill skill, Player player)
        {
            if (skill?.Config == null || player == null) return "";
            
            try
            {
                return Agent.Instance.Get(skill.Config.Name, player);
            }
            catch
            {
                return skill.Config.Name.ToString();
            }
        }
        
        public static string Movement(Movement movement, Player player)
        {
            if (movement?.Config == null || player == null) return "";
            
            try
            {
                string basic = Agent.Instance.Get(movement.Config.description, player);
                return basic;
            }
            catch
            {
                return movement.Config.description.ToString();
            }
        }
        

        
        public static string Map(Map map, Player player)
        {
            if (map?.Config == null || player == null) return "";
            
            try
            {
                return Agent.Instance.Get(map.Config.Name, player);
            }
            catch
            {
                return map.Config.Name.ToString();
            }
        }
    }
}
