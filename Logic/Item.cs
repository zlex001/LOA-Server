using Newtonsoft.Json;
using Utils;

namespace Logic
{
    public class Item : Character<Config.Item>, ITag
    {
        public enum Event
        {
            Equiped,
            UnEquiped,
            Picked,
            Sleeped,
            CountChangeComplete,
            Used,
            Enter,
        }
        public enum Types
        {
            Miscellaneous,
            Material,
            Food,
            Medicine,
            Beverage,
            Weapon,
            Armor,
            Currency,
            Book,
            Bag,
            Inscription,
            Tool,
        }
        public enum Data
        {
            Count,
            Durability,
            MaxDurability,
            MasterId,
            SkillId,
            Lock,
        }
        public int Price => Math.Max(1, (Config?.value ?? 1) * Count);

        public int Count { get => data.Get<int>(Data.Count); set => data.Change(Data.Count, value, this); }
        public int Durability { get => data.Get<int>(Data.Durability); set => data.Change(Data.Durability, value); }
        public int MaxDurability { get => data.Get<int>(Data.MaxDurability); set => data.Change(Data.MaxDurability, value); }
        public int MasterId { get => data.Get<int>(Data.MasterId); set => data.Change(Data.MasterId, value); }
        public int SkillId { get => data.Get<int>(Data.SkillId); set => data.Change(Data.SkillId, value); }
        public bool Lock { get => data.Get<bool>(Data.Lock); set => data.Change(Data.Lock, value, this); }
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public Types Type { get; private set; }
        public Dictionary<string, int> Container { get; private set; } = new Dictionary<string, int>();
        public Part.Types? EquipPart { get; private set; } = null;
        public Item() : base()
        {
            data.after.Register(Data.Count, OnCountChangeComplete);
            data.after.Register(Data.Lock, OnAfterLockChanged);
            data.min.Add(Data.Count, (Func<int>)(() => 0));
            data.raw[Data.Durability] = 0;
            data.raw[Data.MaxDurability] = 0;
            data.raw[Data.MasterId] = 0;
            data.raw[Data.SkillId] = 0;
        }
        public override void Init(params object[] args)
        {
            Config = (Config.Item)args[0];
            int count = args.Length > 1 ? (int)args[1] : 1;
            Dictionary<string, string> properties = args.Length > 2 ? (Dictionary<string, string>)args[2] : new();
            Type = Enum.TryParse(Config.type, out Types result) ? result : default;
            Array.ForEach(Part.Template.Get(Type), part => Create<Part>(part));
            data.raw[Data.Count] = count;
            ApplyPropertiesFromDatabase(properties);

            if (MaxDurability == 0)
            {
                int density = Math.Max(1, (int)(Config.weight / Config.volume));
                data.raw[Data.MaxDurability] = density;
                data.raw[Data.Durability] = density;
            }

            Container = new Dictionary<string, int>();
            EquipPart = null;
            var slots = GetSlots();
            foreach (var slot in slots)
            {
                Container[slot.Key] = slot.Value;
            }

            var capacity = GetTagValue("Capacity");
            if (capacity != null && int.TryParse(capacity, out int cap))
            {
                Container["Capacity"] = cap;
            }

            var carry = GetTagValue("Carry");
            if (carry != null && int.TryParse(carry, out int carryValue))
            {
                Container["Carry"] = carryValue;
            }

            var equipPart = GetTagValue("EquipPart");
            if (equipPart != null && Enum.TryParse(equipPart, out Part.Types equipPartResult))
            {
                EquipPart = equipPartResult;
            }

            Agent.Instance.Add(this);

        }



        public Part GetEquippedPart(Life life)
        {
            if (life == null) return null;
            Part equippedPart = life.Content.Gets<Part>().FirstOrDefault(part => part.Content.Has<Item>(item => item == this));
            return equippedPart;
        }
        private void OnCountChangeComplete(params object[] args)
        {
            int v = (int)args[0];
            if (v <= 0)
            {
                Destroy();
            }
            else if (Parent is Logic.Map map)
            {
                monitor.Fire(Event.CountChangeComplete, this);
            }
        }

        private void OnAfterLockChanged(params object[] args)
        {
            // 空函数，用户自己补充逻辑功能
        }

        private Dictionary<string, int> GetSlots()
        {
            return Config.Tags.ParseSlotTags();
        }

        private string GetTagValue(string prefix)
        {
            return Config.Tags.GetValue(prefix);
        }

        /// <summary>
        /// 从数据库Properties数据中提取并设置具体字段
        /// </summary>
        /// <param name="properties">数据库Properties数据</param>
        private void ApplyPropertiesFromDatabase(Dictionary<string, string> properties)
        {
            if (properties == null) return;

            if (properties.TryGetValue("Durability", out var durabilityStr))
            {
                var parts = durabilityStr?.Split('/');
                if (parts is [var a, var b]
                    && int.TryParse(a, out var current)
                    && int.TryParse(b, out var max))
                {
                    data.raw[Data.Durability] = current;
                    data.raw[Data.MaxDurability] = max;
                }
            }

            if (properties.TryGetValue("Master", out var masterCid) && !string.IsNullOrEmpty(masterCid))
            {
                var config = Logic.Design.Agent.Instance.Content.Gets<Logic.Design.Life>()
                    .FirstOrDefault(c => c.cid == masterCid);
                if (config != null) data.raw[Data.MasterId] = config.id;
            }

            if (properties.TryGetValue("Skill", out var skillCid) && !string.IsNullOrEmpty(skillCid))
            {
                var config = Logic.Design.Agent.Instance.Content.Gets<Logic.Design.Skill>()
                    .FirstOrDefault(c => c.cid == skillCid);
                if (config != null) data.raw[Data.SkillId] = config.id;
            }

            if (properties.TryGetValue("Items", out var itemsJson) && !string.IsNullOrWhiteSpace(itemsJson))
            {
                var itemsData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(itemsJson);
                if (itemsData != null && itemsData.Count > 0)
                {
                    LoadItemsFromPropertiesList(itemsData.Cast<object>().ToList());
                }
            }
        }
        private void LoadItemsFromPropertiesList(List<object> itemsList)
        {
            foreach (var itemObj in itemsList)
            {
                Dictionary<string, object> itemDict = itemObj is Dictionary<string, object> d
                    ? d
                    : (itemObj is Newtonsoft.Json.Linq.JObject j ? j.ToObject<Dictionary<string, object>>() : null);
                if (itemDict == null || !itemDict.TryGetValue("Id", out var idObj)) continue;

                int id = Convert.ToInt32(idObj);
                int count = 1;
                if (itemDict.TryGetValue("Count", out var countObj))
                    int.TryParse(countObj?.ToString(), out count);

                Dictionary<string, string> itemProperties = new();
                if (itemDict.TryGetValue("Properties", out var propsObj))
                {
                    if (propsObj is string propsString && !string.IsNullOrEmpty(propsString))
                        itemProperties = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(propsString) ?? new();
                    else if (propsObj is Dictionary<string, object> propsObjectDict)
                        foreach (var kvp in propsObjectDict) itemProperties[kvp.Key] = kvp.Value?.ToString() ?? "";
                    else if (propsObj is Dictionary<string, string> propsStringDict)
                        itemProperties = propsStringDict;
                }

                Load<Config.Item, Item>(id, count, itemProperties);
            }
        }


        /// <summary>
        /// 收集当前实例的属性数据，用于保存到数据库
        /// </summary>
        /// <returns>Properties数据</returns>
        public Dictionary<string, string> CollectPropertiesForDatabase()
        {
            var properties = new Dictionary<string, string>();

            // 处理耐久度 - 保存为"当前/最大"格式
            if (Durability > 0 || MaxDurability > 0)
            {
                properties["Durability"] = $"{Durability}/{MaxDurability}";
            }

            // 处理师傅ID转CID
            if (MasterId > 0)
            {
                try
                {
                    var lifeConfigs = Logic.Design.Agent.Instance.Content.Gets<Logic.Design.Life>();
                    var masterConfig = lifeConfigs.FirstOrDefault(c => c.id == MasterId);
                    if (masterConfig != null)
                    {
                        properties["Master"] = masterConfig.cid;
                    }
                }
                catch (Exception)
                {
                    properties["Master"] = MasterId.ToString(); // 降级到ID字符串
                }
            }

            // 处理武学ID转CID
            if (SkillId > 0)
            {
                try
                {
                    var skillConfigs = Logic.Design.Agent.Instance.Content.Gets<Logic.Design.Skill>();
                    var skillConfig = skillConfigs.FirstOrDefault(c => c.id == SkillId);
                    if (skillConfig != null)
                    {
                        properties["Skill"] = skillConfig.cid;
                    }
                }
                catch (Exception)
                {
                    properties["Skill"] = SkillId.ToString(); // 降级到ID字符串
                }
            }

            // 处理装备内物品数据 - 如果装备Content中有子Item，保存这些物品（支持背包、腰带等所有容器装备）
            var containerItems = Content.Gets<Item>();
            if (containerItems.Any())
            {
                var itemsData = new List<Dictionary<string, object>>();
                foreach (var item in containerItems)
                {
                    var itemData = new Dictionary<string, object>
                    {
                        ["Id"] = item.Config.Id,
                        ["Count"] = item.Count
                    };

                        var itemProperties = item.CollectPropertiesForDatabase();
                        if (itemProperties.Any())
                        {
                            itemData["Properties"] = Newtonsoft.Json.JsonConvert.SerializeObject(itemProperties);
                        }
                        else
                        {
                            itemData["Properties"] = "{}";
                        }

                    itemsData.Add(itemData);
                }

                if (itemsData.Any())
                {
                    properties["Items"] = Newtonsoft.Json.JsonConvert.SerializeObject(itemsData);
                }
            }

            return properties;
        }

        public IEnumerable<string> GetTags() => Config?.GetTags() ?? Enumerable.Empty<string>();

    }
}