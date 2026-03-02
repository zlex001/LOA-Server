using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Data.Database
{
    public class Device : Basic.MySql.Data
    {
        public enum Platforms
        {
            OSXEditor = 0,
            OSXPlayer = 1,
            WindowsPlayer = 2,
            OSXWebPlayer = 3,           // 已废弃：Unity 5.4+
            OSXDashboardPlayer = 4,     // 已废弃：Unity 5.4+
            WindowsWebPlayer = 5,       // 已废弃：Unity 5.4+
            WindowsEditor = 7,
            IPhonePlayer = 8,
            PS3 = 9,                    // 已废弃：Unity 5.5+
            XBOX360 = 10,               // 已废弃：Unity 5.5+
            Android = 11,
            NaCl = 12,                  // 已废弃：Unity 5.0+
            LinuxPlayer = 13,
            FlashPlayer = 15,           // 已废弃：Unity 5.0+
            LinuxEditor = 16,
            WebGLPlayer = 17,
            MetroPlayerX86 = 18,        // 已废弃，使用 WSAPlayerX86
            WSAPlayerX86 = 18,
            MetroPlayerX64 = 19,        // 已废弃，使用 WSAPlayerX64
            WSAPlayerX64 = 19,
            MetroPlayerARM = 20,        // 已废弃，使用 WSAPlayerARM
            WSAPlayerARM = 20,
            WP8Player = 21,             // 已废弃：Unity 5.3+
            BB10Player = 22,            // 已废弃：Unity 5.4+
            BlackBerryPlayer = 22,      // 已废弃：Unity 5.4+
            TizenPlayer = 23,           // 已废弃：Unity 2017.3+
            PSP2 = 24,                  // 已废弃：Unity 2018.3+
            PS4 = 25,
            PSM = 26,                   // 已废弃：Unity 5.3+
            XboxOne = 27,
            SamsungTVPlayer = 28,       // 已废弃：Unity 2017.3+
            WiiU = 30,                  // 已废弃：Unity 2018.1+
            tvOS = 31,
            Switch = 32,
            Lumin = 33,
            Stadia = 34,
            CloudRendering = 35,
            GameCoreScarlett = -1,      // 已废弃，使用 GameCoreXboxSeries
            GameCoreXboxSeries = 36,
            GameCoreXboxOne = 37,
            PS5 = 38,
            EmbeddedLinuxArm64 = 39,
            EmbeddedLinuxArm32 = 40,
            EmbeddedLinuxX64 = 41,
            EmbeddedLinuxX86 = 42,
            LinuxServer = 43,
            WindowsServer = 44,
            OSXServer = 45
        }
        public string Id { get; set; }
        public string player;
        public Platforms Platform;
        public Text.Languages PreferredLanguage;
        public Queue<string> activitys=new Queue<string>();
        public Device() { }
        public Device(string id, Platforms platform)
        {
            this.Id = id;
            this.Platform = platform;
            this.PreferredLanguage = Text.Languages.ChineseSimplified;
            activitys.Enqueue($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<string>(dict, "Id");
            player = Get<string>(dict, "Player");
            Platform = Get<Platforms>(dict, "Platform");
            PreferredLanguage = Get<Text.Languages>(dict, "PreferredLanguage");
            activitys = Utils.Json.Deserialize<Queue<string>>(Get<string>(dict, "Activitys"));
        }
        public override Dictionary<string, object> ToDictionary
        {
            get
            {
                var dict = new Dictionary<string, object>
                {
                    ["Id"] = Id,
                    ["Player"] = player,
                    ["Platform"] = Platform,
                    ["PreferredLanguage"] = PreferredLanguage,
                    ["Activitys"] = JsonConvert.SerializeObject(activitys),
                };
                return dict;
            }
        }
        public bool New(DateTime dateTime) => DateTime.Parse(activitys.Peek()).Date == dateTime.Date;
    }
}

