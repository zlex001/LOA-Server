using System;
using System.Collections.Generic;

namespace Data.Database
{
    public class Card : Basic.Data
    {
        public string Cid { get; set; }
        public int value;
        public DateTime utilized;
        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Cid = Get<string>(dict, "id");
            value = Get<int>(dict, "value");
            utilized = Get<DateTime>(dict, "utilized");
        }
        public override Dictionary<string, object> ToDictionary
        {
            get
            {
                var dict = new Dictionary<string, object>
                {
                    ["id"] = Cid,
                    ["value"] = value,
                    ["utilized"] = utilized,
                };
                return dict;
            }
        }
    }
}

