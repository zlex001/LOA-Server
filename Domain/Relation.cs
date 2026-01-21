using Logic;

namespace Domain
{
    public class Relation
    {
        public enum Reason
        {
            // 友好关系（正值）
            Help = 10,
            Gift = 15,
            Rescue = 20,
            Heal = 5,
            Trade = 3,

            // 敌对关系（负值）
            Theft = -10,
            Assault = -15,
            Robbery = -20,
            AssaultOfficer = -25,
            Murder = -30,
            JailBreak = -35,
            Kidnap = -40,
            PrisonBreak = -40,
            Hostile = -10
        }
        public static bool Can(Life source, Life target)
        {
            return source != null && target != null;
        }
        public static void Change(Life source, Life target, Reason reason)
        {
            double currentRelation = source.Relation.TryGetValue(target, out var relationValue) ? relationValue : 0;
            source.Relation[target] = currentRelation + (double)reason;
        }
        public static void Do(Life source, Life target, Reason reason)
        {
            if (Can(source, target))
            {
                Change(source, target, reason);
            }
        }
        public static void Interact(Life source, Life target, Reason reason)
        {
            Do(source, target, reason);
            Do(target, source, reason);
        }
    }
}