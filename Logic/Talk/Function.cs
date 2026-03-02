using Data;

namespace Logic.Talk
{
    public class Function : Logic.TagFunction<Function, global::Data.Config.Life, Life>
    {
        public override void Init()
        {
            Handlers["Shopkeeper"] = Shopkeeper;
            Handlers["Teleporter"] = Teleporter;
        }

        private static void Shopkeeper(global::Data.Config.Life objConfig, Life sub)
        {
            (sub as Player)?.Create<global::Data.Option>(global::Data.Option.Types.Shop, sub, sub);
        }

        private static void Teleporter(global::Data.Config.Life objConfig, Life sub)
        {
            (sub as Player)?.Create<global::Data.Option>(global::Data.Option.Types.Teleport, sub, sub);
        }
    }
}
