using Newtonsoft.Json;

namespace Logic.Config
{
    public class Skill : Ability, IName
    {
        public int Name { get; set; }
        public string career;
        public int[] movements;
        public Dictionary<int, int> movementLevels;
        public Dictionary<Logic.Life.Attributes, int> attribute;
        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<int>(dict, "id");
            Name = Get<int>(dict, "name");
            career = Get<string>(dict, "career");
            movements = Utils.Json.Deserialize<int[]>(Get<string>(dict, "movements"));
            movementLevels = Utils.Json.Deserialize<Dictionary<int, int>>(Get<string>(dict, "movementLevels"));
            attribute = Utils.Json.Deserialize<Logic.Life.Attributes, int>(Get<string>(dict, "attribute"));
            text = Utils.Json.Deserialize<Dictionary<string, List<string>>>(Get<string>(dict, "text"));
        }

        public bool IsMovementUnlocked(int movementId, int skillLevel)
        {
            if (movementLevels == null || !movementLevels.ContainsKey(movementId))
            {
                return true;
            }
            return skillLevel >= movementLevels[movementId];
        }
    }
}


