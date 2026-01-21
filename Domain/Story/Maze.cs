using Logic;

namespace Domain.Story
{
    public static class Maze
    {
        public static bool Can(Plot plot, Player player)
        {
            bool hasMazeConfig = plot.Config.maze > 0;
            bool hasScene = player.Map?.Scene != null;
            return hasMazeConfig && hasScene;
        }
        public static Logic.Maze Get(Player player)
        {
            return (Logic.Maze)player?.Map.Parent;

        }
        public static void Do(Plot plot, Player player)
        {
            var mazeConfig = Logic.Config.Agent.Instance.Content.Get<Logic.Config.Maze>(m => m.Id == plot.Config.maze);
            if (mazeConfig != null)
            {
                var maze = Logic.Agent.Instance.Create<Logic.Maze>(mazeConfig, player.Map.Database.pos);
                maze.InitializeCharacters();
                if (maze?.Last != null)
                {
                    maze.Last.AddAsParent(player);
                }
            }
        }
        public static List<Logic.Player> GetPlayers(Logic.Maze maze)
        {
            List<Logic.Player> players = new List<Logic.Player>();
            foreach (Logic.Map map in maze.Content.Gets<Logic.Map>())
            {
                players.AddRange(map.Content.Gets<Logic.Player>());
            }
            return players;
        }
        private static bool Empty(Logic.Maze maze)
        {
            return GetPlayers(maze).Count == 0;
        }
        public static void CheckAndDestroy(Logic.Maze maze)
        {
            if (Empty(maze))
            {
                maze.Destroy();
            }
        }
        public static bool IsIn(Logic.Player player)
        {
            return player.Map?.Parent is Logic.Maze;
        }
        public static bool WillExit(Logic.Player player, Logic.Map destination, out Logic.Maze maze)
        {
            if (IsIn(player))
            {
                maze = (Logic.Maze)player.Map.Parent;
                return destination == maze.Last;
            }
            else
            {
                maze = null;
                return false;
            }
        }
        public static void Exit(Logic.Player player, Logic.Maze maze, Logic.Map destination)
        {
            if (IsIn(player))
            {
                Move.Walk.Do(player, destination);
                CheckAndDestroy(maze);
            }
        }
    }
}
