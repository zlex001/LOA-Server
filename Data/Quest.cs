using System;
using Utils;

namespace Data
{
    public class Quest : Ability
    {
        public Quest() : base() { }
        public Config.Quest Config { get; set; }
        public Enum Trigger { get; private set; }

        public override void Init(params object[] args)
        {
            Config = (Config.Quest)args[0];
            Trigger = Utils.Text.ParseEnum(Config.trigger);
            

        }
        public override void Release()
        {
            Agent.Instance.Remove(this);
            base.Release();
        }

    }
}
