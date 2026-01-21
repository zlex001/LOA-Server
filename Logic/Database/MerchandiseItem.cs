using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Logic.Database
{
    [Serializable]
    public class MerchandiseItem : Basic.Data
    {
        public Item Item { get; set; } = new();
        public int[] Price { get; set; } = new int[3];
        public DateTime ListTime { get; set; } = DateTime.Now;
        
        public MerchandiseItem() { }
        
        public MerchandiseItem(Item item, int[] price)
        {
            Item = item;
            Price = price;
            ListTime = DateTime.Now;
        }
        
        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            
            var itemDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(Get<string>(dict, "Item"));
            Item = new Item();
            Item.Init(itemDict);
            
            Price = JsonConvert.DeserializeObject<int[]>(Get<string>(dict, "Price")) ?? new int[3];
            ListTime = DateTime.TryParse(Get<string>(dict, "ListTime"), out var time) ? time : DateTime.Now;
        }
        
        public override Dictionary<string, object> ToDictionary
        {
            get
            {
                return new Dictionary<string, object>
                {
                    ["Item"] = JsonConvert.SerializeObject(Item.ToDictionary),
                    ["Price"] = JsonConvert.SerializeObject(Price),
                    ["ListTime"] = ListTime.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
        }
    }
} 