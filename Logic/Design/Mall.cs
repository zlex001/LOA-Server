using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Logic.Design
{
    public class Mall : Ability
    {
        public int id;
        public string name;
        public string description;
        public string type;
        public string items;
        public int price;
        public int limit;
        public int value;

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            id = Get<int>(dict, "id");
            name = Get<string>(dict, "name");
            description = Get<string>(dict, "description");
            type = Get<string>(dict, "type");
            items = Get<string>(dict, "items");
            price = Get<int>(dict, "price");
            limit = Get<int>(dict, "limit");
            value = Get<int>(dict, "value");
        }

        public static void Convert()
        {
            List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
            foreach (Mall config in Agent.Instance.Content.Gets<Mall>())
            {
                var parsedItems = ParseItems(config.items);
                var convertedItems = new Dictionary<int, int>();
                bool hasError = false;

                if (parsedItems.Count > 0)
                {
                    foreach (var item in parsedItems)
                    {
                        var itemConfig = Agent.Instance.Content.Get<Item>(x => x.cid == item.Key);
                        if (itemConfig == null)
                        {
                            Logic.Validation.Agent.Instance.Create<Logic.Validation.Error>(
                                "Item not found", "Design", "Mall", config.id.ToString(),
                                $"Mall [{config.id}] references item [{item.Key}] which is not defined in item csv");
                            hasError = true;
                            break;
                        }
                        convertedItems[itemConfig.id] = item.Value;
                    }

                    if (hasError) continue;
                }

                var nameMultilingual = Agent.Instance.Content.Get<Multilingual>(m => m.cid == config.name);
                var descMultilingual = Agent.Instance.Content.Get<Multilingual>(m => m.cid == config.description);
                
                if (nameMultilingual == null && !string.IsNullOrEmpty(config.name))
                {
                    Logic.Validation.Agent.Instance.Create<Logic.Validation.Error>("Multilingual key not found", "Design", "Mall", config.id.ToString(),
                        $"Mall [{config.id}] references multilingual key [{config.name}] which is not defined in language csv");
                }
                if (descMultilingual == null && !string.IsNullOrEmpty(config.description))
                {
                    Logic.Validation.Agent.Instance.Create<Logic.Validation.Error>("Multilingual key not found", "Design", "Mall", config.id.ToString(),
                        $"Mall [{config.id}] references multilingual key [{config.description}] which is not defined in language csv");
                }

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    {"id", config.id},
                    {"name", nameMultilingual?.id ?? 0},
                    {"description", descMultilingual?.id ?? 0},
                    {"type", config.type},
                    {"items", JsonConvert.SerializeObject(convertedItems)},
                    {"price", config.price},
                    {"limit", config.limit},
                    {"value", config.value}
                };
                datas.Add(data);
            }

            string path = $"{Utils.Paths.Library}/Config/Mall.csv";
            Utils.FileManager.Instance.DeleteFile(path);
            Utils.Csv.SaveByRows(datas, path);
        }

        private static Dictionary<string, int> ParseItems(string itemsStr)
        {
            var result = new Dictionary<string, int>();
            if (string.IsNullOrEmpty(itemsStr)) return result;

            var entries = itemsStr.Split(';');
            foreach (var entry in entries)
            {
                var trimmed = entry.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var parts = trimmed.Split('×');
                if (parts.Length == 2)
                {
                    var itemName = parts[0].Trim();
                    if (int.TryParse(parts[1].Trim(), out int count))
                    {
                        if (result.ContainsKey(itemName))
                            result[itemName] += count;
                        else
                            result[itemName] = count;
                    }
                }
                else if (parts.Length == 1)
                {
                    var itemName = parts[0].Trim();
                    if (result.ContainsKey(itemName))
                        result[itemName] += 1;
                    else
                        result[itemName] = 1;
                }
            }
            return result;
        }
    }
}
