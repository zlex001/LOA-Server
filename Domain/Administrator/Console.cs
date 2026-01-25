using System.Threading;

namespace Domain.Administrator
{
    /// <summary>
    /// Console keyboard input handler for GM operations.
    /// Runs in a background thread and delegates to GameMaster for business logic.
    /// </summary>
    public static class Console
    {
        private static Thread _listenerThread;
        private static bool _running;
        private static Action _shutdownCallback;

        /// <summary>
        /// Start the keyboard listener thread.
        /// </summary>
        /// <param name="shutdownCallback">Callback to invoke when ESC is pressed</param>
        public static void Start(Action shutdownCallback)
        {
            if (_listenerThread != null && _listenerThread.IsAlive)
            {
                return;
            }

            _shutdownCallback = shutdownCallback;
            _running = true;
            _listenerThread = new Thread(KeyboardListener)
            {
                IsBackground = true,
                Name = "ConsoleKeyboardListener"
            };
            _listenerThread.Start();
        }

        /// <summary>
        /// Stop the keyboard listener.
        /// </summary>
        public static void Stop()
        {
            _running = false;
        }

        private static void KeyboardListener()
        {
            // Skip keyboard listener if console input is not available
            if (System.Console.IsInputRedirected || !Environment.UserInteractive)
            {
                return;
            }

            while (_running)
            {
                try
                {
                    if (!System.Console.KeyAvailable)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Console not available, exit listener
                    return;
                }

                var key = System.Console.ReadKey(true);
                ProcessKey(key);
            }
        }

        private static void ProcessKey(ConsoleKeyInfo key)
        {
            // ESC - Shutdown (always available)
            if (key.Key == ConsoleKey.Escape)
            {
                _running = false;
                _shutdownCallback?.Invoke();
                return;
            }

            // Development-only commands
            if (!Utils.Debug.Log.IsDevelopment)
            {
                return;
            }

            switch (key.Key)
            {
                case ConsoleKey.Delete:
                    System.Console.Clear();
                    break;

                // F1-F4: Teleport to NPCs
                case ConsoleKey.F1:
                    GameMaster.TeleportAndFollow(2010000, "Farmer");
                    break;
                case ConsoleKey.F2:
                    GameMaster.TeleportAndFollow(2010001, "Miner");
                    break;
                case ConsoleKey.F3:
                    GameMaster.TeleportAndFollow(2010002, "Hunter");
                    break;
                case ConsoleKey.F4:
                    GameMaster.TeleportAndFollow(2010010, "Merchant");
                    break;

                // F5: Behavior tree debug
                case ConsoleKey.F5:
                    GameMaster.ExecuteFollowedNpcBehaviorTree();
                    break;

                // F6-F9: Plot tests
                case ConsoleKey.F6:
                    GameMaster.PlayPlot(30001, "Theft");
                    break;
                case ConsoleKey.F7:
                    GameMaster.PlayPlot(30002, "Blade");
                    break;
                case ConsoleKey.F8:
                    GameMaster.PlayPlot(30003, "Sword");
                    break;
                case ConsoleKey.F9:
                    GameMaster.PlayPlot(30004, "BeastTaming");
                    break;

                // F10: Export life attributes
                case ConsoleKey.F10:
                    GameMaster.ExportLifeAttributes();
                    break;

                // F11: Toggle TCP log
                case ConsoleKey.F11:
                    GameMaster.ToggleTcpLog();
                    break;

                // G: Add gems to current player
                case ConsoleKey.G:
                    var result = GameMaster.AddGem(null, 100);
                    if (!result.Success)
                    {
                        Utils.Debug.Log.Warning("GM", result.Message);
                    }
                    break;
            }
        }
    }
}
