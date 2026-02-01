
using Logic;
using System.Linq;

namespace Domain.Click
{
    public class Map
    {

        public static void On(params object[] args)
        {
            Utils.Debug.Log.Info("CLICK", $"[Map.On] args.Length={args?.Length}, player={args?[0]}, pos={args?[1]}");
            Logic.Player player = (Logic.Player)args[0];
            int[] pos = (int[])args[1];
            
            Utils.Debug.Log.Info("CLICK", $"[Map.On] player.Map={player.Map?.GetType().Name}, player.Map.Copy={player.Map?.Copy}, player.Map.Scene={player.Map?.Scene?.GetType().Name}");
            
            // If player is in a Copy instance, always handle as map click (not scene click)
            // Copy instances use relative coordinates that may conflict with world scene coordinates
            if (player.Map?.Copy != null)
            {
                Utils.Debug.Log.Info("CLICK", $"[Map.On] Branch: player in Copy");
                HandleMapClick(player, pos);
                return;
            }
            
            // If target is in the same scene as player, handle as map click
            // This prevents coordinate conflicts between different scenes
            bool inPlayerScene = IsInPlayerScene(player, pos);
            Utils.Debug.Log.Info("CLICK", $"[Map.On] IsInPlayerScene={inPlayerScene}");
            if (inPlayerScene)
            {
                Utils.Debug.Log.Info("CLICK", $"[Map.On] Branch: target in player's scene");
                HandleMapClick(player, pos);
                return;
            }
            
            bool isSceneCoord = IsSceneCoordinate(pos);
            Utils.Debug.Log.Info("CLICK", $"[Map.On] IsSceneCoordinate={isSceneCoord}");
            if (isSceneCoord)
            {
                Utils.Debug.Log.Info("CLICK", $"[Map.On] Branch: HandleSceneClick");
                HandleSceneClick(player, pos);
            }
            else
            {
                Utils.Debug.Log.Info("CLICK", $"[Map.On] Branch: HandleMapClick (default)");
                HandleMapClick(player, pos);
            }
        }

        private static bool IsInPlayerScene(Player player, int[] pos)
        {
            Utils.Debug.Log.Info("CLICK", $"[IsInPlayerScene] player.Map.Scene={player?.Map?.Scene?.GetType().Name}, sceneNull={player?.Map?.Scene == null}");
            if (player?.Map?.Scene == null || pos == null || pos.Length < 3)
            {
                Utils.Debug.Log.Info("CLICK", $"[IsInPlayerScene] Early return false (null check)");
                return false;
            }
            
            // Check if target position exists as a map in player's current scene
            var mapsInScene = player.Map.Scene.Content.Gets<Logic.Map>().ToList();
            Utils.Debug.Log.Info("CLICK", $"[IsInPlayerScene] Maps in scene count={mapsInScene.Count}");
            
            bool result = player.Map.Scene.Content.Has<Logic.Map>(m => 
                m.Database.pos != null 
                && m.Database.pos.Length >= 3
                && m.Database.pos[0] == pos[0] 
                && m.Database.pos[1] == pos[1] 
                && m.Database.pos[2] == pos[2]);
            Utils.Debug.Log.Info("CLICK", $"[IsInPlayerScene] result={result}");
            return result;
        }

        private static bool IsSceneCoordinate(int[] pos)
        {
            if (pos == null || pos.Length < 3) return false;
            
            return Logic.Design.World.SceneCoordinates.Any(coord => 
                coord.pos != null 
                && coord.pos.Length >= 3 
                && coord.pos[0] == pos[0] 
                && coord.pos[1] == pos[1] 
                && coord.pos[2] == pos[2]);
        }

        private static void HandleSceneClick(Player player, int[] scenePos)
        {
            Utils.Debug.Log.Info("CLICK", $"[HandleSceneClick] Start - scenePos=[{string.Join(",", scenePos)}]");
            var sceneInfo = Logic.Design.World.SceneCoordinates
                .FirstOrDefault(coord => coord.pos != null 
                    && coord.pos.Length >= 3 
                    && coord.pos[0] == scenePos[0] 
                    && coord.pos[1] == scenePos[1] 
                    && coord.pos[2] == scenePos[2]);
            
            Utils.Debug.Log.Info("CLICK", $"[HandleSceneClick] sceneInfo.sceneCid={sceneInfo.sceneCid}");
            if (sceneInfo.sceneCid == null)
            {
                Utils.Debug.Log.Warning("CLICK", $"Scene not found at position [{scenePos[0]}, {scenePos[1]}, {scenePos[2]}]");
                return;
            }
            
            var sceneDesign = Logic.Design.Agent.Instance.Content.Get<Logic.Design.Scene>(s => s.cid == sceneInfo.sceneCid);
            Utils.Debug.Log.Info("CLICK", $"[HandleSceneClick] sceneDesign={sceneDesign?.cid}, id={sceneDesign?.id}");
            if (sceneDesign == null)
            {
                Utils.Debug.Log.Warning("CLICK", $"Scene design not found for cid: {sceneInfo.sceneCid}");
                return;
            }
            
            var scene = Logic.Agent.Instance.Content.Get<Logic.Scene>(s => s.Config.Id == sceneDesign.id);
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
            Utils.Debug.Log.Info("CLICK", $"[HandleMapClick] player.Map={player.Map?.GetType().Name}, playerPos=[{string.Join(",", player.Map?.Database.pos ?? new int[0])}], targetPos=[{string.Join(",", pos)}]");
            Logic.Map destination = Move.Agent.Teleportation(player, pos);
            Utils.Debug.Log.Info("CLICK", $"[HandleMapClick] Teleportation result: destination={destination?.GetType().Name}, destPos=[{string.Join(",", destination?.Database.pos ?? new int[0])}]");
            if (destination == null)
            {
                Utils.Debug.Log.Warning("CLICK", $"Map not found at pos=[{pos[0]},{pos[1]},{pos[2]}]");
                return;
            }
            
            if (Story.Copy.WillExit(player, destination))
            {
                Utils.Debug.Log.Info("CLICK", $"[HandleMapClick] WillExit Copy");
                Story.Copy.Exit(player, player.Map.Copy, destination);
            }
            else if (Story.Maze.WillExit(player, destination, out Logic.Maze maze))
            {
                Utils.Debug.Log.Info("CLICK", $"[HandleMapClick] WillExit Maze");
                Story.Maze.Exit(player, maze, destination);
            }
            else
            {
                Utils.Debug.Log.Info("CLICK", $"[HandleMapClick] Calling Do()");
                Do(player, destination);
            }
        }

        private static Logic.Map CalculateSceneEntryPoint(Logic.Scene scene)
        {
            var maps = scene.Content.Gets<Logic.Map>(m => m.Copy == null).ToList();
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

        private static void Do(Player player, Logic.Map destination) 
        {
            Utils.Debug.Log.Info("CLICK", $"[Do] Start - playerMap=[{string.Join(",", player.Map?.Database.pos ?? new int[0])}], dest=[{string.Join(",", destination?.Database.pos ?? new int[0])}]");
            if (player.State.Is(Logic.Life.States.Unconscious))
            {
                Utils.Debug.Log.Info("CLICK", $"[Do] Player is unconscious, cannot walk");
                string tip = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.CannotWalkWhileUnconscious, player.Language);
                Net.Tcp.Instance.Send(player, new Net.Protocol.FlyTip(tip));
                return;
            }

            player.ClickTarget = destination;

            int distance = Move.Distance.Get(player.Map, destination);
            Utils.Debug.Log.Info("CLICK", $"[Do] distance={distance}, walkScale={player.WalkScale}");
            if (distance > player.WalkScale)
            {
                Utils.Debug.Log.Info("CLICK", $"[Do] Using pathfinding");
                BehaviorTree.Agent.SetBehaviorTree(player, Logic.Constant.OneTimePathfinding);
            }
            else
            {
                Utils.Debug.Log.Info("CLICK", $"[Do] Direct walk");
                Move.Walk.Do(player, destination);
            }
        }
    }
}
