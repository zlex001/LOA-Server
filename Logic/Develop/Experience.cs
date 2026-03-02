using Data;

namespace Logic.Develop
{
    public static class Experience
    {
        public static int CalculateBattleExp(Life attacker, Life target)
        {
            if (attacker == null || target == null) return 0;

            int attackerLevel = attacker.Level;
            int targetLevel = target.Level;

            // ExpPerKill = CurrentLevel × ExpPerKillMultiplier (for same-level enemy)
            int baseExp = attackerLevel * global::Data.Constant.CharacterExpPerKillMultiplier;

            // Gaussian distribution for level difference
            // Center at attacker's level (same-level gives maximum exp)
            // Sigma controls level difference tolerance
            double center = attackerLevel;
            double sigma = 5.0;
            double amplitude = baseExp;

            int exp = (int)Utils.Mathematics.Gaussian(targetLevel, center, sigma, amplitude);

            // Apply monthly card exp bonus for players
            if (attacker is Player player)
            {
                double bonus = Subscription.Agent.GetExpBonus(player);
                exp = (int)(exp * bonus);
            }

            return Math.Max(exp, 1);
        }

        public static void GiveBattleExp(Life attacker, Life target)
        {
            if (attacker == null || target == null) return;

            int exp = CalculateBattleExp(attacker, target);
            attacker.Exp += exp;
        }
    }
}
