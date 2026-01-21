using System;
using System.Collections.Generic;

namespace Logic
{
    public class Payment : Logic.Ability
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
