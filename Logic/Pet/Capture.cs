using Data;
using Utils;

namespace Logic.Pet
{
    public class Capture
    {
        public static bool TryCapture(Player player, Life animal, Item item)
        {
            if (animal.Category != Life.Categories.Animal)
            {
                return false;
            }

            if (!IsFavoriteItem(animal, item))
            {
                Broadcast.Instance.Local(player, [Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.CaptureNotInterested)], 
                    ("sub", player), ("obj", animal), ("item", item));
                return true;
            }

            int maxSlots = player.Database.record.TryGetValue("CompanionSlots", out int slots) ? slots : 1;
            int currentCount = player.Database.companions.Count(c => c.Source == "Capture");
            if (currentCount >= maxSlots)
            {
                Broadcast.Instance.Local(player, [Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.CompanionSlotFull)], 
                    ("sub", player), ("obj", animal));
                return true;
            }

            var huntSkill = GetHuntSkill(player);
            if (huntSkill == null)
            {
                Broadcast.Instance.Local(player, [Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.CaptureNeedSkill)], 
                    ("sub", player), ("obj", animal));
                return true;
            }

            double successRate = CalculateSuccessRate(huntSkill.Level, animal.Level);
            bool success = Utils.Random.Instance.NextDouble() < successRate;

            item.Count -= 1;

            if (success)
            {
                player.Database.companions.Add(new global::Data.Database.Companion
                {
                    LifeConfigId = animal.Config.Id,
                    Level = animal.Level,
                    Source = "Capture",
                    ExpireTime = null
                });

                animal.Leader = player;

                Logic.Relation.Do(player, animal, Relation.Reason.Help);
                Logic.Relation.Do(animal, player, Relation.Reason.Help);

                Broadcast.Instance.Local(player, [Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.CaptureSuccess)], 
                    ("sub", player), ("obj", animal));
            }
            else
            {
                Broadcast.Instance.Local(player, [Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.CaptureFail)], 
                    ("sub", player), ("obj", animal), ("item", item));
            }

            return true;
        }

        private static bool IsFavoriteItem(Life animal, Item item)
        {
            var favoriteValues = animal.Config.Tags.GetValues("Favorite");
            if (favoriteValues == null || favoriteValues.Count == 0)
                return false;

            var itemTags = item.Config.Tags;
            if (itemTags == null || itemTags.Count == 0)
                return false;

            foreach (var favorite in favoriteValues)
            {
                if (itemTags.Contains(favorite))
                    return true;
                
                var cookingValues = itemTags.GetValues("Cook");
                if (cookingValues != null && cookingValues.Contains(favorite))
                    return true;
            }

            return false;
        }

        private static Skill GetHuntSkill(Player player)
        {
            return player.GetAllSkills()
                .FirstOrDefault(s => s.Content.Gets<Movement>()
                .Any(m => m.Effects.Contains(Movement.Effect.Hunt)));
        }

        private static double CalculateSuccessRate(int skillLevel, int animalLevel)
        {
            double baseRate = Utils.Mathematics.Ratio(skillLevel, animalLevel, 1.0);
            return baseRate;
        }
    }
}

