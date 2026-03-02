using Data;

namespace Logic.Companion
{
    public class Agent : Logic.Agent<Agent>
    {
        public static void Init()
        {
            global::Data.Agent.Instance.Content.Remove.Register(typeof(global::Data.Life), OnRemoveLife);
        }

        private static void OnRemoveLife(params object[] args)
        {
            global::Data.Life life = args[1] as global::Data.Life;
            
            if (life == null || life.Leader == null) return;
            if (!(life.Leader is Player player)) return;

            var companion = player.Database.companions.FirstOrDefault(c => 
                c.LifeConfigId == life.Config.Id && c.Level == life.Level);
            
            if (companion != null)
            {
                player.Database.companions.Remove(companion);
                Broadcast.Instance.Local(player, [Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.CompanionDeath)], ("obj", life));
            }
        }
    }
}

