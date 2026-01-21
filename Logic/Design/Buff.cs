using Newtonsoft.Json;
using System.Collections.Generic;

namespace Logic.Design
{
    public class Buff : Ability
    {
        public string cid;
        public string name;
        public string interval;
        public string description;

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            id = Get<int>(dict, "id");
            cid = Get<string>(dict, "cid");
            name = Get<string>(dict, "name");
            interval = Get<string>(dict, "interval");
            description = Get<string>(dict, "description");
        }

        public static void Convert()
        {
            List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
            foreach (Buff config in Agent.Instance.Content.Gets<Buff>())
            {
                var nameMultilingual = Agent.Instance.Content.Get<Multilingual>(m => m.cid == config.name);
                if (nameMultilingual == null && !string.IsNullOrEmpty(config.name))
                {
                    Logic.Validation.Agent.Instance.Create<Logic.Validation.Error>("Multilingual key not found", "Design", "Buff", config.id.ToString(),
                        $"Buff [{config.id}] references multilingual key [{config.name}] which is not defined in 语言.csv");
                }
                
                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    {"id", config.id },
                    {"name", nameMultilingual?.id ?? 0 },
                    {"interval", config.interval },
                    {"description", config.description },
                };
                datas.Add(data);
            }

            string path = $"{Utils.Paths.Library}/Config/Buff.csv";
            Utils.FileManager.Instance.DeleteFile(path);
            Utils.Csv.SaveByRows(datas, path);
        }
    }
}

