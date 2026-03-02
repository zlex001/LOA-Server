using System;
using System.Collections.Generic;

namespace Data.Database
{
    public class DailyAnalytics : Basic.MySql.Data
    {
        public DateTime Date { get; set; }
        public string Weekday { get; set; }
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
        public DateTime CreatedAt { get; set; }

        public DailyAnalytics() { }

        public override Dictionary<string, object> ToDictionary => new()
        {
            ["Date"] = Date.ToString("yyyy-MM-dd"),
            ["Weekday"] = Weekday,
            ["ActivePlayers"] = ActivePlayers,
            ["NewDevices"] = NewDevices,
            ["NewDevicePlayers"] = NewDevicePlayers,
            ["NewValidDevicePlayers"] = NewValidDevicePlayers,
            ["NewPlayers"] = NewPlayers,
            ["NewValidPlayers"] = NewValidPlayers,
            ["RetentionRate"] = RetentionRate,
            ["WinBackRate"] = WinBackRate,
            ["ConversionRate"] = ConversionRate,
            ["ARPU"] = ARPU,
            ["ARPPU"] = ARPPU,
            ["AverageUserLifetime"] = AverageUserLifetime,
            ["LTV"] = LTV,
            ["CreatedAt"] = CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        };

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            
            Date = DateTime.Parse(Get<string>(dict, "Date"));
            Weekday = Get<string>(dict, "Weekday");
            ActivePlayers = Get<int>(dict, "ActivePlayers");
            NewDevices = Get<int>(dict, "NewDevices");
            NewDevicePlayers = Get<int>(dict, "NewDevicePlayers");
            NewValidDevicePlayers = Get<int>(dict, "NewValidDevicePlayers");
            NewPlayers = Get<int>(dict, "NewPlayers");
            NewValidPlayers = Get<int>(dict, "NewValidPlayers");
            RetentionRate = Get<double>(dict, "RetentionRate");
            WinBackRate = Get<double>(dict, "WinBackRate");
            ConversionRate = Get<double>(dict, "ConversionRate");
            ARPU = Get<double>(dict, "ARPU");
            ARPPU = Get<double>(dict, "ARPPU");
            AverageUserLifetime = Get<double>(dict, "AverageUserLifetime");
            LTV = Get<double>(dict, "LTV");
            
            if (dict.ContainsKey("CreatedAt"))
            {
                CreatedAt = DateTime.Parse(Get<string>(dict, "CreatedAt"));
            }
            else
            {
                CreatedAt = DateTime.Now;
            }
        }

    }
}

