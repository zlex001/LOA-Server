using System;
using System.Collections.Generic;

namespace Data
{
    public class Payment : global::Data.Ability
    {
        public DateTime time;
        public string platform;
        public double amount;
        public override void Init(params object[] args)
        {
            Database.Payment payment = (Database.Payment)args[0];
            time = DateTime.Parse(payment.time);
            platform = payment.platform;
            amount = payment.amount;
        }

    }
}
