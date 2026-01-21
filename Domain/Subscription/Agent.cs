using Logic;

namespace Domain.Subscription
{
    public enum CardType
    {
        None = 0,
        Basic = 1,
        Premium = 2,
    }

    public static class Agent
    {
        // Track Mall panel refresh tasks for each player
        private static readonly Dictionary<string, long> _mallRefreshTasks = new();
        private const int MALL_REFRESH_INTERVAL_MS = 1000;

        public static void Init()
        {
            // Register experience bonus application
            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Player), OnAddPlayer);
            Logic.Agent.Instance.Content.Remove.Register(typeof(Logic.Player), OnRemovePlayer);
        }

        private static void OnAddPlayer(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[1];
            player.data.after.Register(Logic.Life.Data.Exp, OnPlayerExpChanged);
            
            // Register Option events for Mall panel refresh (Option is added to player.Content)
            player.Content.Add.Register(typeof(Logic.Option), OnAddOption);
            player.Content.Remove.Register(typeof(Logic.Option), OnRemoveOption);
            
            // Calculate accumulated monthly exp since last calculation
            AccumulateMonthlyExp(player);
        }

        private static void OnRemovePlayer(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[1];
            player.data.after.Unregister(Logic.Life.Data.Exp, OnPlayerExpChanged);
            
            // Unregister Option events
            player.Content.Add.Unregister(typeof(Logic.Option), OnAddOption);
            player.Content.Remove.Unregister(typeof(Logic.Option), OnRemoveOption);
            
            // Stop Mall refresh task if player logs out
            StopMallRefresh(player);
        }

        private static void OnAddOption(params object[] args)
        {
            if (args[0] is not Player player) return;
            if (args[1] is not Option option) return;
            
            // Start refresh when Mall panel is opened and player has monthly card
            if (option.Type == Option.Types.Mall && GetCardType(player) != CardType.None)
            {
                StartMallRefresh(player);
            }
        }

        private static void OnRemoveOption(params object[] args)
        {
            if (args[0] is not Player player) return;
            if (args[1] is not Option option) return;
            
            // Stop refresh when Mall panel is closed
            if (option.Type == Option.Types.Mall)
            {
                StopMallRefresh(player);
            }
        }

        private static void StartMallRefresh(Player player)
        {
            // Cancel existing task if any
            StopMallRefresh(player);
            
            // Start periodic refresh
            long taskId = Time.Agent.Instance.Scheduler.Repeat(MALL_REFRESH_INTERVAL_MS, (_) =>
            {
                // Check if player still has Mall panel open
                if (player.Option?.Type != Option.Types.Mall)
                {
                    StopMallRefresh(player);
                    return;
                }
                
                // Accumulate exp and refresh display
                AccumulateMonthlyExp(player);
                Display.Agent.Instance.Refresh(player);
            });
            
            _mallRefreshTasks[player.Id] = taskId;
        }

        private static void StopMallRefresh(Player player)
        {
            if (_mallRefreshTasks.TryGetValue(player.Id, out long taskId))
            {
                Time.Agent.Instance.Scheduler.CancelTask(taskId);
                _mallRefreshTasks.Remove(player.Id);
            }
        }

        private static void OnPlayerExpChanged(params object[] args)
        {
            // Experience bonus is applied at the source (Experience.cs)
            // This handler can be used for additional processing if needed
        }

        /// <summary>
        /// Check if player has active basic monthly card
        /// </summary>
        public static bool HasBasicCard(Player player)
        {
            if (player == null) return false;
            return player.TokenMonthBasicLastTime > DateTime.Now;
        }

        /// <summary>
        /// Check if player has active premium monthly card
        /// </summary>
        public static bool HasPremiumCard(Player player)
        {
            if (player == null) return false;
            return player.TokenMonthPremiumLastTime > DateTime.Now;
        }

        /// <summary>
        /// Check if player has any monthly card (basic or premium)
        /// </summary>
        public static bool HasAnyCard(Player player)
        {
            return HasBasicCard(player) || HasPremiumCard(player);
        }

        /// <summary>
        /// Get player's highest active monthly card type (for display purposes)
        /// </summary>
        public static CardType GetCardType(Player player)
        {
            if (player == null) return CardType.None;
            
            // For display, show the highest tier card
            if (HasPremiumCard(player)) return CardType.Premium;
            if (HasBasicCard(player)) return CardType.Basic;
            return CardType.None;
        }

        /// <summary>
        /// Get experience bonus multiplier (stackable: 1.0 + Basic 15% + Premium 35% = 1.5)
        /// </summary>
        public static double GetExpBonus(Player player)
        {
            double bonus = 1.0;
            if (HasBasicCard(player)) bonus += Logic.Constant.BasicMonthlyCardExpBonus;
            if (HasPremiumCard(player)) bonus += Logic.Constant.PremiumMonthlyCardExpBonus;
            return bonus;
        }

        /// <summary>
        /// Get monthly experience rate (stackable: Basic 1.0 + Premium 1.0 = 2.0)
        /// </summary>
        public static double GetMonthlyExpRate(Player player)
        {
            double rate = 0.0;
            if (HasBasicCard(player)) rate += Logic.Constant.BasicMonthlyCardMonthlyExp;
            if (HasPremiumCard(player)) rate += Logic.Constant.PremiumMonthlyCardMonthlyExp;
            return rate;
        }

        /// <summary>
        /// Get market tax rate (stackable reduction: 8% - Basic 2% - Premium 4% = 2%)
        /// </summary>
        public static double GetTaxRate(Player player)
        {
            double rate = Logic.Constant.FreePlayerTaxRate;
            if (HasBasicCard(player)) rate -= Logic.Constant.BasicMonthlyCardTaxReduction;
            if (HasPremiumCard(player)) rate -= Logic.Constant.PremiumMonthlyCardTaxReduction;
            return Math.Max(0, rate);
        }

        /// <summary>
        /// Get consignment slot limit (stackable: 5 + Basic 5 + Premium 10, capped at 15)
        /// </summary>
        public static int GetConsignmentSlots(Player player)
        {
            int slots = Logic.Constant.FreePlayerConsignmentSlots;
            if (HasBasicCard(player)) slots += Logic.Constant.BasicMonthlyCardConsignmentSlotsBonus;
            if (HasPremiumCard(player)) slots += Logic.Constant.PremiumMonthlyCardConsignmentSlotsBonus;
            return Math.Min(slots, Logic.Constant.MaxConsignmentSlots);
        }

        /// <summary>
        /// Check if player has behavior tree access (Premium only)
        /// </summary>
        public static bool HasBehaviorTreeAccess(Player player)
        {
            return HasPremiumCard(player);
        }

        /// <summary>
        /// Get daily credit reward (stackable: Basic 2 + Premium 4 = 6)
        /// </summary>
        public static int GetDailyCredit(Player player)
        {
            int credit = 0;
            if (HasBasicCard(player)) credit += Logic.Constant.BasicMonthlyCardDailyCredit;
            if (HasPremiumCard(player)) credit += Logic.Constant.PremiumMonthlyCardDailyCredit;
            return credit;
        }

        /// <summary>
        /// Get daily exp multiplier count (stackable: Basic 1 + Premium 1 = 2 per type)
        /// </summary>
        public static int GetDailyExpMultiplierCount(Player player)
        {
            int count = 0;
            if (HasBasicCard(player)) count += Logic.Constant.BasicMonthlyCardDailyExpMultiplier;
            if (HasPremiumCard(player)) count += Logic.Constant.PremiumMonthlyCardDailyExpMultiplier;
            return count;
        }

        /// <summary>
        /// Calculate and accumulate monthly exp based on time elapsed since last calculation.
        /// Monthly exp accumulates continuously regardless of online status.
        /// Basic and Premium cards accumulate separately with their own rates.
        /// Formula: exp = MonthlyRate(level) × CardRate × ExpBonus × elapsedSeconds
        /// </summary>
        public static void AccumulateMonthlyExp(Player player)
        {
            if (player == null) return;

            DateTime now = DateTime.Now;
            DateTime lastCalc = player.MonthlyExpLastCalcTime;

            // No monthly card or expired
            if (!HasAnyCard(player)) 
            {
                player.MonthlyExpLastCalcTime = now;
                return;
            }

            // First time calculation (lastCalc defaults to DateTime.Now from GetTime)
            // If lastCalc is very close to now (within 1 second), skip accumulation
            double elapsedSeconds = (now - lastCalc).TotalSeconds;
            if (elapsedSeconds < 1)
            {
                return;
            }

            // MonthlyRate(level) = level - 1 (exp per second at base rate)
            // For level 1 players, use minimum rate of 1 to ensure some exp gain
            double monthlyRate = Math.Max(1, player.Level - 1);
            double expBonus = GetExpBonus(player);

            // Accumulate Basic card exp separately
            if (HasBasicCard(player))
            {
                DateTime basicExpire = player.TokenMonthBasicLastTime;
                DateTime effectiveEnd = now > basicExpire ? basicExpire : now;
                double validSeconds = (effectiveEnd - lastCalc).TotalSeconds;
                if (validSeconds > 0)
                {
                    double basicExpGain = monthlyRate * Logic.Constant.BasicMonthlyCardMonthlyExp * expBonus * validSeconds;
                    player.MonthlyExpAccumulatorBasic += basicExpGain;
                    player.MonthlyExpAccumulator += basicExpGain;
                }
            }

            // Accumulate Premium card exp separately
            if (HasPremiumCard(player))
            {
                DateTime premiumExpire = player.TokenMonthPremiumLastTime;
                DateTime effectiveEnd = now > premiumExpire ? premiumExpire : now;
                double validSeconds = (effectiveEnd - lastCalc).TotalSeconds;
                if (validSeconds > 0)
                {
                    double premiumExpGain = monthlyRate * Logic.Constant.PremiumMonthlyCardMonthlyExp * expBonus * validSeconds;
                    player.MonthlyExpAccumulatorPremium += premiumExpGain;
                    player.MonthlyExpAccumulator += premiumExpGain;
                }
            }

            // Update last calculation time
            player.MonthlyExpLastCalcTime = now;
        }

        /// <summary>
        /// Get monthly exp rate per second for display.
        /// Formula: MonthlyRate(level) × MonthlyExpRate × ExpBonus = (level - 1) × cardMultiplier × expBonus
        /// </summary>
        public static double GetMonthlyExpPerSecond(Player player)
        {
            if (player == null) return 0;
            
            double monthlyExpRate = GetMonthlyExpRate(player);
            if (monthlyExpRate <= 0) return 0;
            
            double monthlyRate = Math.Max(1, player.Level - 1);
            double expBonus = GetExpBonus(player);
            return monthlyRate * monthlyExpRate * expBonus;
        }

        /// <summary>
        /// Claim all accumulated monthly exp. Called when player clicks the claim button.
        /// First recalculates accumulated exp to include time since last calculation.
        /// Sends claim message BEFORE adding exp to ensure correct message order (claim -> level up).
        /// </summary>
        public static int ClaimMonthlyExp(Player player)
        {
            if (player == null) return 0;

            // First accumulate any pending exp
            AccumulateMonthlyExp(player);

            int wholeExp = (int)player.MonthlyExpAccumulator;
            if (wholeExp > 0)
            {
                // Send claim message BEFORE adding exp (adding exp may trigger level up which sends its own message)
                string message = Domain.Text.Agent.Instance.GetDynamic(
                    Logic.Text.Labels.MonthlyExpClaimed, 
                    player.Language, 
                    ("exp", wholeExp.ToString()));
                Domain.Broadcast.Instance.System(player, [message]);
                Net.Tcp.Instance.Send(player, new Net.Protocol.FlyTip(message));
                
                player.Exp += wholeExp;
                player.MonthlyExpAccumulator -= wholeExp;
                
                // Clear the separate accumulators proportionally
                double totalAccumulated = player.MonthlyExpAccumulatorBasic + player.MonthlyExpAccumulatorPremium;
                if (totalAccumulated > 0)
                {
                    double ratio = wholeExp / totalAccumulated;
                    player.MonthlyExpAccumulatorBasic -= player.MonthlyExpAccumulatorBasic * ratio;
                    player.MonthlyExpAccumulatorPremium -= player.MonthlyExpAccumulatorPremium * ratio;
                }
            }
            return wholeExp;
        }
    }
}
