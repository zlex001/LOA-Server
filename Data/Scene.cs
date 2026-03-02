using Basic;
using Org.BouncyCastle.Asn1.Pkcs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Data
{
    public class Scene : Ability<Config.Scene>
    {
        public enum Types
        {
            Default,
            City,
            Plain,
            Hill,
            Basin,
            Mountain,
            Desert,
            Volcano,
            Plateau,
            Wetland,
            Glacier,
            Island,
            Coast,
            Canyon,
        }
        
        public Database.Scene Database { get; private set; }
        public Types Type { get; set; }
        
        public override void Init(params object[] args)
        {
            Database = (Database.Scene)args[0];
            Config = global::Data.Config.Agent.Instance.Content.Get<Config.Scene>(m => m.Id == Database.id);
            Type = Enum.TryParse<Types>(Config.Type, true, out var type) ? type : Types.Default;
            foreach (global::Data.Database.Map database in Database.maps)
            {
                Map map = Create<Map>(database);
            }
        }
        public void UpdateLife()
        {
            foreach (global::Data.Config.Map config in Content.Gets<global::Data.Map>().Select(m => m.Config).Distinct())
            {
                foreach (var character in config.Characters)
                {
                    if (global::Data.Config.Agent.Instance.Content.Get<global::Data.Config.Ability>(c => c.Id == character.id) is global::Data.Config.Life)
                    {
                        GenerateLife(config, character);
                    }
                }
            }
        }
        private void GenerateLife(global::Data.Config.Map config, (int id, int count, int? minCount, int? maxCount, int? minLevel, int? maxLevel, double probability) character)
        {
            foreach (global::Data.Map map in Content.Gets<global::Data.Map>(m => m.Config == config && m.Database.teleport == null))
            {
                if (Utils.Random.Instance.NextDouble() > character.probability) continue;
                
                int targetCount = character.minCount.HasValue && character.maxCount.HasValue 
                    ? Utils.Random.Instance.Next(character.minCount.Value, character.maxCount.Value + 1)
                    : character.count;
                
                int currentCount = global::Data.Agent.Instance.Content.Gets<global::Data.Life>(l => !(l is global::Data.Player) && l.Config.Id == character.id && l.Birthplace == map).Count();
                int shortage = targetCount - currentCount;
                
                for (int i = 0; i < shortage; i++)
                {
                    int? level = character.minLevel.HasValue && character.maxLevel.HasValue 
                        ? Utils.Random.Instance.Next(character.minLevel.Value, character.maxLevel.Value + 1) 
                        : null;
                    map.Load<global::Data.Config.Life, global::Data.Life>(character.id, level).Birthplace = map;
                }
            }
        }
        public void InitializeCharacters()
        {
            foreach (global::Data.Config.Map config in Content.Gets<global::Data.Map>().Select(m => m.Config).Distinct())
            {
                foreach (var character in config.Characters)
                {
                    if (global::Data.Config.Agent.Instance.Content.Get<global::Data.Config.Ability>(c => c.Id == character.id) is global::Data.Config.Life)
                    {
                        GenerateLife(config, character);
                    }
                    else if (global::Data.Config.Agent.Instance.Content.Get<global::Data.Config.Ability>(c => c.Id == character.id) is global::Data.Config.Item)
                    {
                        foreach (global::Data.Map map in Content.Gets<global::Data.Map>(m => m.Config == config && m.Database.teleport == null))
                        {
                            if (Utils.Random.Instance.NextDouble() > character.probability) continue;
                            
                            int targetCount = character.minCount.HasValue && character.maxCount.HasValue 
                                ? Utils.Random.Instance.Next(character.minCount.Value, character.maxCount.Value + 1)
                                : character.count;
                            
                            int currentCount = map.Content.Gets<global::Data.Item>(i => i.Config.Id == character.id).Sum(i => i.Count);
                            int shortage = targetCount - currentCount;
                            
                            for (int i = 0; i < shortage; i++)
                            {
                                var existing = map.Content.Get<global::Data.Item>(it => it.Config.Id == character.id);
                                if (existing != null)
                                {
                                    existing.Count += 1;
                                }
                                else
                                {
                                    map.Load<global::Data.Config.Item, global::Data.Item>(character.id, 1);
                                }
                            }
                        }
                    }
                }
            }

            foreach (var character in Config.Characters)
            {
                if (Utils.Random.Instance.NextDouble() > character.probability) continue;
                
                if (global::Data.Config.Agent.Instance.Content.Get<global::Data.Config.Ability>(c => c.Id == character.id) is global::Data.Config.Life)
                {
                    var maps = Content.Gets<global::Data.Map>(m => m.Database.teleport == null);
                    int targetCount = character.minCount.HasValue && character.maxCount.HasValue 
                        ? Utils.Random.Instance.Next(character.minCount.Value, character.maxCount.Value + 1)
                        : character.count;
                    int actualCount = global::Data.Agent.Instance.Content.Gets<global::Data.Life>(l => !(l is global::Data.Player) && l.Config.Id == character.id && maps.Contains(l.Birthplace)).Count();
                    int shortage = targetCount - actualCount;
                    for (int i = 0; i < shortage; i++)
                    {
                        global::Data.Map map = Content.RandomGet<global::Data.Map>(m => m.Database.teleport == null);
                        int? level = character.minLevel.HasValue && character.maxLevel.HasValue 
                            ? Utils.Random.Instance.Next(character.minLevel.Value, character.maxLevel.Value + 1) 
                            : null;
                        map.Load<global::Data.Config.Life, global::Data.Life>(character.id, level).Birthplace = map;
                    }
                }
                else if (global::Data.Config.Agent.Instance.Content.Get<global::Data.Config.Ability>(c => c.Id == character.id) is global::Data.Config.Item)
                {
                    var maps = Content.Gets<global::Data.Map>(m => m.Database.teleport == null);
                    int targetCount = character.minCount.HasValue && character.maxCount.HasValue 
                        ? Utils.Random.Instance.Next(character.minCount.Value, character.maxCount.Value + 1)
                        : character.count;
                    int actualCount = maps.SelectMany(m => m.Content.Gets<global::Data.Item>(i => i.Config.Id == character.id)).Sum(i => i.Count);
                    int shortage = targetCount - actualCount;
                    for (int i = 0; i < shortage; i++)
                    {
                        global::Data.Map map = Content.RandomGet<global::Data.Map>(m => m.Database.teleport == null);
                        var existing = map.Content.Get<global::Data.Item>(it => it.Config.Id == character.id);
                        if (existing != null)
                        {
                            existing.Count += 1;
                        }
                        else
                        {
                            map.Load<global::Data.Config.Item, global::Data.Item>(character.id, 1);
                        }
                    }
                }
            }
        }


    }
}