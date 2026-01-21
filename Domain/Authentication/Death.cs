using System;
using System.Linq;

namespace Domain.Authentication
{
    public static class Death
    {
        public static void Do(Logic.Player player)
        {
            if (player == null) return;

            var client = Net.Tcp.Instance.Content.Get<Net.Client>(c => c.Player == player);
            
            if (client != null)
            {
                client.Player = null;
            }

            try
            {
                Logic.Database.Agent.Instance.Delete(player.Database);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("AUTH", $"Failed to delete database record: {ex.Message}");
            }

            Logic.Database.Agent.Instance.Remove(player.Database);
            
            if (player.Map != null)
            {
                Logic.Agent.Instance.Remove(player);
            }

            if (player.Leader != null)
            {
                Move.Follow.DoUnFollow(player);
            }
            
            player.Destroy();
            
            if (client != null)
            {
                Net.Tcp.Instance.Remove(client);
            }
        }
    }
}

