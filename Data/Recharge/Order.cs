using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Data.Recharge
{
    public class Order : Basic.Element
    {
        public DateTime CreateTime { get; private set; }
        public string Player { get; private set; }
        public string Item { get; private set; }
        public int Amount { get; set; }
        public string OutTradeNo => $"{CreateTime:yyyyMMddHHmmss}-{Player}-{Item}-{Amount}";
        public string BizContent
        {
            get
            {
                var bizContent = new
                {
                    out_trade_no = OutTradeNo,
                    total_amount = Amount.ToString(),
                    subject = $"{Item}*{Amount}",
                    product_code = "QUICK_MSECURITY_PAY",
                    body = "richParame"
                };
                return JsonConvert.SerializeObject(bizContent);
            }
        }
  
        public override void Init(params object[] args)
        {
            CreateTime = DateTime.Now;
            Player = (string)args[0];
            Item = (string)args[1];
            Amount = (int)args[2];
        }
   
    }

}


