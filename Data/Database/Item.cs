using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Data.Database
{
    [Serializable]
    public class Item : Basic.Data
    {
        public int Id { get; set; }
        public int Count { get; set; }
        public global::Data.Part.Types Part { get; set; } = global::Data.Part.Types.Hand; // 记录Item所在的Part类型，默认Hand
        public Dictionary<string, string> Properties { get; set; } = new();
        
        public Item() { }
        
        public Item(int id, int count = 1)
        {
            this.Id = id;
            this.Count = count;
        }

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<int>(dict, "Id");
            Count = Get<int>(dict, "Count");
            
            // 加载Part类型，如果没有则默认Hand（兼容旧数据）
            if (dict.TryGetValue("Part", out var partObj))
            {
                if (partObj is string partStr && System.Enum.TryParse<global::Data.Part.Types>(partStr, out var partType))
                {
                    Part = partType;
                }
                else if (partObj is int partInt)
                {
                    Part = (global::Data.Part.Types)partInt;
                }
            }
            else
            {
                Part = global::Data.Part.Types.Hand; // 旧数据默认Hand
            }

            if (dict.TryGetValue("Properties", out var propsObj) && propsObj != null)
            {
                try
                {
                    if (propsObj is JObject j)
                    {
                        foreach (var kv in j)
                        {
                            Properties[kv.Key] = kv.Value is JArray ? JsonConvert.SerializeObject(kv.Value) : kv.Value?.ToString() ?? "";
                        }
                    }
                    else if (propsObj is string propsString && !string.IsNullOrEmpty(propsString))
                    {
                        var propsDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(propsString);
                        if (propsDict != null)
                        {
                            foreach (var kv in propsDict)
                            {
                                Properties[kv.Key] = kv.Value ?? "";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.Debug.Log.Warning("DATABASE", $"Properties反序列化失败: {ex.Message}");
                }
            }
        }


        public override Dictionary<string, object> ToDictionary
        {
            get
            {
                var dict = new Dictionary<string, object>
                {
                    ["Id"] = Id,
                    ["Count"] = Count,
                    ["Part"] = Part.ToString(), // 保存Part类型
                };
                
                if (Properties.Any())
                {
                    dict["Properties"] = JsonConvert.SerializeObject(Properties);
                }
                
                return dict;
            }
        }
    }
} 