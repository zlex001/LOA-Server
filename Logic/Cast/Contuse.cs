using Data;

namespace Logic.Cast
{
    public static class Contuse
    {
        public static void Do(Life sub, Movement movement, Character obj, Part part)
        {
            if (obj is Life life)
            {
                DoLife(sub, movement, life, part);
            }
            else if (obj is Item item)
            {
                DoItem(sub, movement, item, part);
            }
        }

        private static void DoLife(Life sub, Movement movement, Life obj, Part part)
        {
            if (sub == null) return;
            if (movement == null) return;
            if (obj == null) return;
            if (part == null) return;

            var damagement = Helper.CalculateDamage(sub.Atk, obj.Def, 3, movement, part);
            Helper.ApplyDamageToLife(sub, obj, part, damagement);
            movement.IncrementPartHitIndex(part);
        }

        private static void DoItem(Life sub, Movement movement, Item item, Part part)
        {
            Helper.ApplyDamageToItem(sub, item);
        }
    }
}
