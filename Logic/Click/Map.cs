
using Data;
using System.Linq;

namespace Logic.Click
{
    public class Map
    {

        public static void On(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[0];
            int[] pos = (int[])args[1];
            
            // If player is in a Copy instance, always handle as map click (not scene click)
            // Copy instances use relative coordinates that may conflict with world scene coordinates
            if (player.Map?.Copy != null)
            {
                HandleMapClick(player, pos);
                return;
            }
            
            // If target is in the same scene as player, handle as map click
            // This prevents coordinate conflicts between different scenes
            if (IsInPlayerScene(player, pos))
            {
                HandleMapClick(player, pos);
                return;
            }
            
            if (IsSceneCoordinate(pos))
            {
                HandleSceneClick(player, pos);
            }
            else
            {
                HandleMapClick(player, pos);
            }
        }

        private static bool IsInPlayerScene(Player player, int[] pos)
        {
            if (player?.Map?.Scene == null || pos == null || pos.Length < 3)
            {
                return false;
            }
            
            // Check if target position exists as a map in player's current scene
            return player.Map.Scene.Content.Has<global::Data.Map>(m => 
                m.Database.pos != null 
                && m.Database.pos.Length >= 3
                && m.Database.pos[0] == pos[0] 
                && m.Database.pos[1] == pos[1] 
                && m.Database.pos[2] == pos[2]);
        }

        private static bool IsSceneCoordinate(int[] pos)
        {
            if (pos == null || pos.Length < 3) return false;
            
            return global::Data.Design.World.SceneCoordinates.Any(coord => 
                coord.pos != null 
                && coord.pos.Length >= 3 
                && coord.pos[0] == pos[0] 
                && coord.pos[1] == pos[1] 
                && coord.pos[2] == pos[2]);
        }

        private static void HandleSceneClick(Player player, int[] scenePos)
        {
            var sceneInfo = global::Data.Design.World.SceneCoordinates
                .FirstOrDefault(coord => coord.pos != null 
                    && coord.pos.Length >= 3 
                    && coord.pos[0] == scenePos[0] 
                    && coord.pos[1] == scenePos[1] 
                    && coord.pos[2] == scenePos[2]);
            
            if (sceneInfo.sceneCid == null)
            {
                Utils.Debug.Log.Warning("CLICK", $"Scene not found at position [{scenePos[0]}, {scenePos[1]}, {scenePos[2]}]");
                return;
            }
            
            var sceneDesign = global::Data.Design.Agent.Instance.Content.Get<global::Data.Design.Scene>(s => s.cid == sceneInfo.sceneCid);
            if (sceneDesign == null)
            {
                Utils.Debug.Log.Warning("CLICK", $"Scene design not found for cid: {sceneInfo.sceneCid}");
                return;
            }
            
            var scene = global::Data.Agent.Instance.Content.Get<global::Data.Scene>(s => s.Config.Id == sceneDesign.id);
            if (scene == null)
            {
                Utils.Debug.Log.Warning("CLICK", $"Scene instance not found for id: {sceneDesign.id}");
                return;
            }
            
            var entryPoint = CalculateSceneEntryPoint(scene);
            if (entryPoint == null)
            {
                Utils.Debug.Log.Warning("CLICK", $"Cannot calculate entry point for scene: {sceneInfo.sceneCid}");
                return;
            }
            
            Do(player, entryPoint);
        }

        private static void HandleMapClick(Player player, int[] pos)
        {
            global::Data.Map destination = Move.Agent.Teleportation(player, pos);
            if (destination == null)
            {
                Utils.Debug.Log.Warning("CLICK", $"Map not found at pos=[{pos[0]},{pos[1]},{pos[2]}]");
                return;
            }
            
            if (Quest.Copy.WillExit(player, destination))
            {
                Quest.Copy.Exit(player, player.Map.Copy, destination);
            }
            else if (Quest.Maze.WillExit(player, destination, out global::Data.Maze maze))
            {
                Quest.Maze.Exit(player, maze, destination);
            }
            else
            {
               Do(player, destination);
            }
        }

        private static global::Data.Map CalculateSceneEntryPoint(global::Data.Scene scene)
        {
            var maps = scene.Content.Gets<global::Data.Map>(m => m.Copy == null).ToList();
            if (maps.Count == 0) return null;
            
            int minX = maps.Min(m => m.Database.pos[0]);
            int maxX = maps.Max(m => m.Database.pos[0]);
            int minY = maps.Min(m => m.Database.pos[1]);
            int maxY = maps.Max(m => m.Database.pos[1]);
            int z = maps[0].Database.pos[2];
            
            int centerX = (minX + maxX) / 2;
            int centerY = (minY + maxY) / 2;
            
            var centerMap = maps
                .OrderBy(m => System.Math.Abs(m.Database.pos[0] - centerX) + System.Math.Abs(m.Database.pos[1] - centerY))
                .FirstOrDefault();
            
            return centerMap;
        }

        private static void Do(Player player, global::Data.Map destination) 
        {
            if (player.State.Is(global::Data.Life.States.Unconscious))
            {
                string tip = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.CannotWalkWhileUnconscious, player.Language);
                Net.Tcp.Instance.Send(player, new Net.Protocol.FlyTip(tip));
                return;
            }

            player.ClickTarget = destination;

            int distance = Move.Distance.Get(player.Map, destination);
            if (distance > player.WalkScale)
            {
                BehaviorTree.Agent.SetBehaviorTree(player, global::Data.Constant.OneTimePathfinding);
            }
            else
            {
                Move.Walk.Do(player, destination);
            }
        }
    }
}
