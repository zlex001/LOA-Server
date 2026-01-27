using Basic;
using Utils;

namespace Logic
{
    public class Copy : Ability
    {
        public class Map : Logic.Map
        {
     
        }


        public Logic.Map Start { get; set; }
        public Plot Plot { get;  set; }
        public override void Init(params object[] args)
        {
            var start = (Logic.Map)args[0];
            var config = (Config.Plot.Copy)args[1];

            foreach (var map in start.Scene.Content.Gets<Logic.Map>(m => Utils.Mathematics.EuclideanDistance(start.Database.pos, m.Database.pos) <= config.scope && m.Copy == null))
            {
                var database = new Logic.Database.Map(map.Config.Id, 0, map.Database.pos, null);
                var m = map.Scene.Create<Map>(database);
                m.Database.shortest = map.Database.shortest;
                m.Copy = this;
                if (config.characters.TryGetValue(map.Config.Id, out var characterList))
                {
                    foreach (var character in characterList)
                    {
                        var characterConfig = Logic.Config.Agent.Instance.Content.Get<Config.Ability>(c => c.Id == character.id);
                        if (characterConfig != null)
                        {
                            for (int i = 0; i < character.count; i++)
                            {
                                if (characterConfig is Config.Life lifeCfg)
                                {
                                    int? level = (character.min.HasValue && character.max.HasValue) ? random.Next(character.min.Value, character.max.Value + 1) : null;
                                    var npc = m.Create<Life>(lifeCfg, level);
                                    npc.Birthplace = m;
                                }
                                else if (characterConfig is Config.Item itemCfg)
                                {
                                    var container = m.Load<Item>(itemCfg);
                                    if (character.loot != null && character.loot.Count > 0)
                                        GenerateLootForContainer(container, character.loot);
                                    if (character.nested != null && character.nested.Count > 0)
                                        GenerateNestedCharactersForContainer(container, character.nested);
                                }
                            }
                        }
                    }
                }
                Add(m);
            }
            Start = Content.Get<Copy.Map>(m => Enumerable.SequenceEqual(m.Database.pos, start.Database.pos));
        }
        public override void Release()
        {
            foreach (var copyMap in Content.Gets<Copy.Map>().ToList())
            {
                copyMap.Copy = null;
                copyMap.Destroy();
            }
            Start = null;
            Plot = null;
            base.Release();
        }


        private void GenerateLootForContainer(Item container, List<Config.Plot.Loot> lootPool)
        {
            if (lootPool == null || lootPool.Count == 0) return;

            var lootResult = GenerateLoot(lootPool);
            if (lootResult.id > 0 && lootResult.count > 0)
            {
                var lootItem = container.Load<Config.Item, Item>(lootResult.id, lootResult.count);
            }
        }
        
        private void GenerateNestedCharactersForContainer(Item container, List<Config.Plot.Character> nestedCharacters)
        {
            if (nestedCharacters == null || nestedCharacters.Count == 0) return;

            foreach (var nestedChar in nestedCharacters)
            {
                var characterConfig = Logic.Config.Agent.Instance.Content.Get<Config.Ability>(c => c.Id == nestedChar.id);
                if (characterConfig != null)
                {
                    for (int i = 0; i < nestedChar.count; i++)
                    {
                        if (characterConfig is Config.Life lifeCfg)
                        {
                            int? level = (nestedChar.min.HasValue && nestedChar.max.HasValue) 
                                ? random.Next(nestedChar.min.Value, nestedChar.max.Value + 1) 
                                : null;
                            var npc = container.Load<Config.Life, Life>(lifeCfg.Id, level);
                            if (container.Parent is Map map)
                            {
                                npc.Birthplace = map;
                            }
                        }
                        else if (characterConfig is Config.Item itemCfg)
                        {
                            var nestedContainer = container.Load<Item>(itemCfg);
                            if (nestedChar.loot != null && nestedChar.loot.Count > 0)
                                GenerateLootForContainer(nestedContainer, nestedChar.loot);
                            if (nestedChar.nested != null && nestedChar.nested.Count > 0)
                                GenerateNestedCharactersForContainer(nestedContainer, nestedChar.nested);
                        }
                    }
                }
            }
        }
        
        private (int id, int count) GenerateLoot(List<Config.Plot.Loot> lootPool)
        {

            double roll = random.NextDouble();
            double cumulative = 0.0;

            foreach (var entry in lootPool)
            {
                cumulative += entry.probability;
                if (roll < cumulative)
                {
                    if (entry.id <= 0)
                        return (0, 0);

                    int count = random.Next(entry.minCount, entry.maxCount + 1);
                    return (entry.id, count);
                }
            }

            return (0, 0);
        }


    }
}

