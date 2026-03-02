using System;
using Utils;

namespace Logic.Time
{
    public class Season
    {
        public static Season Instance => instance ??= new Season();
        private static Season instance;

        public void Init()
        {
        }

        public void OnSeasonChanged(Agent.Season oldSeason)
        {
            // 扩展钩子：如天气/产出变化等
        }
    }
}
