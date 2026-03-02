using System;
using System.Collections.Generic;
using System.Linq;

namespace Data.ServerMerge
{
    public class IdMapper
    {
        private Dictionary<string, IdMapping> mappings;
        private const long ID_OFFSET_BASE = 100000;

        public IdMapper()
        {
            mappings = new Dictionary<string, IdMapping>();
        }

        public void GenerateMappings(List<string> sourceServerIds, string targetServerId)
        {
            mappings.Clear();

            for (int i = 0; i < sourceServerIds.Count; i++)
            {
                var serverId = sourceServerIds[i];
                var mapping = new IdMapping
                {
                    SourceServerId = serverId,
                    Offset = serverId == targetServerId ? 0 : (i + 1) * ID_OFFSET_BASE
                };

                mappings[serverId] = mapping;
            }
        }

        public IdMapping GetMapping(string serverId)
        {
            return mappings.TryGetValue(serverId, out var mapping) ? mapping : null;
        }

        public long MapPlayerId(string serverId, long originalId)
        {
            var mapping = GetMapping(serverId);
            return mapping?.MapPlayerId(originalId) ?? originalId;
        }

        public long MapItemId(string serverId, long originalId)
        {
            var mapping = GetMapping(serverId);
            return mapping?.MapItemId(originalId) ?? originalId;
        }

        public string MapPlayerName(string serverId, string originalName, HashSet<string> existingNames)
        {
            var mapping = GetMapping(serverId);
            if (mapping == null) return originalName;

            var hasDuplicate = existingNames.Contains(originalName);
            var newName = mapping.MapPlayerName(originalName, hasDuplicate);
            
            if (hasDuplicate)
            {
                mapping.NameMap[originalName] = newName;
            }

            return newName;
        }

        public Dictionary<string, IdMapping> GetAllMappings()
        {
            return new Dictionary<string, IdMapping>(mappings);
        }

        public void SaveMappings(string filePath)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(mappings, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(filePath, json);
        }

        public void LoadMappings(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                Utils.Debug.Log.Error("MERGE", $"Mapping file not found: {filePath}");
                return;
            }

            var json = System.IO.File.ReadAllText(filePath);
            mappings = Utils.Json.Deserialize<Dictionary<string, IdMapping>>(json);
        }
    }
}

