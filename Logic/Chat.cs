using Data;

namespace Logic
{
    public class Chat
    {
        private static Chat instance;
        public static Chat Instance { get { if (instance == null) { instance = new Chat(); } return instance; } }

        public void Init()
        {
            global::Data.Agent.Instance.Content.Add.Register(typeof(global::Data.Player), OnAddPlayer);
            global::Data.Agent.Instance.Content.Remove.Register(typeof(global::Data.Player), OnUnbundlePlayer);
        }

        private void OnAddPlayer(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[1];
            player.monitor.Register(global::Data.Player.Event.Chat, OnPlayerChat);
        }

        private void OnUnbundlePlayer(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[1];
            player.monitor.Unregister(global::Data.Player.Event.Chat, OnPlayerChat);
        }

        private void OnPlayerChat(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[0];
            string content = (string)args[1];
            Broadcast.Instance.All(new object[] { "{sub}：{content}" }, ("sub", player), ("content", content));
        }
    }
}
