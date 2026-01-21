using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic
{
    public class Warehouse : Ability
    {
        #region Enums and Properties
        public enum Data
        {
            Level,
            Item,
        }
        
        public enum Event
        {
            PlayerObtained,
        }

        public  int Level { get => data.Get<int>(Data.Level); set => data.Change(Data.Level, value); }
        public Dictionary<int, int> Item { get => data.Get<Dictionary<int, int>>(Data.Item); set => data.Change(Data.Item, value); }
        #endregion

        #region Constructors and Initialization
        public Warehouse()
        {
        }

        public Warehouse(int level)
        {
            data.raw[Data.Level] = level;
            data.raw[Data.Item] = new Dictionary<string, int>();
            monitor.Register(Player.Event.AfterAddAsParent, OnAfterPlayerObtainThis);
        }

        public override void Init(params object[] args)
        {
            Logic.Database.Warehouse database = (Logic.Database.Warehouse)args[0];
            data.raw[Data.Level] = database.level;
            data.raw[Data.Item] = database.item;
        }
        #endregion

        #region Event Handlers
        public bool OnPlayerAddableThis(params object[] args)
        {
            Player player = (Player)args[0];
            return player.Content.Count<Warehouse>() < 10;
        }

        private void OnAfterPlayerObtainThis(params object[] args)
        {
            Player player = (Player)args[0];
            monitor.Fire(Event.PlayerObtained, player, this);
        }
        #endregion
    }
}