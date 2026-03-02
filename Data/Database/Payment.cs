using System;
using System.Collections.Generic;

namespace Data.Database
{
    [Serializable]
    public class Payment : Basic.Data
    {
        public string time;
        public string platform;
        public double amount;
        
        public Payment() { }
        public Payment(string time, string platform, double amount)
        {
            this.time = time;
            this.platform = platform;
            this.amount = amount;
        }
        public Payment(global::Data.Payment payment)
        {
            time = payment.time.ToString();
            platform = payment.platform;
            amount = payment.amount;
        }

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            
            time = Get<string>(dict, "time");
            platform = Get<string>(dict, "platform");
            amount = Get<double>(dict, "amount");
        }

        public override Dictionary<string, object> ToDictionary
        {
            get
            {
                return new Dictionary<string, object>
                {
                    ["time"] = time,
                    ["platform"] = platform,
                    ["amount"] = amount
                };
            }
        }
    }
}
