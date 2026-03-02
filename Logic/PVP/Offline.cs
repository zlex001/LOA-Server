using Data;

namespace Logic.PVP
{
    public class Offline
    {
        private static Offline instance;
        public static Offline Instance { get { if (instance == null) { instance = new Offline(); } return instance; } }

        public void Init()
        {
            global::Data.Agent.Instance.Content.Add.Register(typeof(global::Data.Player), OnAddPlayer);
            global::Data.Agent.Instance.Content.Remove.Register(typeof(global::Data.Player), OnUnbundlePlayer);
        }

        private void OnAddPlayer(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[1];
            player.monitor.Register(global::Data.Player.Event.OpvpScoreChanged, OnPlayerOpvpScoreChanged);
            player.monitor.Register(global::Data.Player.Event.ArenaPayOut, OnPlayerArenaPayOut);
        }

        private void OnUnbundlePlayer(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[1];
            player.monitor.Unregister(global::Data.Player.Event.OpvpScoreChanged, OnPlayerOpvpScoreChanged);
            player.monitor.Unregister(global::Data.Player.Event.ArenaPayOut, OnPlayerArenaPayOut);
        }

        private void OnPlayerOpvpScoreChanged(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[0];
            int o = (int)args[1];
            int v = (int)args[2];
            int d = v - o;
            string sign = d >= 0 ? "！" : "。";
            int o_rank = Utils.Mathematics.IntervalSearch(Utils.Mathematics.OPVP_RANK_SCORE_RANGE, o);
int v_rank = Utils.Mathematics.IntervalSearch(Utils.Mathematics.OPVP_RANK_SCORE_RANGE, v);
            int d_rank = v_rank - o_rank;
            string sign_rank = d_rank >= 0 ? "！" : "。";
            Broadcast.Instance.System(player, new object[] { Utils.Text.Color(Utils.Text.Colors.Success, $"比武积分{(d > 0 ? "+" : "")}{d}，当前比武积分总计：{v}[{Utils.Text.Chinese(v_rank)}段]{sign}") });
            if (d_rank != 0)
            {
                Broadcast.Instance.System(player, new object[] { Utils.Text.Color(Utils.Text.Colors.Quality6, $"比武积分段位{(d_rank > 0 ? "+" : "")}{d_rank}，当前比武段位：{Utils.Text.Chinese(v_rank)}段{sign_rank}") });
            }
        }

        private void OnPlayerArenaPayOut(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[0];
            Broadcast.Instance.Local(player, new object[] { "$N竞技场赔付。" });
        }


    }
}