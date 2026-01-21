using Logic;

namespace Domain
{
    public class Chat
    {
        private static Chat instance;
        public static Chat Instance { get { if (instance == null) { instance = new Chat(); } return instance; } }

        public void Init()
        {
            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Player), OnAddPlayer);
            Logic.Agent.Instance.Content.Remove.Register(typeof(Logic.Player), OnUnbundlePlayer);
        }

        private void OnAddPlayer(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[1];
            player.monitor.Register(Logic.Player.Event.Chat, OnPlayerChat);
        }

        private void OnUnbundlePlayer(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[1];
            player.monitor.Unregister(Logic.Player.Event.Chat, OnPlayerChat);
        }

        private void OnPlayerChat(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[0];
            string content = (string)args[1];
            Broadcast.Instance.All(new object[] { "{sub}：{content}" }, ("sub", player), ("content", content));
        }
    }
}
