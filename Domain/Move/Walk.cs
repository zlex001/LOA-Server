using Logic;
using MathNet.Numerics;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Move
{
    public static class Walk
    {

        public static bool Can(Life life, Map destination)
        {
            if (life.State.Is(Logic.Life.States.Unconscious))
                return false;
            if (life.WalkScale < Distance.Get(life.Map, destination))
                return false;
            return true;
        }
        public static void Do(Life life, Map destination)
        {
            if (Can(life, destination))
            {
                var directionLabel = Agent.GetDirectionLabel(life.Map, destination);
                var directionText = Domain.Text.Agent.Instance.Id(directionLabel);
                //Broadcast.Instance.Local(life, [Logic.Text.Instance.FirstIdOf(Logic.Text.Labels.Leave)], ("sub", life), ("direction", directionText));
                Agent.Do(life, destination);
                if (life.Level < 10)
                {
                    life.Exp += 1;
                }
            }
        }


        public static void FollowShortest(Life life, Map destination)
        {
            if (life == null) return;
            if (life.Map == null) return;
            if (destination == null) return;
            if (destination.Scene == null) return;
            if (life.Map.Scene == null) return;
            
            if (life.Map.Scene == destination.Scene)
            {
                FollowShortestInScene(life, destination);
            }
            else
            {
                FollowShortestCrossScene(life, destination);
            }
        }

        private static void FollowShortestInScene(Life life, Map destination)
        {
            if (life.Map.Database.shortest == null)
            {
                return;
            }
            
            if (!life.Map.Database.shortest.TryGetValue(destination.Database.gid, out var paths))
            {
                return;
            }
            
            if (paths.Count == 0)
            {
                return;
            }
            
            if (life.Map.Scene.Content.Has<Map>(m => m.Database.gid == paths[0], out Map nextStep))
            {
                Do(life, nextStep);
            }
        }

        private static void FollowShortestCrossScene(Life life, Map destination)
        {
            if (life.Map.Scene.Database.shortest == null)
            {
                return;
            }
            
            if (destination.Scene.Database == null)
            {
                return;
            }
            
            if (!life.Map.Scene.Database.shortest.TryGetValue(destination.Scene.Database.id, out var paths))
            {
                return;
            }
            
            if (paths.Count == 0)
            {
                return;
            }
            
            if (!Logic.Agent.Instance.Content.Has<Scene>(s => s.Database.id == paths[0], out var nextScene))
            {
                return;
            }
            
            if (life.Map.Scene.Content.Has<Map>(m => m.Database.teleport != null && Agent.Teleportation(m.Database.teleport)?.Scene == nextScene, out var teleportPoint))
            {
                FollowShortestInScene(life, teleportPoint);
            }
        }
        public static List<int[]> Area(Logic.Player player)
        {
            var result = new List<int[]>();
            if (player.Map?.Scene != null)
            {
                foreach (var m in player.Map.Scene.Content.Gets<Logic.Map>())
                {
                    double distance = Distance.Get(player.Map, m);
                    if (distance <= player.ViewScale)
                    {
                        result.Add(m.Database.pos);
                    }
                }
            }
            return result;
        }

        public static void Nearest<T>(Life life, Func<T, bool> predicate) where T : Character
        {
            if (life == null) return;
            T target = Distance.Nearest<T>(life, predicate);
            if (target?.Map == null) return;
            FollowShortest(life, target.Map);
        }

        public static void Nearest(Character character, Func<Map, bool> predicate)
        {
            if (character == null) return;
            if (character is not Life life) return;
            if (life.Map == null) return;
            Map target = Distance.Nearest(life.Map, predicate);
            if (target == null) return;
            FollowShortest(life, target);
        }


    }
}