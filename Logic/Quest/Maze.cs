using Data;

namespace Logic.Quest
{
    public static class Maze
    {
        public static bool Can(global::Data.Quest quest, Player player)
        {
            bool hasMazeConfig = quest.Config.maze > 0;
            bool hasScene = player.Map?.Scene != null;
            return hasMazeConfig && hasScene;
        }
        public static global::Data.Maze Get(Player player)
        {
            return (global::Data.Maze)player?.Map.Parent;

        }
        public static void Do(global::Data.Quest quest, Player player)
        {
            var mazeConfig = global::Data.Config.Agent.Instance.Content.Get<global::Data.Config.Maze>(m => m.Id == quest.Config.maze);
            if (mazeConfig != null)
            {
                var maze = global::Data.Agent.Instance.Create<global::Data.Maze>(mazeConfig, player.Map.Database.pos);
                maze.InitializeCharacters();
                if (maze?.Last != null)
                {
                    maze.Last.AddAsParent(player);
                }
            }
        }
        public static List<global::Data.Player> GetPlayers(global::Data.Maze maze)
        {
            List<global::Data.Player> players = new List<global::Data.Player>();
            foreach (global::Data.Map map in maze.Content.Gets<global::Data.Map>())
            {
                players.AddRange(map.Content.Gets<global::Data.Player>());
            }
            return players;
        }
        private static bool Empty(global::Data.Maze maze)
        {
            return GetPlayers(maze).Count == 0;
        }
        public static void CheckAndDestroy(global::Data.Maze maze)
        {
            if (Empty(maze))
            {
                maze.Destroy();
            }
        }
        public static bool IsIn(global::Data.Player player)
        {
            return player.Map?.Parent is global::Data.Maze;
        }
        public static bool WillExit(global::Data.Player player, global::Data.Map destination, out global::Data.Maze maze)
        {
            if (IsIn(player))
            {
                maze = (global::Data.Maze)player.Map.Parent;
                return destination == maze.Last;
            }
            else
            {
                maze = null;
                return false;
            }
        }
        public static void Exit(global::Data.Player player, global::Data.Maze maze, global::Data.Map destination)
        {
            if (IsIn(player))
            {
                Move.Walk.Do(player, destination);
                CheckAndDestroy(maze);
            }
        }
    }
}
