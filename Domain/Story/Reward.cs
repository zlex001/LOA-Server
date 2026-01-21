using Logic;

namespace Domain.Story
{
    public static class Reward
    {
        public static bool Can(Plot plot)
        {
            return plot.Config.rewards?.Count > 0;
        }

        public static void Do(Plot plot, Player player, Ability source = null)
        {
            foreach (var (type, id, amount) in plot.Config.rewards)
            {
                switch (type)
                {
                    case "Item":
                        if (source != null)
                        {
                            var createdItem = source.Load<Logic.Config.Item, Logic.Item>(id, amount);
                            Exchange.Receive.Do(player, createdItem, amount);
                        }
                        break;

                    case "Exp":
                        player.Exp += amount;
                        break;

                case "Skill":
                    if (!player.Content.Has<Skill>(s => s.Config.Id == id))
                    {
                        player.Load<Logic.Config.Skill, Skill>(id, 0, 1);
                    }
                    break;
                }
            }
        }
    }
}
