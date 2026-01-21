using Logic;
using Utils;

namespace Domain.Time
{
    public class Daily
    {
        public static Daily Instance => instance ??= new Daily();
        private static Daily instance;


        public bool IsShopOpen(Agent.ShopType type) => type switch
        {
            Agent.ShopType.General => Agent.Instance.Current.Period != Agent.Period.Night,
            Agent.ShopType.Tavern => Agent.Instance.Current.Period is Agent.Period.Evening or Agent.Period.Night,
            Agent.ShopType.NightMarket => Agent.Instance.Current.Period == Agent.Period.Night,
            _ => true
        };

        public void OnPeriodChanged(Agent.Period oldPeriod)
        {
            var currentPeriod = Agent.Instance.Current.Period;

            if (oldPeriod == Agent.Period.Night && currentPeriod == Agent.Period.Dawn)
            {
                foreach (Player player in Logic.Agent.Instance.Content.Gets<Player>())
                {
                    Domain.Broadcast.Instance.System(player, new object[] { Domain.Text.Agent.Instance.Id(Logic.Text.Labels.Sunrise) });
                }
            }
            else if (oldPeriod == Agent.Period.Evening && currentPeriod == Agent.Period.Night)
            {
                foreach (Player player in Logic.Agent.Instance.Content.Gets<Player>())
                {
                    Domain.Broadcast.Instance.System(player, new object[] { Domain.Text.Agent.Instance.Id(Logic.Text.Labels.Sunset) });
                }
            }

            Lighting.Instance.UpdateAllPlayers();
        }

        public void Update(int oldDay)
        {
            EconomyMonitor.Instance.OnDayChanged(oldDay);
            
            UpdateAge();
            UpdateInjury();
            NPC();
            Resource();
        }

        private void UpdateInjury()
        {
            foreach (var scene in Logic.Agent.Instance.Content.Gets<Logic.Scene>())
            {
                foreach (var map in scene.Content.Gets<Logic.Map>())
                {
                    foreach (var life in map.Content.Gets<Logic.Life>())
                    {
                        if (life.Injury > 0)
                        {
                            life.Injury -= 1;
                        }
                    }
                }
            }
        }

        private void UpdateAge()
        {
            foreach (Player player in Logic.Agent.Instance.Content.Gets<Player>())
            {
                player.Age += 1.0;
            }
            
            foreach (var scene in Logic.Agent.Instance.Content.Gets<Logic.Scene>())
            {
                foreach (var map in scene.Content.Gets<Logic.Map>())
                {
                    foreach (var life in map.Content.Gets<Logic.Life>().Where(l => !(l is Player)))
                    {
                        life.Age += 1.0;
                    }
                }
            }
        }



        private void NPC()
        {
            foreach (var scene in Logic.Agent.Instance.Content.Gets<Logic.Scene>(s => s is not Maze))
            {
                scene.UpdateLife();
            }
        }


        public void InitializeAllScenesNPCs()
        {
            var scenes = Logic.Agent.Instance.Content.Gets<Logic.Scene>().ToList();
            
            foreach (var scene in scenes)
            {
                scene.InitializeCharacters();
            }
            
            Resource();
        }




        private void Resource()
        {
            int resourcePointCount = 0;
            int resourceGeneratedCount = 0;
            var rng = new System.Random();

            foreach (var scene in Logic.Agent.Instance.Content.Gets<Logic.Scene>())
            {
                foreach (var map in scene.Content.Gets<Map>())
                {
                    foreach (var item in map.Content.Gets<Item>())
                    {
                        if (item?.Config?.Tags != null && Utils.Tag.HasPrefix(item.Config.Tags, "Generate"))
                        {
                            resourcePointCount++;

                            string generateTag = item.Config.Tags.GetValue("Generate");
                            if (!string.IsNullOrEmpty(generateTag))
                            {
                                var materialConfigs = new List<Logic.Config.Item>(8);
                                var weights = new List<float>(8);
                                const int BaseWeightValue = 50;

                                var parts = generateTag.Split(',');
                                for (int i = 0; i < parts.Length; i++)
                                {
                                    string s = parts[i].Trim();
                                    int id;
                                    if (s.Length > 0 && int.TryParse(s, out id))
                                    {
                                        var cfg = Logic.Config.Agent.Instance.Content.Get<Logic.Config.Item>(x => x.Id == id);
                                        if (cfg != null)
                                        {
                                            materialConfigs.Add(cfg);
                                            float w = 1000f / (cfg.value + BaseWeightValue);
                                            weights.Add(w);
                                        }
                                    }
                                }

                                if (materialConfigs.Count > 0)
                                {
                                    int currentTotalValue = 0;
                                    foreach (var child in item.Content.Gets<Item>())
                                    {
                                        currentTotalValue += child.Config.value * child.Count;
                                    }

                                    int maxCapacity = item.Config.value;
                                    if (currentTotalValue < maxCapacity)
                                    {
                                        float totalWeight = 0f;
                                        for (int i = 0; i < weights.Count; i++) totalWeight += weights[i];

                                        if (totalWeight > 0f)
                                        {
                                            float r = (float)(rng.NextDouble() * totalWeight);
                                            float acc = 0f;
                                            Logic.Config.Item selected = null;

                                            for (int i = 0; i < materialConfigs.Count; i++)
                                            {
                                                acc += weights[i];
                                                if (r <= acc)
                                                {
                                                    selected = materialConfigs[i];
                                                    break;
                                                }
                                            }

                                            if (selected == null && materialConfigs.Count > 0)
                                            {
                                                selected = materialConfigs[materialConfigs.Count - 1];
                                            }

                                            if (selected != null)
                                            {
                                                int v = selected.value;
                                                if (currentTotalValue + v <= maxCapacity)
                                                {
                                                    var existing = item.Content.Get<Item>(it => it.Config.Id == selected.Id);
                                                    if (existing != null)
                                                    {
                                                        existing.Count += 1;
                                                    }
                                                    else
                                                    {
                                                        int count = Utils.Random.Range(1, 10);
                                                        var created = item.Create<Item>(selected, count);
                                                        created.CreateTime = DateTime.Now;
                                                    }
                                                    resourceGeneratedCount++;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

    }
}
