using Data;

namespace Logic.Cast
{
    public static class Steal
    {
        public static void Do(Life sub, Movement movement, Character obj, Part part)
        {
            if (sub == null) return;
            if (obj == null) return;
            if (part == null) return;
            if (movement == null) return;
            if (!movement.Effects.Contains(Movement.Effect.Steal)) return;
            Skill skill = sub.Content.Gets<Skill>(IsSkill).OrderByDescending(s => s.Level).FirstOrDefault();
            if (skill == null) return;

            sub.Load<global::Data.Config.Buff, global::Data.Buff>(global::Data.Constant.ConcealmentBuff, (double)skill.Level, (double)skill.Level);
        }

        public static bool Probability(Life sub, Item item, int count)
        {
            if (sub == null || item == null) return false;

            double lightFactor = LightFactor();
            if (lightFactor == 0) return false;

            double concealmentValue = Buff.Agent.GetConcealmentValue(sub);
            double totalValue = item.Config.value * count;

            double successRate = lightFactor * Utils.Mathematics.Ratio(concealmentValue, totalValue);
            return Utils.Mathematics.Probability(successRate);
        }

        public static double LightFactor()
        {
            Time.Agent.Period period = Logic.Time.Agent.Instance.Current.Period;

            return period switch
            {
                Time.Agent.Period.Afternoon => 0,
                Time.Agent.Period.Night => 2,
                _ => 1
            };
        }

        public static bool IsSkill(Skill skill)
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

        public static bool IsMovement(Movement movement)
        {
            return movement.Effects.Contains(Movement.Effect.Steal);
        }
    }
}
