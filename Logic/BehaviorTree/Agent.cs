using Data;
using System.Collections.Generic;
using System.Linq;
using Utils;

namespace Logic.BehaviorTree
{
    public class Agent
    {

        public static void Init()
        {
            foreach (global::Data.Config.BehaviorTree behaviorTree in global::Data.Config.Agent.Instance.Content.Gets<global::Data.Config.BehaviorTree>())
            {
                global::Data.Agent.Instance.Load<global::Data.Config.BehaviorTree, global::Data.BehaviorTree.Node>(behaviorTree.Id);
            }
            foreach (var node in global::Data.Agent.Instance.Content.Gets<global::Data.BehaviorTree.Node>())
            {
                node.BuildChildRelations();
            }
            global::Data.Agent.Instance.Content.Add.Register(typeof(global::Data.Life), OnAddLife);

        }

        public static void SetBehaviorTree(Life life, int id)
        {
            if (life == null) return;
            var behaviorTree = global::Data.Agent.Instance.Content.Get<global::Data.BehaviorTree.Node>(n => n.Config.Id == id);
            if (behaviorTree != null)
            {
                Logic.State.Agent.StopBehaviorTree(life);
                life.BtRoot = behaviorTree;
                if (life.State != null && life.State.Is(Life.States.Normal))
                {
                    Logic.State.Agent.StartBehaviorTree(life);
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