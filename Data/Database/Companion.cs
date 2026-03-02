using System;
using System.Collections.Generic;

namespace Data.Database
{
    public class Companion
    {
        public int LifeConfigId;
        public int Level;
        public string Source;
        public DateTime? ExpireTime;

        public Dictionary<string, object> ToDictionary => new()
        {
            ["LifeConfigId"] = LifeConfigId,
            ["Level"] = Level,
            ["Source"] = Source,
            ["ExpireTime"] = ExpireTime?.ToString() ?? ""
        };
    }
}

