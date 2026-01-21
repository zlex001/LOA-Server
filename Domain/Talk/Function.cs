using Logic;

namespace Domain.Talk
{
    public class Function : Domain.TagFunction<Function, Logic.Config.Life, Life>
    {
        public override void Init()
        {
            Handlers["Shopkeeper"] = Shopkeeper;
            Handlers["Teleporter"] = Teleporter;
        }

        private static void Shopkeeper(Logic.Config.Life objConfig, Life sub)
        {
            (sub as Player)?.Create<Logic.Option>(Logic.Option.Types.Shop, sub, sub);
        }

        private static void Teleporter(Logic.Config.Life objConfig, Life sub)
        {
            (sub as Player)?.Create<Logic.Option>(Logic.Option.Types.Teleport, sub, sub);
        }
    }
}
