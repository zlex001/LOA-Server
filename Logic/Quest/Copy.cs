using Data;
using System.Collections.Generic;

namespace Logic.Quest
{
    public static class Copy
    {
        public static bool Can(global::Data.Quest quest, Player player)
        {
            return quest.Config.copy != null && quest.Config.copy.characters.Count > 0 && player.Map?.Scene != null;
        }
        public static bool IsIn(global::Data.Player player)
        {
            return player.Map?.Copy != null;
        }
        public static global::Data.Copy Get(global::Data.Player player)
        {
            return player?.Map?.Copy;
        }
        public static void Do(global::Data.Quest quest, Player player)
        {
            global::Data.Copy copy = global::Data.Agent.Instance.Create<global::Data.Copy>(player.Map, quest.Config.copy);
            copy.Quest = quest;
            copy.Start.AddAsParent(player);
        }
        public static List<global::Data.Player> GetPlayers(global::Data.Copy copy)
        {
            List<global::Data.Player> players = new List<global::Data.Player>();
            foreach (global::Data.Map map in copy.Content.Gets<global::Data.Copy.Map>())
            {
                players.AddRange(map.Content.Gets<global::Data.Player>());
            }
            return players;
        }
        public static bool WillExit(global::Data.Player player, global::Data.Map destination )
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
        public static void Exit(global::Data.Player player,global::Data.Copy copy, global::Data.Map destination)
        {
            if (IsIn(player))
            {
                Move.Walk.Do(player, destination);
                CheckAndDestroy(copy);
            }
        }
        private static bool Empty(global::Data.Copy copy)
        {
            return GetPlayers(copy).Count == 0;
        }
        public static void CheckAndDestroy ( global::Data.Copy copy)
        {
            if (Empty(copy))
            {
                copy.Destroy();
            }
        }
    }
}
