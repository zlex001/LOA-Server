using System.Collections.Generic;
using System.Linq;
using System;
using Newtonsoft.Json;

namespace Logic.Database
{
    [Serializable]
    public class Warehouse : Basic.Data
    {
        public Warehouse() { }
        public Warehouse(Logic.Warehouse warehouse)
        {
            level = warehouse.Level;
            item = warehouse.Item;
        }

        public int level;
        public Dictionary<int, int> item = new();
        
        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            
            level = Get<int>(dict, "Level");
            
          
        }
        
        public override Dictionary<string, object> ToDictionary
        {
            get
            {
                var dict = new Dictionary<string, object>
                {
                    ["Level"] = level,
                    ["Item"] = JsonConvert.SerializeObject(item),
                };
                return dict;
            }
        }
    }
}
