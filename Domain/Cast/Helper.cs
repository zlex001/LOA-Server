using Logic;
using System.Linq;

namespace Domain.Cast
{
    internal static class Helper
    {
        internal static double CalculateDamage(double atk, double def, double fix, Movement movement, Part part)
        {
            double ratio = Utils.Mathematics.Ratio(atk, def, fix);
            double baseDamage = fix * atk * ratio;
            
            double damage = ApplyDamageModifier(baseDamage, movement, part);
            
            double fluctuation = Utils.Mathematics.Floatation(damage, 10);
            return System.Math.Max(1, fluctuation);
        }

        private static double ApplyDamageModifier(double baseDamage, Movement movement, Part part)
        {
            if (movement == null || movement.HitCount <= 1)
            {
                return baseDamage;
            }

            switch (movement.DamageModifier)
            {
                case Movement.DamageModifierType.None:
                    return baseDamage;

                case Movement.DamageModifierType.Decay:
                    int hitIndex = movement.GetPartHitIndex(part);
                    double decayFactor = System.Math.Pow(movement.DamageModifierValue, hitIndex + 1);
                    return baseDamage * decayFactor;

                case Movement.DamageModifierType.Ratio:
                    return baseDamage * movement.DamageModifierValue;

                case Movement.DamageModifierType.Total:
                    double totalDamage = baseDamage * movement.DamageModifierValue;
                    return totalDamage / movement.HitCount;

                default:
                    return baseDamage;
            }
        }

        internal static void BrokePart(Part part)
        {
            if (part == null) return;
            var life = part.Parent as Logic.Life;
            if (life == null) return;
            if (life.Config == null) return;
            if (life.Map == null) return;

            var dismemberTag = Utils.Tag.GetValue(life.Config.Tags, "Dismember");
            if (string.IsNullOrEmpty(dismemberTag)) return;

            var materials = dismemberTag.Split(';');
            foreach (var material in materials)
            {
                var parts = material.Split(':');
                if (parts.Length < 2) continue;

                string materialIdStr = parts[0].Trim();
                string dropConfig = parts[1].Trim();

                if (!int.TryParse(materialIdStr, out int materialId)) continue;
                if (!dropConfig.Contains('%') || !dropConfig.Contains('×')) continue;

                var percentIndex = dropConfig.IndexOf('%');
                var multiplyIndex = dropConfig.IndexOf('×');

                string probStr = dropConfig.Substring(0, percentIndex);
                if (!double.TryParse(probStr, out double probability)) continue;

                probability /= 100.0;
                double roll = Utils.Random.Instance.NextDouble();
                if (roll > probability) continue;

                string countRange = dropConfig.Substring(multiplyIndex + 1);
                int dropCount;

                if (countRange.Contains('~'))
                {
                    var rangeParts = countRange.Split('~');
                    if (rangeParts.Length == 2 &&
                        int.TryParse(rangeParts[0], out int min) &&
                        int.TryParse(rangeParts[1], out int max))
                    {
                        dropCount = Utils.Random.Instance.Next(min, max + 1);
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    if (!int.TryParse(countRange, out dropCount)) continue;
                }

                var item = life.Load<Logic.Config.Item, Item>(materialId, dropCount);
                Exchange.Receive.Do(life.Map, item, dropCount);
            }

            life.Remove(part);
            Broadcast.Instance.Local(life, [Text.Agent.Instance.Id(Logic.Text.Labels.PartBroken)], ("sub", life), ("part", part));
        }

        internal static void ApplyDamageToLife(Life sub, Life obj, Part part, double damagement)
        {
            if (obj.State.Is(Life.States.Unconscious))
            {
                if (part.data.IsEmpty<int>(Part.Data.Hp))
                {
                    BrokePart(part);
                }
                else
                {
                    part.Hp -= (int)damagement;
                }
            }
            else
            {
                if (part.data.IsEmpty<int>(Part.Data.Hp))
                {
                    if (Utils.Mathematics.Probability((int)damagement, part.data.GetMax<int>(Part.Data.Hp)))
                    {
                        BrokePart(part);
                    }
                    else
                    {
                        part.Hp -= (int)damagement;
                    }
                }
                else
                {
                    Broadcast.Instance.Battle(obj, [Text.Agent.Instance.Id(Logic.Text.Labels.Damage)], ("obj", obj), ("part", part), ("damagement", $"{damagement:F0}"));
                    part.Hp -= (int)damagement;
                }
            }
        }

        internal static void ApplyDamageToItem(Life sub, Item item)
        {
            int damage = 1;
            item.Durability -= damage;
            
            ProcessDropOnAttack(sub, item);

            if (item.Durability <= 0)
            {
                if (item.Count > 1)
                {
                    item.Count -= 1;
                    var newItem = item.Load<Logic.Config.Item, Item>(item.Config.Id, item.Count);
                    newItem.Durability = newItem.MaxDurability;
                    
                    if (item.Parent is Logic.Map map)
                    {
                        Exchange.Receive.Do(map, newItem, newItem.Count);
                    }
                    else if (item.Parent is Life owner)
                    {
                        Exchange.Receive.Do(owner, newItem, newItem.Count);
                    }
                    else if (item.Parent is Item container)
                    {
                        Exchange.Receive.Do(container, newItem, newItem.Count);
                    }

                    item.Count = 1;
                    DestroyItem(sub, item);
                }
                else
                {
                    DestroyItem(sub, item);
                }
            }
        }
        
        private static void ProcessDropOnAttack(Life sub, Item item)
        {
            var dropTags = Utils.Tag.GetValues(item.Config.Tags, "Drop");
            if (!dropTags.Any()) return;
            
            Logic.Map targetMap = item.Map ?? sub.Map;
            if (targetMap == null) return;
            
            foreach (var dropTag in dropTags)
            {
                var parts = dropTag.Split(':');
                if (parts.Length < 2) continue;
                
                string idStr = parts[0];
                string dropConfig = parts[1];
                
                if (!int.TryParse(idStr, out int itemId)) continue;
                
                if (!dropConfig.Contains('%') || !dropConfig.Contains('×')) continue;
                
                var percentIndex = dropConfig.IndexOf('%');
                var multiplyIndex = dropConfig.IndexOf('×');
                
                string probStr = dropConfig.Substring(0, percentIndex);
                if (!double.TryParse(probStr, out double probability)) continue;
                
                probability /= 100.0;
                double roll = Utils.Random.Instance.NextDouble();
                if (roll > probability) continue;
                
                string countRange = dropConfig.Substring(multiplyIndex + 1);
                int dropCount;
                
                if (countRange.Contains('~'))
                {
                    var rangeParts = countRange.Split('~');
                    if (rangeParts.Length == 2 && 
                        int.TryParse(rangeParts[0], out int min) && 
                        int.TryParse(rangeParts[1], out int max))
                    {
                        dropCount = Utils.Random.Instance.Next(min, max + 1);
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    if (!int.TryParse(countRange, out dropCount)) continue;
                }
                
                var materialConfig = Logic.Config.Agent.Instance.Content.Get<Logic.Config.Item>(c => c.Id == itemId);
                if (materialConfig != null)
                {
                    var tempItem = sub.Hand.Create<Item>(materialConfig, dropCount);
                    Exchange.Receive.Do(targetMap, tempItem, dropCount);
                }
            }
        }

        private static void DestroyItem(Life sub, Item item)
        {
            Broadcast.Instance.Local(item, [Text.Agent.Instance.Id(Logic.Text.Labels.Destroy)], ("sub", sub), ("item", item));
            DropContainerContents(item);
            item.Destroy();
        }

        private static void DropContainerContents(Item container)
        {
            if (!Exchange.Agent.IsContainer(container)) return;

            var contents = container.Content.Gets<Item>().ToList();
            if (!contents.Any()) return;

            Logic.Map targetMap = container.Map;
            if (targetMap == null)
            {
                if (container.Parent is Life life)
                {
                    targetMap = life.Map;
                }
                else if (container.Parent is Item parentContainer)
                {
                    targetMap = parentContainer.Map;
                }
            }

            if (targetMap == null) return;

            foreach (var content in contents)
            {
                Exchange.Receive.Do(targetMap, content, content.Count);
            }
        }
    }
}
