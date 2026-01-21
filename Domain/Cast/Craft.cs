using Aop.Api.Domain;
using Logic;
using Utils;

namespace Domain.Cast
{
    public class Craft
    {
        public Craft(string type, Movement.Effect effect)
        {
            this.type = type;
            this.effect = effect;
            point = $"Craft:{type}";
            var requireds = new HashSet<string>();
            foreach (var config in Logic.Config.Agent.Instance.Content.Gets<Logic.Config.Item>())
            {
                if (config.Tags.HasPrefix(type))
                {
                    products.Add(config);
                    foreach (string require in config.Tags.GetValues(type))
                    {
                        requireds.Add(require);
                    }
                }
            }
            foreach (var config in Logic.Config.Agent.Instance.Content.Gets<Logic.Config.Item>())
            {

                foreach (var tag in config.Tags.GetIndividuals())
                {
                    if (requireds.Contains(tag))
                    {
                        materials.Add(config);
                        break;
                    }
                }
            }
            Agent.Instance.Register(effect, Do);
        }
        private readonly string type;
        private readonly string point;
        private readonly Movement.Effect effect;
        private List<Logic.Config.Item> materials = new List<Logic.Config.Item>();
        private List<Logic.Config.Item> products = new List<Logic.Config.Item>();

        private Dictionary<string, float> Requirement(Logic.Config.Item product)
        {
            var require = new Dictionary<string, float>();
            foreach (var weight in Utils.Mathematics.DescendingWeight(product.Tags.GetValues(type).ToList(), product.value))
            {
                if (require.TryGetValue(weight.Key, out var existing))
                {
                    require[weight.Key] = existing + weight.Value;
                }
                else
                {
                    require[weight.Key] = weight.Value;
                }
            }
            return require;
        }
        public bool IsMaterial(Item item)
        {
            if (materials == null) return false;
            return materials.Contains(item.Config);
        }
        public bool IsProduct(Item item)
        {
            if (products == null) return false;
            return products.Contains(item.Config);
        }
        public bool IsPoint(Character character)
        {
            if (character == null) return false;
            if (character is not Item item) return false;
            return item.Config.Tags.Contains(point);
        }
        public bool IsSkill(Skill skill)
        {
            if (skill == null) return false;
            foreach (Movement movement in skill.Content.Gets<Movement>())
            {
                if (IsMovement(movement))
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsMovement(Movement movement)
        {
            return movement.Effects.Contains(effect);
        }
        private void Do(Life sub, Movement movement, Character obj, Part part)
        {

            if (sub == null) return;
            if (movement == null) return;
            if (obj == null) return;
            if (part == null) return;
            if (!IsPoint(obj)) return;
            if (!IsMovement(movement)) return;
            if (products.Count == 0) return;
            
            var selected = products[Utils.Random.Instance.Next(products.Count)];
            
            var available = new Dictionary<string, float>();
            foreach (Item material in obj.Content.Gets<Item>())
            {
                foreach (var weight in Utils.Mathematics.DescendingWeight(material.Config.Tags.GetIndividuals().ToList(), material.Config.value))
                {
                    if (available.TryGetValue(weight.Key, out var existing))
                    {
                        available[weight.Key] = existing + weight.Value * material.Count;
                    }
                    else
                    {
                        available[weight.Key] = weight.Value * material.Count;
                    }
                }
            }
            
            var require = Requirement(selected);
            bool hasMaterials = true;
            foreach (var r in require)
            {
                if (!available.TryGetValue(r.Key, out float availValue) || availValue < r.Value)
                {
                    hasMaterials = false;
                    break;
                }
            }
            
            if (!hasMaterials) return;
            
            Skill skill = (Skill)movement.Parent;
            double successRate = Utils.Mathematics.Ratio(skill.Level, selected.value);
            bool success = Utils.Mathematics.Probability(successRate);
            
            if (!success)
            {
                return;
            }

            foreach (Item material in obj.Content.Gets<Item>(IsMaterial))
            {
                foreach (var tag in material.Config.Tags.GetIndividuals())
                {
                    if (require.TryGetValue(tag, out float needed) && needed > 0)
                    {
                        float consumed = Math.Min(needed, material.Count * material.Config.value);
                        int countToConsume = (int)Math.Ceiling(consumed / material.Config.value);
                        material.Count -= countToConsume;
                        require[tag] = needed - consumed;
                    }
                }
            }

            var product = obj.Load<Logic.Config.Item, Item>(selected.Id, 1);
            Broadcast.Instance.Local(obj, [Domain.Text.Agent.Instance.Id(Logic.Text.Labels.Craft)], ("sub", sub), ("product", product), ("point", point));
            Domain.Exchange.Pick.Do(sub, product, 1);
        }
        public List<Logic.Config.Item> Availables(Character point)
        {
            var result = new List<Logic.Config.Item>();
            
            var available = new Dictionary<string, float>();
            foreach (Item material in point.Content.Gets<Item>())
            {
                foreach (var weight in Utils.Mathematics.DescendingWeight(material.Config.Tags.GetIndividuals().ToList(), material.Config.value))
                {
                    if (available.TryGetValue(weight.Key, out var existing))
                    {
                        available[weight.Key] = existing + weight.Value * material.Count;
                    }
                    else
                    {
                        available[weight.Key] = weight.Value * material.Count;
                    }
                }
            }

            foreach (var product in products)
            {
                var require = Requirement(product);
                bool can = true;
                float totalAvailableValue = 0f;

                foreach (var r in require)
                {
                    if (!available.TryGetValue(r.Key, out float availValue) || availValue < r.Value)
                    {
                        can = false;
                        break;
                    }

                    totalAvailableValue += availValue;
                }

                if (can && totalAvailableValue >= product.value)
                {
                    result.Add(product);
                }
            }
            return result;
        }



    }
}
