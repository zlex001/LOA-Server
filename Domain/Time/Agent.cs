using Basic;
using Logic;
using System;
using System.Linq;
using Utils;

namespace Domain.Time
{
    public class Current
    {
        public DateTime DateTime { get; private set; }
        public Agent.Period Period { get; private set; }
        public Agent.Season Season { get; private set; }
        public Agent.Hour ChineseHour { get; private set; }
        public int Day { get; private set; }
        
        public bool Update(DateTime dt, out bool periodChanged, out bool seasonChanged, out bool dayChanged)
        {
            periodChanged = false;
            seasonChanged = false;
            dayChanged = false;
            DateTime = dt;

            var p = GetPeriod(dt);
            if (p != Period) { periodChanged = true; Period = p; }

            var s = GetSeason(dt);
            if (s != Season) { seasonChanged = true; Season = s; }

            var d = dt.Day;
            if (d != Day) { dayChanged = true; Day = d; }

            ChineseHour = GetChineseHour(dt);
            return periodChanged || seasonChanged || dayChanged;
        }

        private static Agent.Period GetPeriod(DateTime dt) => dt.Hour switch
        {
            >= 5 and < 7 => Agent.Period.Dawn,
            >= 7 and < 12 => Agent.Period.Morning,
            >= 12 and < 18 => Agent.Period.Afternoon,
            >= 18 and < 22 => Agent.Period.Evening,
            _ => Agent.Period.Night
        };

        private static Agent.Season GetSeason(DateTime dt) => dt.Month switch
        {
            >= 1 and <= 3 => Agent.Season.Spring,
            >= 4 and <= 6 => Agent.Season.Summer,
            >= 7 and <= 9 => Agent.Season.Autumn,
            _ => Agent.Season.Winter
        };

        private static Agent.Hour GetChineseHour(DateTime dt)
            => (Agent.Hour)(((dt.Hour + 1) / 2) % 12);

        public override string ToString() => $"{DateTime:yyyy-MM-dd HH:mm:ss} {Period} {Season}";
    }

    public class Agent
    {
        public static Agent Instance => instance ??= new Agent();
        private static Agent instance;

        public const double Rate = 360.0;
        private DateTime serverStartTime;

        public readonly Current Current = new();
        public static DateTime Now => Instance.Current.DateTime;
        
        public Basic.Time.Scheduler Scheduler => Basic.Time.Manager.Instance.Scheduler;
        
        public enum Period { Dawn, Morning, Afternoon, Evening, Night }
        public enum Season { Spring, Summer, Autumn, Winter }
        public enum Hour { Zi, Chou, Yin, Mao, Chen, Si, Wu, Wei, Shen, You, Xu, Hai }
        public enum Type { Minute, Hour, Day, Month, Year }
        public enum ShopType { General, Tavern, NightMarket, Special }

        public void Init()
        {
            serverStartTime = DateTime.Now;
            Current.Update(GetNow(), out _, out _, out _);
            
            Scheduler.Repeat(1000, (_) => OnTick());

            State.Instance.Init();
        }

        public void Update()
        {
            Scheduler.Tick();
        }

        public TimeSpan GetTimeUntilNextRefresh(DateTime last, TimeSpan interval)
            => last.Add(interval) - Current.DateTime;

        private DateTime GetNow()
            => serverStartTime.AddSeconds((DateTime.Now - serverStartTime).TotalSeconds * Rate);

        private void OnTick()
        {
            var dt = GetNow();
            var oldPeriod = Current.Period;
            var oldSeason = Current.Season;
            var oldDay = Current.Day;
            bool changed = Current.Update(dt, out var periodChanged, out var seasonChanged, out var dayChanged);

            if (changed)
            {
                if (periodChanged) Daily.Instance.OnPeriodChanged(oldPeriod);
                if (seasonChanged) Domain.Time.Season.Instance.OnSeasonChanged(oldSeason);
                if (dayChanged) Daily.Instance.Update(oldDay);
            }

            CheckEvents(dt);
        }

        private void CheckEvents(DateTime dt)
        {
        }


    }
}
