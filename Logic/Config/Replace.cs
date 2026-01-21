using System.Collections.Generic;

namespace Logic.Config
{
    public class Replace : Logic.Config.Ability
    {
        public string a;
        public string b;
        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            a = Get<string>(dict, "a");
            b = Get<string>(dict, "b");

        }
    }
}


