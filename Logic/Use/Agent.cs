using Data;

namespace Logic.Use
{
    public class Agent
    {
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }

        public void Init()
        {
            Function.Instance.Init();
        }
        
        public bool Can(Life user, global::Data.Item item)
        {
            if (item?.Config?.Tags == null) return false;
            return item.Config.Tags.Any(t => t.StartsWith("Use:"));
        }
        
        public void Do(Life user, global::Data.Item item)
        {
            Broadcast.Instance.Local(user, [Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.Use)], ("sub", user), ("item", item));
            Function.Instance.Do(item, user);
        }
    }
}
