namespace Logic
{
    public class Buff : Ability<Config.Buff>
    {
        public enum Data
        {
            EffectValue,
            Duration,
            RemainingTime,
        }

        public double EffectValue { get => data.Get<double>(Data.EffectValue); set => data.Change(Data.EffectValue, value, this); }
        public double Duration { get => data.Get<double>(Data.Duration); set => data.Change(Data.Duration, value, this); }
        public double RemainingTime { get => data.Get<double>(Data.RemainingTime); set => data.Change(Data.RemainingTime, value, this); }

        public override void Init(params object[] args)
        {
            Config = (Config.Buff)args[0];
            double effectValue = args.Length > 1 ? (double)args[1] : 0;
            double duration = args.Length > 2 ? (double)args[2] : 0;
            
            data.raw[Data.EffectValue] = effectValue;
            data.raw[Data.Duration] = duration;
            data.raw[Data.RemainingTime] = duration;
        }
    }
}

