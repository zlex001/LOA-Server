using Basic;
using Logic;
using Utils;

namespace Domain
{
    public class Deposit
    {
        private static Deposit instance;
        public static Deposit Instance { get { if (instance == null) { instance = new Deposit(); } return instance; } }

        public void Init()
        {
            Logic.Agent.Instance.Content.Add.Register(typeof(Player), OnAddPlayer);
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
