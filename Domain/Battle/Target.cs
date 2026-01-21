using Logic;

namespace Domain.Battle
{
    public static class Target
    {
        public static List<Character> Get(Life life)
        {
            var targets = new List<Character>();
            if (life == null || life.State.Is(Logic.Life.States.Unconscious))
            {
                return targets;
            }
            if (life.Map == null)
            {
                return targets;
            }

            var invalidTargets = new List<Character>();

            foreach (var kvp in life.Relation.Where(kvp => kvp.Value < 0))
            {
                if (kvp.Key is Life target)
                {
                    if (!target.State.Is(Logic.Life.States.Unconscious) && target.Map != null && target.Map == life.Map)
                    {
                        targets.Add(target);
                    }
                    else
                    {
                        invalidTargets.Add(target);
                    }
                }
            }

            foreach (var invalid in invalidTargets)
            {
                life.Relation.Remove(invalid);
            }

            targets = targets.OrderBy(t => life.Relation.TryGetValue(t, out var v) ? v : 0).ToList();

            return targets;
        }

        public static Part Aim(Movement movement, Life obj)
        {
            if (obj == null) return null;
            if (movement == null) return null;
            if (movement.Config.target == null) return null;

            bool IsTargetPart(Part p) => movement.Config.target.Contains(p.Type);
            bool IsValidTargetPart(Part p) => IsTargetPart(p) && p.Hp > 0;

            if (!obj.Content.Has<Part>(IsTargetPart)) return null;

            return obj.Content.Has<Part>(IsValidTargetPart) ? obj.Content.RandomGet<Part>(IsValidTargetPart) : obj.Content.RandomGet<Part>(IsTargetPart);
        }
    }
}

