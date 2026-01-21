using System.Collections.Generic;

namespace Logic.Config
{
    public class Damage : Ability
    {
        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<int>(dict, "id");
            text = Utils.Json.Deserialize<Dictionary<string, List<string>>>(Get<string>(dict, "information"));
        }
    }
}


