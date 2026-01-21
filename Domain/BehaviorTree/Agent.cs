using Logic;
using System.Collections.Generic;
using System.Linq;
using Utils;

namespace Domain.BehaviorTree
{
    public class Agent
    {

        public static void Init()
        {
            foreach (Logic.Config.BehaviorTree behaviorTree in Logic.Config.Agent.Instance.Content.Gets<Logic.Config.BehaviorTree>())
            {
                Logic.Agent.Instance.Load<Logic.Config.BehaviorTree, Logic.BehaviorTree.Node>(behaviorTree.Id);
            }
            foreach (var node in Logic.Agent.Instance.Content.Gets<Logic.BehaviorTree.Node>())
            {
                node.BuildChildRelations();
            }
            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Life), OnAddLife);

        }

        public static void SetBehaviorTree(Life life, int id)
        {
            if (life == null) return;
            var behaviorTree = Logic.Agent.Instance.Content.Get<Logic.BehaviorTree.Node>(n => n.Config.Id == id);
            if (behaviorTree != null)
            {
                Domain.State.Agent.StopBehaviorTree(life);
                life.BtRoot = behaviorTree;
                if (life.State != null && life.State.Is(Life.States.Normal))
                {
                    Domain.State.Agent.StartBehaviorTree(life);
                }
            }
        }

        public static List<Scene> CalculateWorkingScenes(Life life)
        {
            var scenes = new List<Scene>();
            
            if (life?.Birthplace?.Scene == null) return scenes;
            
            var birthScene = life.Birthplace.Scene;
            scenes.Add(birthScene);
            
            var teleportMaps = birthScene.Content.Gets<Map>(m => m.Database.teleport != null);
            foreach (var teleportMap in teleportMaps)
            {
                var targetMap = Move.Agent.Teleportation(teleportMap.Database.teleport);
                if (targetMap?.Scene != null && targetMap.Scene != birthScene && !scenes.Contains(targetMap.Scene))
                {
                    scenes.Add(targetMap.Scene);
                }
            }
            
            return scenes;
        }

        private static void OnAddLife(params object[] args)
        {
            Life life = (Life)args[1];
            if (life == null) return;
            if (life.Config == null) return;
            
            if (life.Birthplace?.Scene != null)
            {
                life.WorkingScenes = CalculateWorkingScenes(life);
            }
            
            string behaviorTreeIdStr = life.Config.Tags.GetValue("BehaviorTree");
            if (string.IsNullOrEmpty(behaviorTreeIdStr)) return;
            if (int.TryParse(behaviorTreeIdStr, out int behaviorTreeId))
            {
                SetBehaviorTree(life, behaviorTreeId);
            }
        }


    }
}