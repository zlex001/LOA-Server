using Logic;

namespace Domain.Companion
{
    public class Agent : Domain.Agent<Agent>
    {
        public static void Init()
        {
            Logic.Agent.Instance.Content.Remove.Register(typeof(Logic.Life), OnRemoveLife);
        }

        private static void OnRemoveLife(params object[] args)
        {
            Logic.Life life = args[1] as Logic.Life;
            
            if (life == null || life.Leader == null) return;
            if (!(life.Leader is Player player)) return;

            var companion = player.Database.companions.FirstOrDefault(c => 
                c.LifeConfigId == life.Config.Id && c.Level == life.Level);
            
            if (companion != null)
            {
                player.Database.companions.Remove(companion);
                Broadcast.Instance.Local(player, [Domain.Text.Agent.Instance.Id(Logic.Text.Labels.CompanionDeath)], ("obj", life));
            }
        }
    }
}

