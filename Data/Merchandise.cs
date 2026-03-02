using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;
using Data.Database;

namespace Data
{
    public partial class Merchandise : Ability
    {
        public enum Event
        {
            AfterObtain,
        }
        public Merchandise()
        {

            Content.Add.Register(typeof(Item), OnContentAddItem);
            Content.Remove.Register(typeof(Item), OnContentRemoveItem);
        }
        public Database.MerchandiseItem database { get; private set; }
        public string SupplierId { get; private set; }
        public Item Item { get; private set; }
        public override void Init(params object[] args)
        {
            database = (Database.MerchandiseItem)args[0];
            SupplierId = (string)args[1];

            Item = Load<Config.Item, Item>(database.Item.Id, database.Item.Count, database.Item.Properties);
        }
        private void OnContentAddItem(params object[] args)
        {
            Basic.Element obj = (Basic.Element)args[0];
            obj.monitor.Fire(Event.AfterObtain, this);
        }
        private void OnContentRemoveItem(params object[] args)
        {
            global::Data.Item item = (global::Data.Item)args[0];
            item.Destroy();
        }


    }
}
