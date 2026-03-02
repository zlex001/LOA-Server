using Data;
using System.Collections.Generic;

namespace Logic.Mall
{
    public class Agent
    {
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }

        private Dictionary<string, int> purchaseRecords = new Dictionary<string, int>();

        private string GetRecordKey(Player player, int mallId)
        {
            return $"{player.Id}_{mallId}";
        }

        public int GetPurchasedCount(Player player, int mallId)
        {
            var key = GetRecordKey(player, mallId);
            return purchaseRecords.TryGetValue(key, out int count) ? count : 0;
        }

        public int GetMaxBuyable(Player player, global::Data.Config.Mall mallConfig)
        {
            if (player.Gem <= 0 || mallConfig.Price <= 0) return 0;

            int byGem = player.Gem / mallConfig.Price;
            
            // For Subscription type, allow purchasing multiple months (extend subscription)
            // The Limit field for subscriptions represents the card type, not purchase limit
            // Max buyable is determined by current gem balance only
            if (mallConfig.Type == global::Data.Config.Mall.Types.Subscription)
            {
                return byGem;
            }

            if (mallConfig.Limit > 0)
            {
                int purchased = GetPurchasedCount(player, mallConfig.Id);
                int remaining = mallConfig.Limit - purchased;
                if (remaining <= 0) return 0;
                return System.Math.Min(byGem, remaining);
            }

            return byGem;
        }

        public void RecordPurchase(Player player, int mallId, int count)
        {
            if (count <= 0) return;

            var mallConfig = global::Data.Config.Agent.Instance.Content.Get<global::Data.Config.Mall>(m => m.Id == mallId);
            if (mallConfig == null || mallConfig.Limit <= 0) return;

            var key = GetRecordKey(player, mallId);
            if (!purchaseRecords.ContainsKey(key))
            {
                purchaseRecords[key] = 0;
            }
            purchaseRecords[key] += count;
        }
    }
}
