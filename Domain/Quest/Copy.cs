using Logic;
using System.Collections.Generic;

namespace Domain.Quest
{
    public static class Copy
    {
        public static bool Can(Logic.Quest quest, Player player)
        {
            return quest.Config.copy != null && quest.Config.copy.characters.Count > 0 && player.Map?.Scene != null;
        }
        public static bool IsIn(Logic.Player player)
        {
            return player.Map?.Copy != null;
        }
        public static Logic.Copy Get(Logic.Player player)
        {
            return player?.Map?.Copy;
        }
        public static void Do(Logic.Quest quest, Player player)
        {
            Logic.Copy copy = Logic.Agent.Instance.Create<Logic.Copy>(player.Map, quest.Config.copy);
            copy.Quest = quest;
            copy.Start.AddAsParent(player);
        }
        public static List<Logic.Player> GetPlayers(Logic.Copy copy)
        {
            List<Logic.Player> players = new List<Logic.Player>();
            foreach (Logic.Map map in copy.Content.Gets<Logic.Copy.Map>())
            {
                players.AddRange(map.Content.Gets<Logic.Player>());
            }
            return players;
        }
        public static bool WillExit(Logic.Player player, Logic.Map destination )
        {
            if (IsIn(player))
            {
                return player.Map.Copy != destination.Copy;
            }
            else
            {
                return false;
            }
        }
        public static void Exit(Logic.Player player,Logic.Copy copy, Logic.Map destination)
        {
            if (IsIn(player))
            {
                Move.Walk.Do(player, destination);
                CheckAndDestroy(copy);
            }
        }
        private static bool Empty(Logic.Copy copy)
        {
            return GetPlayers(copy).Count == 0;
        }
        public static void CheckAndDestroy ( Logic.Copy copy)
        {
            if (Empty(copy))
            {
                copy.Destroy();
            }
        }
    }
}
