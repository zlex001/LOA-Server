using System.Collections.Generic;

namespace Data.Config
{
    public class Replace : global::Data.Config.Ability
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


