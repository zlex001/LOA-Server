using System;
using System.Collections.Generic;
using System.Linq;

namespace Data
{
    public class Hostel : Copy
    {
        #region Enums and Properties
        public enum Data
        {
            Occupant,
            Warehouse,
        }

        public enum Event
        {
            AfterAdd,
            AfterRemove,
        }

        public Player Occupant => data.Get<Player>(Data.Occupant);
        public Dictionary<Item, Warehouse> Warehouse { get => data.Get<Dictionary<Item, Warehouse>>(Data.Warehouse); set => data.Change(Data.Warehouse, value); }
        #endregion

        #region Constructor
        public Hostel()
        {
            data.raw[Data.Warehouse] = new Dictionary<Item, Warehouse>();
            Content.Add.Register(typeof(Player), OnContentAddPlayer);
            Content.Remove.Register(typeof(Player), OnContentRemovePlayer);
        }
        #endregion

        #region Event Handlers
        private void OnContentAddPlayer(params object[] args)
        {
            Ability obj = (Ability)args[0];
            obj.monitor.Fire(Event.AfterAdd, this);
        }

        private void OnContentRemovePlayer(params object[] args)
        {
            Ability obj = (Ability)args[0];
            obj.monitor.Fire(Event.AfterRemove, this);
        }
        #endregion
    }
} 