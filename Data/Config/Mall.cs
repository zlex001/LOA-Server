using System.Collections.Generic;

namespace Data.Config
{
    public class Mall : Ability
    {
        public enum Types
        {
            Other = 0,
            Subscription = 1,
            Experience = 2,  // Unified experience multiplier (Character/Skill/Pet)
            Equipment = 5,
            Pack = 6,
        }

        public int Name { get; private set; }
        public int Description { get; private set; }
        public Types Type { get; private set; }
        public Dictionary<int, int> Items { get; private set; }
        public int Price { get; private set; }
        public int Limit { get; private set; }
        public int Value { get; private set; }

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<int>(dict, "id");
            Name = Get<int>(dict, "name");
            Description = Get<int>(dict, "description");
            Type = System.Enum.TryParse<Types>(Get<string>(dict, "type"), true, out var t) ? t : Types.Other;
            Items = Utils.Json.Deserialize<Dictionary<int, int>>(Get<string>(dict, "items"));
            Price = Get<int>(dict, "price");
            Limit = Get<int>(dict, "limit");
            Value = Get<int>(dict, "value");
        }
    }
}
