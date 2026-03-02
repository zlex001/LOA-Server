using Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.BehaviorTree
{
    /// <summary>
    /// 治安业务类别 - 对应005xxxx系列节点
    /// 包含缉杀、关押、缉拿、巡逻等治安相关的行为树节点
    /// </summary>
    public class Security
    {
        /// <summary>
        /// Condition:0051001 - 缉杀目标是否存在
        /// 动态查找缉杀目标（犯罪者）
        /// </summary>
        [BehaviorCondition(0051001)]
        public static bool HasKillTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null)
                return false;
            if (life.Map == null)
                return false;
            if (life.Map.Scene == null)
                return false;
            return global::Data.Agent.Instance.Content.Has<Life>(l => l != life && !l.State.Is(global::Data.Life.States.Unconscious) && l.Content.Has<Punishment>() && l.Map?.Scene == life.Birthplace?.Scene);

        }

        /// <summary>
        /// Condition:0051002 - 缉杀目标是否相邻
        /// 动态查找并检查缉杀目标是否在当前地图中
        /// </summary>
        [BehaviorCondition(0051002)]
        public static bool IsKillTargetAdjacent(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null)
                return false;
            if (life.Map == null)
                return false;
            return life.Map.Content.Has<Life>(l => l != life && !l.State.Is(global::Data.Life.States.Unconscious) && l.Content.Has<Punishment>());
        }

        /// <summary>
        /// Condition:0051003 - 关押目标是否存在
        /// 动态查找关押目标
        /// </summary>
        [BehaviorCondition(0051003)]
        public static bool HasDetainTarget(Character character)
        {
            var life = character as global::Data.Life;
            var cargor = Logic.Exchange.Agent.Cargor(life);
            return cargor != null;
        }

        /// <summary>
        /// Condition:0051004 - 关押目标是否相邻
        /// 动态查找并检查关押目标是否在当前地图中
        /// </summary>
        [BehaviorCondition(0051004)]
        public static bool IsDetainTargetAdjacent(Character character)
        {
            var life = character as global::Data.Life;
            if (life?.Map == null)
                return false;
            Life cargor = Logic.Exchange.Agent.Cargor(life);
            if (cargor == null)
                return false;
            return life.Map.Content.Has<Item>(i => Logic.Move.Enter.Can(cargor, i));
        }

        /// <summary>
        /// Condition:0051005 - 缉拿目标是否存在
        /// 动态查找缉拿目标
        /// </summary>
        [BehaviorCondition(0051005)]
        public static bool HasCaptureTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null)
                return false;
            if (life.Map == null)
                return false;
            if (life.Map.Scene == null)
                return false;
            return global::Data.Agent.Instance.Content.Has<Life>(l => l != life && l.Content.Has<Punishment>() && l.State.Is(Life.States.Unconscious) && l.Map?.Scene == life.Birthplace?.Scene && l.Bearer == null);
        }

        /// <summary>
        /// Condition:0051006 - 缉拿目标是否相邻
        /// 动态查找并检查缉拿目标是否在当前地图中
        /// </summary>
        [BehaviorCondition(0051006)]
        public static bool IsCaptureTargetAdjacent(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null)
                return false;
            if (life.Map == null)
                return false;
            return life.Map.Content.Has<Life>(l => l != life && l.Content.Has<Punishment>() && l.State.Is(Life.States.Unconscious) && l.Bearer == null);
        }

        /// <summary>
        /// Action:0052001 - 缉杀
        /// 动态查找缉杀目标并执行缉杀
        /// </summary>
        [BehaviorAction(0052001)]
        public static bool HuntAndKillTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null)
                return false;
            if (life.Map == null)
                return false;
            if (!life.Map.Content.Has<Life>(l => l != life && !l.State.Is(global::Data.Life.States.Unconscious) && l.Content.Has<Punishment>() && l.Map?.Scene == life.Birthplace?.Scene))
                return false;
            Life target = life.Map.Content.Get<Life>(l => l != life && !l.State.Is(global::Data.Life.States.Unconscious) && l.Content.Has<Punishment>() && l.Map?.Scene == life.Birthplace?.Scene);
            Logic.Battle.Agent.Instance.Hostile(life, target);
            return true;
        }

        /// <summary>
        /// Action:0052002 - 自动寻路缉杀目标
        /// 动态查找缉杀目标并自动寻路
        /// </summary>
        [BehaviorAction(0052002)]
        public static bool AutoPathToKillTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null)
                return false;
            if (life.Map == null)
                return false;
            if (life.Map.Scene == null)
                return false;
            
            Life target = global::Data.Agent.Instance.Content.Gets<Life>()
                .Where(l => l != life)
                .Where(l => !l.State.Is(global::Data.Life.States.Unconscious))
                .Where(l => l.Content.Has<Punishment>())
                .Where(l => l.Map?.Scene == life.Birthplace?.Scene)
                .FirstOrDefault();
            
            if (target == null)
                return false;
            
            Logic.Move.Walk.FollowShortest(life, target.Map);
            return true;
        }

        /// <summary>
        /// Action:0052003 - 巡逻
        /// 执行巡逻行为，在地图间随机移动
        /// </summary>
        [BehaviorAction(0052003)]
        public static bool Patrol(Character character)
        {
            var life = character as Life;
            if (life?.Map?.Scene == null)
                return false;

            var destination = life.Map.Scene.Content.RandomGet<Map>(m => Logic.Move.Walk.Can(life, m));
            if (destination == null)
                return false;

            Logic.Move.Walk.Do(life, destination);
            return true;
        }

        /// <summary>
        /// Action:0052004 - 关押
        /// 动态查找关押目标并执行关押
        /// </summary>
        [BehaviorAction(0052004)]
        public static bool DetainTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life?.Map == null)
                return false;
            Life cargor = Logic.Exchange.Agent.Cargor(life);
            if (cargor == null)
                return false;
            if (!life.Map.Content.Has<Item>(i => Logic.Move.Enter.Can(cargor, i)))
                return false;
            Item jail = life.Map.Content.Get<Item>(i => Logic.Move.Enter.Can(cargor, i));
            Logic.Move.Enter.Do(cargor, jail);
            return true;
        }

        /// <summary>
        /// Action:0052005 - 自动寻路关押目标
        /// 动态查找关押目标并自动寻路
        /// </summary>
        [BehaviorAction(0052005)]
        public static bool AutoPathToDetainTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life?.Map == null)
                return false;
            Life cargor = Logic.Exchange.Agent.Cargor(life);
            if (cargor == null)
                return false;
            Item jail = global::Data.Agent.Instance.Content.Get<Item>(i => i.Map?.Scene == life.Birthplace?.Scene && Logic.Move.Enter.Can(cargor, i));
            if (jail == null)
                return false;
            Logic.Move.Walk.FollowShortest(life, jail.Map);
            return true;
        }


        /// <summary>
        /// Action:0052006 - 缉拿
        /// 动态查找缉拿目标并执行缉拿
        /// </summary>
        [BehaviorAction(0052006)]
        public static bool CaptureTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life?.Map == null)
                return false;
            if (!life.Map.Content.Has<Life>(l => l != life && l.Content.Has<Punishment>() && l.State.Is(Life.States.Unconscious) && l.Bearer == null))
                return false;
            Life criminal = life.Map.Content.Get<Life>(l => l != life && l.Content.Has<Punishment>() && l.State.Is(Life.States.Unconscious) && l.Bearer == null);
            Logic.Exchange.Pick.Do(life, criminal);
            return true;
        }

        /// <summary>
        /// Action:0052007 - 自动寻路缉拿目标
        /// 动态查找缉拿目标并自动寻路
        /// </summary>
        [BehaviorAction(0052007)]
        public static bool AutoPathToCaptureTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null)
                return false;
            if (life.Map == null)
                return false;
            if (life.Map.Scene == null)
                return false;
            
            Life target = global::Data.Agent.Instance.Content.Gets<Life>()
                .Where(l => l != life)
                .Where(l => l.Content.Has<Punishment>())
                .Where(l => l.State.Is(Life.States.Unconscious))
                .Where(l => l.Map?.Scene == life.Birthplace?.Scene)
                .Where(l => l.Bearer == null)
                .FirstOrDefault();
            
            if (target == null)
                return false;
            
            Logic.Move.Walk.FollowShortest(life, target.Map);
            return true;
        }
    }
}