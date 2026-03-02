using Data;

namespace Logic.BehaviorTree
{
    public class General
    {
        [BehaviorCondition(0001001)]
        public static bool MinimalProbability(Character character)
        {
            return Utils.Random.Instance.NextDouble() < 0.05;
        }

        [BehaviorCondition(0001002)]
        public static bool IsAtHome(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            if (life.Birthplace == null) return false;
            if (life.Birthplace != life.Map) return false;
            return true;
        }

        [BehaviorAction(0002001)]
        public static bool GoHome(Character character)
        {
            var life = character as Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            if (life.Birthplace == null) return false;
            Logic.Move.Walk.FollowShortest(life, life.Birthplace);
            return false;
        }
    }
}

