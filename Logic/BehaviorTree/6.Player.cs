using Data;
using System.Linq;

namespace Logic.BehaviorTree
{
    public class Player
    {
        [BehaviorCondition(0061002)]
        public static bool HasReachedClickTarget(Character character)
        {
            var player = character as global::Data.Player;
            if(player==null)return false;

            if (player.ClickTarget == null) return false;

            if (player.Map == player.ClickTarget)
            {
                return true;
            }

            if (player.ClickTarget.Database.teleport != null && player.Map.Database.pos != null)
            {
                if (player.Map.Database.pos.SequenceEqual(player.ClickTarget.Database.teleport))
                {
                    return true;
                }
            }

            return false;
        }

        [BehaviorAction(0062001)]
        public static bool AutoPathToClickTarget(Character character)
        {
            var player = character as global::Data.Player;
            if(player==null)return false;

            if (player.ClickTarget == null) return false;

            Logic.Move.Walk.FollowShortest(player, player.ClickTarget);
            return true;
        }

        [BehaviorAction(0062002)]
        public static bool ClearBehaviorTree(Character character)
        {
            var life = character as global::Data.Life;
            if(life==null)return false;

            Logic.State.Agent.StopBehaviorTree(life);
            life.BtRoot = null;
            return true;
        }

        [BehaviorCondition(0061003)]
        public static bool HasLeader(Character character)
        {
            return character is Life life && life.Leader != null;
        }

        [BehaviorCondition(0061004)]
        public static bool HasTracker(Character character)
        {
            return character is Life life && life.Tracker != null;
        }

        [BehaviorCondition(0061005)]
        public static bool HasReachedLeader(Character character)
        {
            if (character is not Life life) return false;
            if (life.Leader?.Map == null) return false;
            return life.Map == life.Leader.Map;
        }

        [BehaviorCondition(0061006)]
        public static bool HasReachedTracker(Character character)
        {
            if (character is not Life life) return false;
            if (life.Tracker?.Map == null) return false;
            return life.Map == life.Tracker.Map;
        }

        [BehaviorAction(0062003)]
        public static bool AutoPathToLeader(Character character)
        {
            if (character is not Life life) return false;
            if (life.Leader?.Map == null) return false;
            Logic.Move.Walk.FollowShortest(life, life.Leader.Map);
            return true;
        }

        [BehaviorAction(0062004)]
        public static bool AutoPathToTracker(Character character)
        {
            if (character is not Life life) return false;
            if (life.Tracker?.Map == null) return false;
            Logic.Move.Walk.FollowShortest(life, life.Tracker.Map);
            return true;
        }
    }
}

