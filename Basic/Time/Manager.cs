using System;
using System.Collections.Generic;
using System.Linq;

namespace Basic.Time
{

    public class Manager : Element
    {

        private static Manager instance;
        public static Manager Instance { get { if (instance == null) { instance = new Manager(); } return instance; } }
        
        public readonly Scheduler Scheduler = new();
        
        public Manager() { }

        public enum Data
        {
            StartTime,
        }
   
        public override void Init(params object[] args)
        {
            DateTime now = DateTime.Now;
            data.raw[Data.StartTime] = now;
        }

        public DateTime StartTime => data.Get<DateTime>(Data.StartTime);

        public void Update()
        {
            Scheduler.Tick();
        }

    }
}
