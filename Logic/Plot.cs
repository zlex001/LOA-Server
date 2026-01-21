using System;
using Utils;

namespace Logic
{
    public class Plot : Ability
    {
        public Plot() : base() { }
        public Config.Plot Config { get; set; }
        public Enum Trigger { get; private set; }

        public override void Init(params object[] args)
        {
            Config = (Config.Plot)args[0];
            Trigger = Utils.Text.ParseEnum(Config.trigger);
            

        }
        public override void Release()
        {
            Agent.Instance.Remove(this);
            base.Release();
        }

    }
}
