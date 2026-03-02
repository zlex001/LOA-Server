using Data;

namespace Logic.Move
{
    public static class Enter
    {
        public static bool Can(Life life, Item container)
        {
            if (life == null || container == null)
                return false;

            if (life.Parent is not Map)
                return false;

            if (!container.Container.TryGetValue("Capacity", out int capacity) || capacity < 10000)
                return false;

            if (!container.Config.Tags.Contains("Jail"))
                return false;

            return true;
        }

        public static void Do(Life life, Item container)
        {
            if (Can(life, container))
            {
                life.Bearer = null;
                container.AddAsParent(life);
                container.monitor.Fire(Item.Event.Enter, life);
                Broadcast.Instance.Local(life, [Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.Enter)], ("sub", life), ("obj", container));
            }
        }
    }
}