using Newtonsoft.Json;

namespace Data.Design
{
    public class Skill : Ability
    {
        public string name;
        public string movements;

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            id = Get<int>(dict, "id");
            cid = Get<string>(dict, "cid");
            name = Get<string>(dict, "name");
            movements = Get<string>(dict, "movements");
        }

        public static void Convert()
        {
            List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
            foreach (Skill config in Agent.Instance.Content.Gets<Skill>())
            {
                List<int> movementsList = new List<int>();
                Dictionary<int, int> movementLevels = new Dictionary<int, int>();

                if (!string.IsNullOrEmpty(config.movements))
                {
                    foreach (var movementEntry in config.movements.Split(','))
                    {
                        var trimmed = movementEntry.Trim();
                        if (trimmed.Contains(':'))
                        {
                            var parts = trimmed.Split(':');
                            if (parts.Length == 2)
                            {
                                string movementCid = parts[0].Trim();
                                int requiredLevel = int.Parse(parts[1].Trim());
                                
                                var movement = Agent.Instance.Content.Get<Movement>(m => m.cid == movementCid);
                                if (movement != null)
                                {
                                    movementsList.Add(movement.id);
                                    movementLevels[movement.id] = requiredLevel;
                                }
                            }
                        }
                        else
                        {
                            var movement = Agent.Instance.Content.Get<Movement>(m => m.cid == trimmed);
                            if (movement != null)
                            {
                                movementsList.Add(movement.id);
                                movementLevels[movement.id] = 1;
                            }
                        }
                    }
                }

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    {"id", config.id },
                    {"name", Agent.Instance.Content.Get<Multilingual>(m=>m.cid== config.name).id },
                    {"movements", JsonConvert.SerializeObject(movementsList.ToArray()) },
                    {"movementLevels", JsonConvert.SerializeObject(movementLevels) },
                };
                datas.Add(data);
            }

            string path = $"{Utils.Paths.Library}/Config/Skill.csv";
            Utils.FileManager.Instance.DeleteFile(path);
            Utils.Csv.SaveByRows(datas, path);
        }

    }
}




