using Data;

namespace Logic.Quest
{
    public static class Reward
    {
        public static bool Can(global::Data.Quest quest)
        {
            return quest.Config.rewards?.Count > 0;
        }

        public static void Do(global::Data.Quest quest, Player player, Ability source = null)
        {
            foreach (var (type, id, amount) in quest.Config.rewards)
            {
                switch (type)
                {
                    case "Item":
                        if (source != null)
                        {
                            var createdItem = source.Load<global::Data.Config.Item, global::Data.Item>(id, amount);
                            Exchange.Receive.Do(player, createdItem, amount);
                        }
                        break;

                    case "Exp":
                        player.Exp += amount;
                        break;

                case "Skill":
                    if (!player.Content.Has<Skill>(s => s.Config.Id == id))
                    {
                        player.Load<global::Data.Config.Skill, Skill>(id, 0, 1);
                    }
                    break;
                }
            }
        }
    }
}
