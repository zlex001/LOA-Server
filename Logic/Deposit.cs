using Basic;
using Data;
using Utils;

namespace Logic
{
    public class Deposit
    {
        private static Deposit instance;
        public static Deposit Instance { get { if (instance == null) { instance = new Deposit(); } return instance; } }

        public void Init()
        {
            global::Data.Agent.Instance.Content.Add.Register(typeof(Player), OnAddPlayer);
        }

        private void OnAddPlayer(params object[] args)
        {
            var player = (Player)args[1];
            var warehouses = player.Database.warehouses;
            if (warehouses != null && warehouses.Count > 0)
            {
                foreach (var dbWarehouse in warehouses)
                {
                    player.Create<Warehouse>(dbWarehouse);
                }
            }
        }

    }
}
