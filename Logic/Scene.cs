using Basic;
using Org.BouncyCastle.Asn1.Pkcs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Logic
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
            Config = Logic.Config.Agent.Instance.Content.Get<Config.Scene>(m => m.Id == Database.id);
            Type = Enum.TryParse<Types>(Config.Type, true, out var type) ? type : Types.Default;
            foreach (Logic.Database.Map database in Database.maps)
            {
                Map map = Create<Map>(database);
            }
        }
        public void UpdateLife()
        {
            foreach (Logic.Config.Map config in Content.Gets<Logic.Map>().Select(m => m.Config).Distinct())
            {
                foreach (var character in config.Characters)
                {
                    if (Logic.Config.Agent.Instance.Content.Get<Logic.Config.Ability>(c => c.Id == character.id) is Logic.Config.Life)
                    {
                        GenerateLife(config, character);
                    }
                }
            }
        }
        private void GenerateLife(Logic.Config.Map config, (int id, int count, int? minCount, int? maxCount, int? minLevel, int? maxLevel, double probability) character)
        {
            foreach (Logic.Map map in Content.Gets<Logic.Map>(m => m.Config == config && m.Database.teleport == null))
            {
                if (Utils.Random.Instance.NextDouble() > character.probability) continue;
                
                int targetCount = character.minCount.HasValue && character.maxCount.HasValue 
                    ? Utils.Random.Instance.Next(character.minCount.Value, character.maxCount.Value + 1)
                    : character.count;
                
                int currentCount = Logic.Agent.Instance.Content.Gets<Logic.Life>(l => !(l is Logic.Player) && l.Config.Id == character.id && l.Birthplace == map).Count();
                int shortage = targetCount - currentCount;
                
                for (int i = 0; i < shortage; i++)
                {
                    int? level = character.minLevel.HasValue && character.maxLevel.HasValue 
                        ? Utils.Random.Instance.Next(character.minLevel.Value, character.maxLevel.Value + 1) 
                        : null;
                    map.Load<Logic.Config.Life, Logic.Life>(character.id, level).Birthplace = map;
                }
            }
        }
        public void InitializeCharacters()
        {
            foreach (Logic.Config.Map config in Content.Gets<Logic.Map>().Select(m => m.Config).Distinct())
            {
                foreach (var character in config.Characters)
                {
                    if (Logic.Config.Agent.Instance.Content.Get<Logic.Config.Ability>(c => c.Id == character.id) is Logic.Config.Life)
                    {
                        GenerateLife(config, character);
                    }
                    else if (Logic.Config.Agent.Instance.Content.Get<Logic.Config.Ability>(c => c.Id == character.id) is Logic.Config.Item)
                    {
                        foreach (Logic.Map map in Content.Gets<Logic.Map>(m => m.Config == config && m.Database.teleport == null))
                        {
                            if (Utils.Random.Instance.NextDouble() > character.probability) continue;
                            
                            int targetCount = character.minCount.HasValue && character.maxCount.HasValue 
                                ? Utils.Random.Instance.Next(character.minCount.Value, character.maxCount.Value + 1)
                                : character.count;
                            
                            int currentCount = map.Content.Gets<Logic.Item>(i => i.Config.Id == character.id).Sum(i => i.Count);
                            int shortage = targetCount - currentCount;
                            
                            for (int i = 0; i < shortage; i++)
                            {
                                var existing = map.Content.Get<Logic.Item>(it => it.Config.Id == character.id);
                                if (existing != null)
                                {
                                    existing.Count += 1;
                                }
                                else
                                {
                                    map.Load<Logic.Config.Item, Logic.Item>(character.id, 1);
                                }
                            }
                        }
                    }
                }
            }

            foreach (var character in Config.Characters)
            {
                if (Utils.Random.Instance.NextDouble() > character.probability) continue;
                
                if (Logic.Config.Agent.Instance.Content.Get<Logic.Config.Ability>(c => c.Id == character.id) is Logic.Config.Life)
                {
                    var maps = Content.Gets<Logic.Map>(m => m.Database.teleport == null);
                    int targetCount = character.minCount.HasValue && character.maxCount.HasValue 
                        ? Utils.Random.Instance.Next(character.minCount.Value, character.maxCount.Value + 1)
                        : character.count;
                    int actualCount = Logic.Agent.Instance.Content.Gets<Logic.Life>(l => !(l is Logic.Player) && l.Config.Id == character.id && maps.Contains(l.Birthplace)).Count();
                    int shortage = targetCount - actualCount;
                    for (int i = 0; i < shortage; i++)
                    {
                        Logic.Map map = Content.RandomGet<Logic.Map>(m => m.Database.teleport == null);
                        int? level = character.minLevel.HasValue && character.maxLevel.HasValue 
                            ? Utils.Random.Instance.Next(character.minLevel.Value, character.maxLevel.Value + 1) 
                            : null;
                        map.Load<Logic.Config.Life, Logic.Life>(character.id, level).Birthplace = map;
                    }
                }
                else if (Logic.Config.Agent.Instance.Content.Get<Logic.Config.Ability>(c => c.Id == character.id) is Logic.Config.Item)
                {
                    var maps = Content.Gets<Logic.Map>(m => m.Database.teleport == null);
                    int targetCount = character.minCount.HasValue && character.maxCount.HasValue 
                        ? Utils.Random.Instance.Next(character.minCount.Value, character.maxCount.Value + 1)
                        : character.count;
                    int actualCount = maps.SelectMany(m => m.Content.Gets<Logic.Item>(i => i.Config.Id == character.id)).Sum(i => i.Count);
                    int shortage = targetCount - actualCount;
                    for (int i = 0; i < shortage; i++)
                    {
                        Logic.Map map = Content.RandomGet<Logic.Map>(m => m.Database.teleport == null);
                        var existing = map.Content.Get<Logic.Item>(it => it.Config.Id == character.id);
                        if (existing != null)
                        {
                            existing.Count += 1;
                        }
                        else
                        {
                            map.Load<Logic.Config.Item, Logic.Item>(character.id, 1);
                        }
                    }
                }
            }
        }


    }
}