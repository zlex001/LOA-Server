using System;
using System.Collections.Generic;

namespace Data.ServerMerge
{
    public class MergeConfig
    {
        public string MergeId { get; set; }
        public DateTime MergeDate { get; set; }
        public string TargetServerId { get; set; }
        public List<string> SourceServerIds { get; set; }
        public MergeStatus Status { get; set; }
        public Dictionary<string, object> Options { get; set; }
        
        public MergeConfig()
        {
            SourceServerIds = new List<string>();
            Options = new Dictionary<string, object>();
            Status = MergeStatus.Pending;
        }
    }

    public enum MergeStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Rollback
    }

    public class MergeCompensation
    {
        public string ServerId { get; set; }
        public int OriginalRank { get; set; }
        public Dictionary<string, int> Rewards { get; set; }

        public MergeCompensation()
        {
            Rewards = new Dictionary<string, int>();
        }
    }

    public class IdMapping
    {
        public string SourceServerId { get; set; }
        public long Offset { get; set; }
        public Dictionary<long, long> PlayerIdMap { get; set; }
        public Dictionary<long, long> ItemIdMap { get; set; }
        public Dictionary<string, string> NameMap { get; set; }

        public IdMapping()
        {
            PlayerIdMap = new Dictionary<long, long>();
            ItemIdMap = new Dictionary<long, long>();
            NameMap = new Dictionary<string, string>();
        }

        public long MapPlayerId(long originalId)
        {
            return originalId + Offset;
        }

        public long MapItemId(long originalId)
        {
            return originalId + Offset;
        }

        public string MapPlayerName(string originalName, bool hasDuplicate)
        {
            if (!hasDuplicate) return originalName;
            return $"{originalName}_{SourceServerId}";
        }
    }
}

