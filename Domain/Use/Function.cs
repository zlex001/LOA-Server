using Logic;

namespace Domain.Use
{
    public class Function : Domain.TagFunction<Function, Logic.Item, Life>
    {
        public override void Init()
        {
            Handlers["Use:Feed"] = Feed;
            Handlers["Use:Heal"] = Heal;
            Handlers["Use:Refresh"] = Refresh;
        }


        private static void Feed(Logic.Item item, Life user)
        {
            user.Lp += item.Config.value;
            item.Count--;
            item.monitor.Fire(Logic.Item.Event.Used, user);
        }
        private static void Heal(Logic.Item item, Life user) => user.HealHp(item.Config.value);
        private static void Refresh(Logic.Item item, Life user) => user.Mp += item.Config.value;
    }
}
