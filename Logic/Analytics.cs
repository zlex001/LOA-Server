using Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic
{
    public class Analytics
    {
        #region Instance Management
        private static Analytics instance;
        public static Analytics Instance { get { if (instance == null) { instance = new Analytics(); } return instance; } }
        #endregion

        private int ActivePlayerCount(DateTime dateTime) => global::Data.Database.Agent.Instance.Content.Count<global::Data.Database.Player>(p => p.Active(dateTime));

        #region User Growth Metrics (增长)
        private int NewDeviceCount(DateTime dateTime) => global::Data.Database.Agent.Instance.Content.Count<global::Data.Database.Device>(d => d.New(dateTime));
        private int NewDevicePlayerCount(DateTime dateTime) => global::Data.Database.Agent.Instance.Content.Count<global::Data.Database.Device>(d => d.New(dateTime) && !string.IsNullOrEmpty(d.player));
        private int NewValidDevicePlayerCount(DateTime dateTime) => global::Data.Database.Agent.Instance.Content.Count<global::Data.Database.Device>(d => 
        {
            if (!d.New(dateTime) || string.IsNullOrEmpty(d.player)) 
                return false;
            
            var player = global::Data.Database.Agent.Instance.Content.Get<global::Data.Database.Player>(p => p.Id == d.player);
            return player != null && player.skills.Count > 0;
        });
        private int NewPlayerCount(DateTime dateTime) => global::Data.Database.Agent.Instance.Content.Count<global::Data.Database.Player>(p => p.New(dateTime));
        private int NewValidPlayerCount(DateTime dateTime) => global::Data.Database.Agent.Instance.Content.Count<global::Data.Database.Player>(p => p.New(dateTime) && p.skills.Count > 0);
        #endregion

        #region Retention Metrics (留存)
        private int RetainedUsers(DateTime dateTime, int days) => global::Data.Database.Agent.Instance.Content.Count<global::Data.Database.Player>(p => p.New(dateTime.AddDays(-days)) && p.Active(dateTime));
        private int LostUsers(DateTime dateTime, int days) => global::Data.Database.Agent.Instance.Content.Count<global::Data.Database.Player>(p => p.New(dateTime.AddDays(-days)) && !p.Active(dateTime));
        private int WinBackUsers(DateTime dateTime, int days) => global::Data.Database.Agent.Instance.Content.Count<global::Data.Database.Player>(p => p.New(dateTime.AddDays(-(days + 1))) && !p.Active(dateTime.AddDays(-1)) && p.Active(dateTime));

        private double RetentionRate(DateTime dateTime, int days)
        {
            int newUsers = NewPlayerCount(dateTime.AddDays(-days));
            int retainedUsers = RetainedUsers(dateTime, days);
            return newUsers == 0 ? 0 : (double)retainedUsers / newUsers;
        }

        private double WinBackRate(DateTime dateTime, int days)
        {
            int lostUsers = LostUsers(dateTime.AddDays(-1), days);
            int winBackUsers = WinBackUsers(dateTime, days);
            return lostUsers == 0 ? 0 : (double)winBackUsers / lostUsers;
        }

        private double AverageUserLifetime => (double)global::Data.Database.Agent.Instance.Content.Gets<global::Data.Database.Player>().Select(p => p.activitys.Count).Sum() / global::Data.Database.Agent.Instance.Content.Count<global::Data.Database.Player>();
        #endregion

        #region Monetization Metrics (付费指标)
        private double ConversionRate(DateTime dateTime)
        {
            int @new = global::Data.Database.Agent.Instance.Content.Count<global::Data.Database.Player>(p => p.New(dateTime));
            int retained = global::Data.Database.Agent.Instance.Content.Count<global::Data.Database.Player>(p => p.New(dateTime) && p.NewPaying(dateTime));
            return @new == 0 ? 0 : (double)retained / @new;
        }

        private double Revenue(DateTime dateTime)
        {
            double revenue = 0;
            foreach (global::Data.Database.Player player in global::Data.Database.Agent.Instance.Content.Gets<global::Data.Database.Player>(p => p.Active(dateTime)))
            {
                foreach (global::Data.Database.Payment payment in player.payments)
                {
                    DateTime time = DateTime.Parse(payment.time);
                    if (time.Date == dateTime.Date)
                    {
                        revenue += payment.amount;
                    }
                }
            }
            return revenue;
        }

        private double ARPU(DateTime dateTime)
        {
            double revenue = Revenue(dateTime);
            int activeUsers = global::Data.Database.Agent.Instance.Content.Count<global::Data.Database.Player>(p => p.Active(dateTime));
            return activeUsers == 0 ? 0 : revenue / activeUsers;
        }

        private double ARPPU(DateTime dateTime)
        {
            double revenue = Revenue(dateTime);
            var activeUsers = global::Data.Database.Agent.Instance.Content.Gets<global::Data.Database.Player>(p => p.Active(dateTime));
            var payingUsers = activeUsers.Where(p => p.payments.Any(payment => DateTime.Parse(payment.time).Date == dateTime.Date));
            int payingUserCount = payingUsers.Count();
            return payingUserCount == 0 ? 0 : revenue / payingUserCount;
        }

        private double LTV(DateTime dateTime) => ARPU(dateTime) * AverageUserLifetime;
        #endregion

        #region Data Export & Reporting
        public class Daily
        {
            public string Weekday { get; set; }
            public string Date { get; set; }
            public int ActivePlayers { get; set; }
            public int NewDevices { get; set; }
            public int NewDevicePlayers { get; set; }
            public int NewValidDevicePlayers { get; set; }
            public int NewPlayers { get; set; }
            public int NewValidPlayers { get; set; }
            public double RetentionRate { get; set; }
            public double WinBackRate { get; set; }
            public double ConversionRate { get; set; }
            public double ARPU { get; set; }
            public double ARPPU { get; set; }
            public double AverageUserLifetime { get; set; }
            public double LTV { get; set; }

            public Daily() { }

            public Daily(DateTime dateTime)
            {
                Weekday = dateTime.DayOfWeek.ToString();
                Date = dateTime.ToString("yyyy-MM-dd");
                ActivePlayers = Instance.ActivePlayerCount(dateTime);
                NewDevices = Instance.NewDeviceCount(dateTime);
                NewDevicePlayers = Instance.NewDevicePlayerCount(dateTime);
                NewValidDevicePlayers = Instance.NewValidDevicePlayerCount(dateTime);
                NewPlayers = Instance.NewPlayerCount(dateTime);
                NewValidPlayers = Instance.NewValidPlayerCount(dateTime);
                RetentionRate = Instance.RetentionRate(dateTime, 1);
                WinBackRate = Instance.WinBackRate(dateTime, 1);
                ConversionRate = Instance.ConversionRate(dateTime);
                ARPU = Instance.ARPU(dateTime);
                ARPPU = Instance.ARPPU(dateTime);
                AverageUserLifetime = Instance.AverageUserLifetime;
                LTV = Instance.LTV(dateTime);
            }

        }
        
        public void SaveToDatabase(DateTime dateTime)
        {
            try
            {
                var daily = new Daily(dateTime);
                var dbAnalytics = new global::Data.Database.DailyAnalytics
                {
                    Date = dateTime,
                    Weekday = daily.Weekday,
                    ActivePlayers = daily.ActivePlayers,
                    NewDevices = daily.NewDevices,
                    NewDevicePlayers = daily.NewDevicePlayers,
                    NewValidDevicePlayers = daily.NewValidDevicePlayers,
                    NewPlayers = daily.NewPlayers,
                    NewValidPlayers = daily.NewValidPlayers,
                    RetentionRate = daily.RetentionRate,
                    WinBackRate = daily.WinBackRate,
                    ConversionRate = daily.ConversionRate,
                    ARPU = daily.ARPU,
                    ARPPU = daily.ARPPU,
                    AverageUserLifetime = daily.AverageUserLifetime,
                    LTV = daily.LTV
                };
                global::Data.Database.Agent.Instance.SaveDailyAnalytics(dbAnalytics);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ANALYTICS", $"Failed to save daily analytics to database for {dateTime:yyyy-MM-dd}");
                Utils.Debug.Log.Error("ANALYTICS", $"Exception: {ex.Message}");
                Utils.Debug.Log.Error("ANALYTICS", $"StackTrace: {ex.StackTrace}");
            }
        }
        
        public List<Daily> QueryFromDatabase(DateTime startDate, DateTime endDate)
        {
            try
            {
                var dbResults = global::Data.Database.Agent.Instance.QueryDailyAnalytics(startDate, endDate);
                var dailyList = dbResults.Select(db => new Daily
                {
                    Date = db.Date.ToString("yyyy-MM-dd"),
                    Weekday = db.Weekday,
                    ActivePlayers = db.ActivePlayers,
                    NewDevices = db.NewDevices,
                    NewDevicePlayers = db.NewDevicePlayers,
                    NewValidDevicePlayers = db.NewValidDevicePlayers,
                    NewPlayers = db.NewPlayers,
                    NewValidPlayers = db.NewValidPlayers,
                    RetentionRate = db.RetentionRate,
                    WinBackRate = db.WinBackRate,
                    ConversionRate = db.ConversionRate,
                    ARPU = db.ARPU,
                    ARPPU = db.ARPPU,
                    AverageUserLifetime = db.AverageUserLifetime,
                    LTV = db.LTV
                }).ToList();
                
                return dailyList;
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ANALYTICS", $"Failed to query daily analytics from database");
                Utils.Debug.Log.Error("ANALYTICS", $"Exception: {ex.Message}");
                return new List<Daily>();
            }
        }
        #endregion

    }
}

