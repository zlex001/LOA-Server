using System.Collections.Generic;
using System.Linq;

namespace Data.Config
{
    public class Life : global::Data.Config.Ability, ICharacter, ITag
    {
        public int Name { get; private set; }
        public int Description { get; private set; }
        public string category;
        public string[] parts;  // 部位配置（英文）：Head, Chest, Hand, etc.
        public List<string> Tags { get; private set; }
        public string gender;
        public int age;
        public int[] level;
        public string career;
        public int[] equipments;
        public int[] skills;
        public Dictionary<global::Data.Life.Attributes, int> attribute;
        public Dictionary<int, int> item;
        public int[] quests;


        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<int>(dict, "id");
            Name = Get<int>(dict, "name");
            Description = Get<int>(dict, "description");
            category = Get<string>(dict, "category");
            parts = Utils.Json.Deserialize<string[]>(Get<string>(dict, "parts"));  // 读取parts字段
            Tags = Utils.Json.Deserialize<List<string>>(Get<string>(dict, "tags"));
            gender = Get<string>(dict, "gender");
            age = Get<int>(dict, "age");
            level = Utils.Json.Deserialize<int[]>(Get<string>(dict, "level"));
            career = Get<string>(dict, "career");
            item = Utils.Json.Deserialize<Dictionary<int, int>>(Get<string>(dict, "item"));
            equipments = Utils.Json.Deserialize<int[]>(Get<string>(dict, "equipments"));
            skills = Utils.Json.Deserialize<int[]>(Get<string>(dict, "skills"));
            attribute = Utils.Json.Deserialize<global::Data.Life.Attributes, int>(Get<string>(dict, "attribute"));
            text = Utils.Json.Deserialize<Dictionary<string, List<string>>>(Get<string>(dict, "text"));
            quests = Utils.Json.Deserialize<int[]>(Get<string>(dict, "quests"));
        }

        public IEnumerable<string> GetTags() => Tags ?? Enumerable.Empty<string>();

    }
}


