using System.Collections.Generic;
using System.Linq;

namespace Logic.Config
{
    public class Item : Ability, ICharacter, ITag
    {
        public int Name { get; private set; }
        public int Description { get; private set; }
        public string Type { get; private set; }
        public int[] quests;
        public string type;
        public Dictionary<string, int> feature;
        public List<string> Tags { get; private set; }
        public int value;
        public int weight;
        public int volume;

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<int>(dict, "id");
            value = Get<int>(dict, "value");
            weight = Get<int>(dict, "weight");
            volume = Get<int>(dict, "volume");
            Name = Get<int>(dict, "name");
            Description = Get<int>(dict, "description");
            type = Get<string>(dict, "type");
            quests = Utils.Json.Deserialize<int[]>(Get<string>(dict, "quests"));
            feature = Utils.Json.Deserialize<Dictionary<string, int>>(Get<string>(dict, "feature"));
            text = Utils.Json.Deserialize<Dictionary<string, List<string>>>(Get<string>(dict, "information"));
            Tags = Utils.Json.Deserialize<List<string>>(Get<string>(dict, "tags"));
        }

        public IEnumerable<string> GetTags() => Tags ?? Enumerable.Empty<string>();

    }
}