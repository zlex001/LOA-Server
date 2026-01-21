using Logic;
using NPOI.SS.Formula.Functions;
using System.Linq;

namespace Domain.Move
{
    public class Follow : Domain.Agent<Follow>
    {

        public static void Init()
        {
            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Life), OnAddLife);
        }
        public static bool Can(Life follower, Logic.Ability target)
        {
            if (follower == null || target == null)
                return false;

            if (!(target is Life life))
                return false;

            if (follower == life)
                return false;

            if (life.State.Is(Logic.Life.States.Unconscious))
                return false;

            if (IsInAnyTeam(follower))
                return false;

            return true;
        }



        public static void Do(Life sub, Life obj)
        {
            sub.Leader = obj;
        }
        public static void DoUnFollow(Life follower)
        {
            follower.Leader = null;
        }


        private static void OnBeforeLifeLeaderChanged(params object[] args)
        {
            Life o = (Life)args[0];
            Life v = (Life)args[1];
            Life life = (Life)args[2];

            if (o != null)
            {
                Instance.Unregister(o.data.after, Basic.Element.Data.Parent);
                Instance.Unregister(life.data.after, Basic.Element.Data.Parent);
                Broadcast.Instance.Local(life, [Text.Agent.Instance.Id(Logic.Text.Labels.FollowStop)], ("sub", life), ("obj", o));
            }

            if (v != null)
            {
                Instance.Register(v.data.after, Basic.Element.Data.Parent, life, OnAfterLeaderParentChanged);
                Instance.Register(life.data.after, Basic.Element.Data.Parent, life, OnAfterFollowerParentChanged);
                Broadcast.Instance.Local(life, [Text.Agent.Instance.Id(Logic.Text.Labels.FollowStart)], ("sub", life), ("obj", v));
            }
        }



        private static void OnAfterLeaderParentChanged(Life follower, object[] args)
        {
            Basic.Manager parent = (Basic.Manager)args[0];

            if (parent == null)
            {
                follower.Leader = null;
            }
            else if (parent is Logic.Map map)
            {
                Walk.FollowShortest(follower, map);
            }
        }

        private static void OnAfterFollowerParentChanged(Life follower, object[] args)
        {
        }
        private static void OnAddLife(params object[] args)
        {
            Life life = (Life)args[1];
            life.data.before.Register(Life.Data.Leader, OnBeforeLifeLeaderChanged);
        }

        private static bool IsInAnyTeam(Life life)
        {
            return life.Leader != null;
        }





    }
}












