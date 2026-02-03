namespace Domain.Administrator
{
    /// <summary>
    /// Core GM (Game Master) operations.
    /// Both HTTP API (Command.cs) and Console (Console.cs) call these methods.
    /// </summary>
    public static class GameMaster
    {
        public class Result
        {
            public bool Success { get; set; }
            public string Message { get; set; }

            public static Result Ok(string message) => new Result { Success = true, Message = message };
            public static Result Fail(string message) => new Result { Success = false, Message = message };
        }

        /// <summary>
        /// Add gems to a player's account.
        /// </summary>
        /// <param name="playerId">Player ID (null = first online player)</param>
        /// <param name="amount">Amount of gems to add</param>
        public static Result AddGem(string playerId, int amount)
        {
            if (amount <= 0)
            {
                return Result.Fail("Amount must be positive");
            }

            Logic.Player player;
            if (string.IsNullOrEmpty(playerId))
            {
                player = Logic.Agent.Instance.Content.Get<Logic.Player>();
                if (player == null)
                {
                    return Result.Fail("No online player found");
                }
            }
            else
            {
                player = Logic.Agent.Instance.Content.Get<Logic.Player>(p => p.Id == playerId);
                if (player == null)
                {
                    return Result.Fail($"Player not found: {playerId}");
                }
            }

            int oldGem = player.Gem;
            player.Gem += amount;
            int newGem = player.Gem;

            Utils.Debug.Log.Info("GM", $"[AddGem] Player={player.Id}, Amount={amount}, {oldGem} -> {newGem}");

            return Result.Ok($"Added {amount} gems to {player.Id} ({oldGem} -> {newGem})");
        }

        /// <summary>
        /// Teleport player to target NPC and follow.
        /// </summary>
        public static Result TeleportAndFollow(int targetConfigId, string targetName = null)
        {
            var player = Logic.Agent.Instance.Content.Get<Logic.Player>();
            if (player == null)
            {
                return Result.Fail("No online player found");
            }

            var targetLife = Move.Distance.Nearest<Logic.Life>(player, life =>
                life.Config?.Id == targetConfigId && !life.State.Is(Logic.Life.States.Unconscious));

            if (targetLife == null)
            {
                return Result.Fail($"Target NPC not found: {targetName ?? targetConfigId.ToString()}");
            }

            if (targetLife.Map == null)
            {
                return Result.Fail($"Target NPC {targetName ?? targetConfigId.ToString()} is not on any map");
            }

            if (player.Leader != null)
            {
                Move.Follow.DoUnFollow(player);
            }

            Move.Agent.Do(player, targetLife.Map);
            Move.Follow.Do(player, targetLife);

            string mapInfo = targetLife.Map.Config != null
                ? Text.Agent.Instance.Get(targetLife.Map.Config.Name, player)
                : $"Map#{targetLife.Map.Database.id}";

            var message = $"Teleported to {targetName ?? targetConfigId.ToString()} at {mapInfo}";
            Utils.Debug.Log.Info("GM", $"[TeleportAndFollow] {message}");

            return Result.Ok(message);
        }

        /// <summary>
        /// Execute behavior tree of the NPC that player is following.
        /// </summary>
        public static Result ExecuteFollowedNpcBehaviorTree()
        {
            var player = Logic.Agent.Instance.Content.Get<Logic.Player>();
            if (player == null)
            {
                return Result.Fail("No online player found");
            }

            if (player.Leader == null)
            {
                return Result.Fail("Player is not following any NPC");
            }

            var npc = player.Leader;
            string npcName = Text.Agent.Instance.Get(npc.Config?.Name ?? 0, player);

            if (npc.BtRoot == null)
            {
                return Result.Fail($"{npcName} has no behavior tree");
            }

            Utils.Debug.Log.Info("GM", $"[BehaviorTree] Executing {npcName} (Config ID: {npc.Config.Id})");
            npc.BtRoot.ExecuteWithDebug(npc);

            return Result.Ok($"Behavior tree executed for {npcName}");
        }

        /// <summary>
        /// Play a quest for the player.
        /// </summary>
        public static Result PlayQuest(int questId, string questName = null)
        {
            var player = Logic.Agent.Instance.Content.Get<Logic.Player>();
            if (player == null)
            {
                return Result.Fail("No online player found");
            }

            var questConfig = Logic.Config.Agent.Instance.Content.Get<Logic.Config.Quest>(p => p.Id == questId);
            if (questConfig == null)
            {
                return Result.Fail($"Quest not found: {questName ?? questId.ToString()}");
            }

            var quest = new Logic.Quest { Config = questConfig };
            Quest.DialogueSender.Do(player, quest);

            var message = $"Playing quest: {questName ?? questId.ToString()}";
            Utils.Debug.Log.Info("GM", $"[PlayQuest] {message}");

            return Result.Ok(message);
        }

        /// <summary>
        /// Toggle TCP protocol logging.
        /// </summary>
        public static Result ToggleTcpLog()
        {
            var currentState = Utils.Debug.Log.IsCategoryEnabled("TCP");
            Utils.Debug.Log.SetCategoryEnabled("TCP", !currentState);
            var newState = !currentState ? "ON" : "OFF";

            var message = $"TCP protocol log switched {newState}";
            Utils.Debug.Log.Info("GM", $"[ToggleTcpLog] {message}");

            return Result.Ok(message);
        }

        /// <summary>
        /// Export all life attributes to CSV file.
        /// </summary>
        public static Result ExportLifeAttributes()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"LifeAttributes_{timestamp}.csv";
            var filePath = System.IO.Path.Combine(Utils.Paths.Logs, fileName);

            var lives = Logic.Agent.Instance.Content.Gets<Logic.Life>().ToList();

            var rows = new List<List<object>>();
            rows.Add(new List<object> { "ConfigID", "Name", "Category", "Gender", "Level", "Age", "Hp", "Mp", "Lp", "Atk", "Def", "Agi", "Ine", "Con", "BTIntervalSec" });

            foreach (var life in lives)
            {
                var configId = life is Logic.Player ? 0 : (life.Config?.Id ?? 0);

                var name = life is Logic.Player p
                    ? (p.Name ?? p.Id)
                    : (life.Config?.Name != null
                        ? Text.Agent.Instance.Get(life.Config.Name, Logic.Text.Languages.ChineseSimplified)
                        : string.Empty);

                var parts = life.Content.Gets<Logic.Part>();
                var totalMaxHp = parts.Sum(p => p.MaxHp);
                var totalHp = parts.Sum(p => p.Hp);

                var realAge = life.Age / Time.Agent.Rate;

                double speed = Utils.Mathematics.Ratio(life.Agi, 100);
                double btIntervalMs = 1000 * (1 - speed);
                double btIntervalSec = btIntervalMs / 1000.0;

                rows.Add(new List<object>
                {
                    configId,
                    name,
                    life.Category.ToString(),
                    life.Gender.ToString(),
                    life.Level,
                    realAge.ToString("F1"),
                    $"{totalHp}/{totalMaxHp}",
                    $"{(int)life.Mp}/{(int)life.MaxMp}",
                    $"{(int)life.Lp}/{(int)life.MaxLp}",
                    ((int)life.Atk).ToString(),
                    ((int)life.Def).ToString(),
                    ((int)life.Agi).ToString(),
                    ((int)life.Ine).ToString(),
                    ((int)life.Con).ToString(),
                    btIntervalSec.ToString("F2")
                });
            }

            Utils.Csv.SaveByCells(rows, filePath);

            var message = $"Life attributes exported: {fileName} ({lives.Count} entities)";
            Utils.Debug.Log.Info("GM", $"[ExportLifeAttributes] {message}");

            return Result.Ok(message);
        }
    }
}
