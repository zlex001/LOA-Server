using Newtonsoft.Json;
using System.Linq;

namespace Logic.Design
{
    public class Item : Ability
    {
        public string type;
        public string function;
        public string unit;
        public string name;
        public string description;
        public string use;
        public string equip;
        public string unequip;
        public string burn;
        public int value;
        public int weight;
        public int flashpoint;
        public int volume;
        public string[] quests;
        public string[] usage;
        public HashSet<string> tags;


        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            id = Get<int>(dict, "id");
            cid = Get<string>(dict, "cid");
            type = Get<string>(dict, "type");
            function = Get<string>(dict, "function");
            name = Get<string>(dict, "name");
            description = Get<string>(dict, "description");
            unit = Get<string>(dict, "unit");
            use = Get<string>(dict, "use");
            equip = Get<string>(dict, "equip");
            unequip = Get<string>(dict, "unequip");
            burn = Get<string>(dict, "burn");
            value = Get<int>(dict, "value");
            weight = Get<int>(dict, "weight");
            flashpoint = Get<int>(dict, "flashpoint");
            volume = Get<int>(dict, "volume");

            quests = Get<string>(dict, "quests")?.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray() ?? Array.Empty<string>();
            usage = Get<string>(dict, "usage")?.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray() ?? Array.Empty<string>();
            tags = Utils.Tag.ParseTagsFromString(Get<string>(dict, "tags"));
        }

        private static string ConvertCraftTagToEnglish(string chineseTag)
        {
            return chineseTag switch
            {
                "料理" => "Cook",
                "祈祷" => "Brew",
                "制药" => "Alchemy",
                "轻装" => "Sew",
                "重装" => "Smith",
                _ => chineseTag
            };
        }

        private static string ConvertMaterialTagToEnglish(string chineseTag)
        {
            return chineseTag switch
            {
                "稻米" => "Rice",
                "小麦" => "Wheat",
                "谷物" => "Grain",
                "肉" => "Meat",
                "瓜果" => "Fruit",
                "蔬菜" => "Vegetable",
                "神性" => "Divinity",
                "治疗" => "Healing",
                "液体" => "Liquid",
                "柔软" => "Soft",
                "粗糙" => "Rough",
                "薄" => "Thin",
                "厚" => "Thick",
                "韧" => "Tough",
                "坚硬" => "Hard",
                "导电" => "Conductive",
                _ => chineseTag
            };
        }

        private static string ConvertIndividualTagToEnglish(string chineseTag)
        {
            return chineseTag switch
            {
                "锋利" => "Sharp",
                "肉" => "Meat",
                "柔软" => "Soft",
                "厚" => "Thick",
                "坚硬" => "Hard",
                "导电" => "Conductive",
                "神性" => "Divinity",
                "液体" => "Liquid",
                "稻米" => "Rice",
                "小麦" => "Wheat",
                "瓜果" => "Fruit",
                "蔬菜" => "Vegetable",
                "粗糙" => "Rough",
                "薄" => "Thin",
                "韧" => "Tough",
                "治疗" => "Healing",
                _ => chineseTag
            };
        }

        private static string ConvertUseTagToEnglish(string chineseUse)
        {
            return chineseUse switch
            {
                "食用" => "Feed",
                "治愈" => "Heal",
                "恢复" => "Refresh",
                "望远镜" => "Telescope",
                _ => chineseUse
            };
        }

        private static string ConvertTagKeyToEnglish(string chineseKey)
        {
            return chineseKey switch
            {
                "容量" => "Capacity",
                "负重" => "Carry",
                "装备部位" => "EquipPart",
                "槽位" => "Slot",
                "料理" => "Cook",
                "祈祷" => "Brew",
                "制药" => "Alchemy",
                "轻装" => "Sew",
                "重装" => "Smith",
                "视野" => "ViewScale",
                _ => chineseKey
            };
        }

        private static string ConvertEquipPartToEnglish(string chinesePart)
        {
            return chinesePart switch
            {
                "头" => "Head",
                "胸" => "Chest",
                "背" => "Back",
                "脚" => "Foot",
                "腿" => "Leg",
                "腰" => "Waist",
                "手" => "Hand",
                _ => chinesePart
            };
        }

        private static string ConvertSlotToEnglish(string chineseSlot)
        {
            if (chineseSlot.Contains("*"))
            {
                var parts = chineseSlot.Split('*');
                var slotName = parts[0] switch
                {
                    "袋子" => "Pouch",
                    _ => parts[0]
                };
                return $"{slotName}*{parts[1]}";
            }
            return chineseSlot switch
            {
                "袋子" => "Pouch",
                _ => chineseSlot
            };
        }

        private static HashSet<string> ConvertTagsCidToId(HashSet<string> originalTags)
        {
            var convertedTags = new HashSet<string>();

            foreach (var tag in originalTags)
            {
                if (tag.StartsWith("生成:"))
                {
                    var materialCid = tag.Substring(3).Trim();
                            var materialConfig = Agent.Instance.Content.Get<Item>(x => x.cid == materialCid);
                            if (materialConfig != null)
                            {
                        convertedTags.Add($"Generate:{materialConfig.id}");
                            }
                            else
                            {
                        convertedTags.Add($"Generate:{materialCid}");
                    }
                }
                else if (tag.StartsWith("掉落:"))
                {
                    var parts = tag.Split(':');
                    if (parts.Length >= 3)
                    {
                        var materialCid = parts[1];
                        var dropConfig = string.Join(":", parts.Skip(2));
                        
                        var materialConfig = Agent.Instance.Content.Get<Item>(x => x.cid == materialCid);
                        if (materialConfig != null)
                        {
                            convertedTags.Add($"Drop:{materialConfig.id}:{dropConfig}");
                        }
                        else
                        {
                            convertedTags.Add($"Drop:{materialCid}:{dropConfig}");
                        }
                    }
                }
                else if (tag.StartsWith("制作:"))
                {
                    var chineseCraft = tag.Substring(3).Trim();
                    var englishCraft = ConvertCraftTagToEnglish(chineseCraft);
                    convertedTags.Add($"Craft:{englishCraft}");
                }
                else if (tag.StartsWith("容量:") || tag.StartsWith("负重:"))
                {
                    var parts = tag.Split(':');
                    var englishKey = ConvertTagKeyToEnglish(parts[0]);
                    convertedTags.Add($"{englishKey}:{parts[1]}");
                }
                else if (tag.StartsWith("装备部位:"))
                {
                    var chinesePart = tag.Substring(5).Trim();
                    var englishPart = ConvertEquipPartToEnglish(chinesePart);
                    convertedTags.Add($"EquipPart:{englishPart}");
                }
                else if (tag.StartsWith("槽位:"))
                {
                    var chineseSlot = tag.Substring(3).Trim();
                    var englishSlot = ConvertSlotToEnglish(chineseSlot);
                    convertedTags.Add($"Slot:{englishSlot}");
                }
                else if (tag.StartsWith("使用:"))
                {
                    var chineseUse = tag.Substring(3).Trim();
                    var englishUse = ConvertUseTagToEnglish(chineseUse);
                    convertedTags.Add($"Use:{englishUse}");
                }
                else if (tag.Contains(":"))
                {
                    var parts = tag.Split(':');
                    var key = parts[0];
                    var value = parts[1];
                    
                    var englishKey = ConvertTagKeyToEnglish(key);
                    var englishValue = ConvertMaterialTagToEnglish(value);
                    convertedTags.Add($"{englishKey}:{englishValue}");
                }
                else
                {
                    var englishTag = ConvertIndividualTagToEnglish(tag);
                    convertedTags.Add(englishTag);
                }
            }

            return convertedTags;
        }

        public static void Convert()
        {
            List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
            foreach (Item config in Agent.Instance.Content.Gets<Item>())
            {



                List<int> quests = config.quests.Select(p => Agent.Instance.Content.Get<Quest>(x => x.cid == p).id).ToList();

                var nameMultilingual = Agent.Instance.Content.Get<Multilingual>(m => m.cid == config.name);
                var descriptionMultilingual = Agent.Instance.Content.Get<Multilingual>(m => m.cid == config.description);

                if (nameMultilingual == null && !string.IsNullOrEmpty(config.name))
                {
                    Logic.Validation.Agent.Instance.Create<Logic.Validation.Error>("Multilingual key not found", "Design", "Item", config.cid,
                        $"Item [{config.cid}] references multilingual key [{config.name}] which is not defined in 语言.csv");
                }

                if (descriptionMultilingual == null && !string.IsNullOrEmpty(config.description))
                {
                    Logic.Validation.Agent.Instance.Create<Logic.Validation.Error>("Multilingual key not found", "Design", "Item", config.cid,
                        $"Item [{config.cid}] references multilingual key [{config.description}] which is not defined in 语言.csv");
                }

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    {"id", config.id },
                    {"name", nameMultilingual?.id ?? 0 },
                    {"description", descriptionMultilingual?.id ?? 0 },
                    {"type", config.type },
                    {"weight", config.weight },
                    {"value", config.value },
                    {"volume", config.volume },

                    {"quests", JsonConvert.SerializeObject(quests)},
                    {"tags", JsonConvert.SerializeObject(ConvertTagsCidToId(config.tags))}
                };
                datas.Add(data);
            }

            string path = $"{Utils.Paths.Library}/Config/Item.csv";
            Utils.FileManager.Instance.DeleteFile(path);
            Utils.Csv.SaveByRows(datas, path);
        }
    }
}


