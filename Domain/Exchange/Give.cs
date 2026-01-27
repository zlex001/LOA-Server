using Logic;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;

namespace Domain.Exchange
{
    public class Give
    {
        public static bool Can(Life sub, Ability obj)
        {
            if (sub == null) return false;
            if (obj == null) return false;
            if (sub == obj) return false;
            if (obj is Life life && sub != life)
            {
                return Availability(sub).Count > 0 || HasCarriedLife(sub);
            }

            if (obj is Item item && Agent.IsContainer(item))
            {
                return HasCarriedLife(sub);
            }

            return false;
        }

        private static bool HasCarriedLife(Life life)
        {
            return life.Map != null && life.Map.Content.Gets<Life>().Any(l => l.Bearer == life && l != life);
        }

        public static List<Character> Availability(Life life)
        {
            if (life == null) return null;

            List<Character> results = new List<Character>();
            if (Agent.Cargor(life) != null)
            {
                results.Add(Agent.Cargor(life));
            }
            if (Agent.GetHandleItem(life) != null)
            {
                results.Add(Agent.GetHandleItem(life));
            }
            foreach (Item item in Agent.GetItemsInBag(life))
            {
                if (!item.Content.Has<Item>())
                {
                    results.Add(item);
                }
            }
            return results;
        }
        private static bool Can(Life sub, Character obj, Character target, int count = 1)
        {
            if (sub == null) return false;
            if (obj == null) return false;
            if (target == null) return false;
            if (!Availability(sub).Contains(target)) return false;
            return true;
        }
        public static void Do(Life sub, Life obj, Life target)
        {
            if (Can(sub, obj, target))
            {
                Broadcast.Instance.Local(obj, [Domain.Text.Agent.Instance.Id(Logic.Text.Labels.GiveLifeToLife)], ("sub", sub), ("item", target), ("obj", obj));
                Receive.Do(obj, target);
            }
        }
        public static void Do(Life sub, Life obj, Item target, int count)
        {
            if (Can(sub, obj, target, count))
            {
                // 检查是否为宠物捕捉场景
                if (sub is Player player && obj.Category == Life.Categories.Animal)
                {
                    // 尝试捕捉流程，如果触发了捕捉逻辑则直接返回
                    if (Domain.Pet.Capture.TryCapture(player, obj, target))
                    {
                        return;
                    }
                }

                // 普通给予流程
                Broadcast.Instance.Local(obj, [Domain.Text.Agent.Instance.Id(Logic.Text.Labels.GiveItemToLife)], ("sub", sub), ("target", target), ("obj", obj), ("count", count.ToString()));
                Receive.Do(obj, target, count);
            }
        }
        public static void Do(Life sub, Item obj, Life target)
        {
            if (Can(sub, obj, target))
            {
                Broadcast.Instance.Local(obj, [Domain.Text.Agent.Instance.Id(Logic.Text.Labels.GiveLifeToItem)], ("sub", sub), ("item", target), ("obj", obj));
                Receive.Do(obj, target);
            }
        }
        public static void Do(Life sub, Item obj, Item target, int count)
        {
            if (Can(sub, obj, target, count))
            {
                Broadcast.Instance.Local(obj, [Domain.Text.Agent.Instance.Id(Logic.Text.Labels.GiveItemToItem)], ("sub", sub), ("target", target), ("obj", obj), ("count", count.ToString()));
                Receive.Do(obj, target, count);
                
                // Check for tutorial stele interaction (obj is the container receiving items, target is the given item)
                if (sub is Player player && obj.Config?.Tags != null && obj.Config.Tags.Contains("Tutorial:Stele"))
                {
                    Tutorial.Instance.OnGiveToStele(player, obj, target);
                }
            }
        }

        public static bool ShouldShowItem(Option option, Item item, string operationType)
        {
            var filter = option.Setting?.Filter?.ToLower();
            var toggles = option.Setting?.ToggleGroup;
            var category = item.Type.ToString();
            bool toggleOk = toggles != null && ((toggles.TryGetValue("", out bool all) && all) || (toggles.TryGetValue(category, out bool match) && match));
            if (!toggleOk) return false;
            return string.IsNullOrEmpty(filter) || Domain.Text.Name.Item(item, option.Player).ToLower().Contains(filter);
        }


    }
}