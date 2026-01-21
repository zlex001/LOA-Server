using System.Collections.Generic;

namespace Logic.Design
{
    public class Common : Ability
    {
        public string Cid { get; set; }
        public string value;

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Cid = Get<string>(dict, "id");
            value = Get<string>(dict, "value");
        }
    }
}


