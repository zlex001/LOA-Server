using Data;

namespace Logic.Use
{
    public class Function : Logic.TagFunction<Function, global::Data.Item, Life>
    {
        public override void Init()
        {
            Handlers["Use:Feed"] = Feed;
            Handlers["Use:Heal"] = Heal;
            Handlers["Use:Refresh"] = Refresh;
        }


        private static void Feed(global::Data.Item item, Life user)
        {
            user.Lp += item.Config.value;
            item.Count--;
            item.monitor.Fire(global::Data.Item.Event.Used, user);
        }
        private static void Heal(global::Data.Item item, Life user) => user.HealHp(item.Config.value);
        private static void Refresh(global::Data.Item item, Life user) => user.Mp += item.Config.value;
    }
}
