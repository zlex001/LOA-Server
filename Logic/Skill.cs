using Newtonsoft.Json;
using Utils;

namespace Logic
{
    public class Skill : Ability<Config.Skill>
    {
        public enum Data
        {
            Exp,
            Level,
        }
        public enum Event
        {
            Upgrade,
        }
 
    public int Exp { get => data.Get<int>(Data.Exp); set => data.Change(Data.Exp, value, this); }
    public int Level { get => data.Get<int>(Data.Level); set => data.Change(Data.Level, value, this); }
    public int NextExp { get; set; }
    public override void Init(params object[] args)
    {
        Config = (Config.Skill)args[0];
        int exp = args.Length > 1 ? (int)args[1] : 0;
        int level = args.Length > 2 ? (int)args[2] : 1;
        data.raw[Data.Exp] = exp;
        data.raw[Data.Level] = level;
        foreach (int movement in Config.movements)
        {
            Load<Config.Movement, Movement>(movement);
        }
    }



    }
}

