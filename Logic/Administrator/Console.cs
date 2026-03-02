using System.Text;
using System.Threading;

namespace Logic.Administrator
{
    /// <summary>
    /// Console keyboard input handler for GM operations.
    /// Runs in a background thread and delegates to GameMaster for business logic.
    /// 
    /// Supports two modes:
    /// - Hotkey mode: F1-F11, Delete, ESC for quick actions
    /// - Command mode: Press '/' to enter, type command, Enter to execute, ESC to cancel
    /// </summary>
    public static class Console
    {
        private static Thread _listenerThread;
        private static bool _running;
        private static Action _shutdownCallback;
        
        // Command input mode
        private static bool _commandMode;
        private static StringBuilder _commandBuffer = new StringBuilder();

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
                
                if (_commandMode)
                {
                    ProcessCommandModeKey(key);
                }
                else
                {
                    ProcessHotkeyModeKey(key);
                }
            }
        }

        private static void ProcessHotkeyModeKey(ConsoleKeyInfo key)
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

            // '/' - Enter command mode
            if (key.KeyChar == '/')
            {
                EnterCommandMode();
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

                // F6-F9: Quest tests
                case ConsoleKey.F6:
                    GameMaster.PlayQuest(30001, "Theft");
                    break;
                case ConsoleKey.F7:
                    GameMaster.PlayQuest(30002, "Blade");
                    break;
                case ConsoleKey.F8:
                    GameMaster.PlayQuest(30003, "Sword");
                    break;
                case ConsoleKey.F9:
                    GameMaster.PlayQuest(30004, "BeastTaming");
                    break;

                // F10: Export life attributes
                case ConsoleKey.F10:
                    GameMaster.ExportLifeAttributes();
                    break;

                // F11: Toggle TCP log
                case ConsoleKey.F11:
                    GameMaster.ToggleTcpLog();
                    break;
            }
        }

        private static void EnterCommandMode()
        {
            _commandMode = true;
            _commandBuffer.Clear();
            System.Console.Write("\n> /");
        }

        private static void ExitCommandMode()
        {
            _commandMode = false;
            _commandBuffer.Clear();
            System.Console.WriteLine();
        }

        private static void ProcessCommandModeKey(ConsoleKeyInfo key)
        {
            // ESC - Cancel command input
            if (key.Key == ConsoleKey.Escape)
            {
                System.Console.WriteLine(" (cancelled)");
                ExitCommandMode();
                return;
            }

            // Enter - Execute command
            if (key.Key == ConsoleKey.Enter)
            {
                System.Console.WriteLine();
                ExecuteCommand(_commandBuffer.ToString().Trim());
                ExitCommandMode();
                return;
            }

            // Backspace - Delete last character
            if (key.Key == ConsoleKey.Backspace)
            {
                if (_commandBuffer.Length > 0)
                {
                    _commandBuffer.Length--;
                    System.Console.Write("\b \b");
                }
                return;
            }

            // Regular character input
            if (!char.IsControl(key.KeyChar))
            {
                _commandBuffer.Append(key.KeyChar);
                System.Console.Write(key.KeyChar);
            }
        }

        private static void ExecuteCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLower();
            var args = parts.Skip(1).ToArray();

            GameMaster.Result result;

            switch (command)
            {
                case "addgem":
                case "gem":
                    result = ExecuteAddGem(args);
                    break;

                case "help":
                case "?":
                    ShowHelp();
                    return;

                default:
                    Utils.Debug.Log.Warning("GM", $"Unknown command: {command}. Type 'help' for available commands.");
                    return;
            }

            if (result.Success)
            {
                Utils.Debug.Log.Info("GM", result.Message);
            }
            else
            {
                Utils.Debug.Log.Warning("GM", result.Message);
            }
        }

        private static GameMaster.Result ExecuteAddGem(string[] args)
        {
            // Usage: addgem <amount> [playerId]
            // Usage: addgem <playerId> <amount>
            
            if (args.Length == 0)
            {
                return GameMaster.Result.Fail("Usage: addgem <amount> [playerId]");
            }

            string playerId = null;
            int amount;

            if (args.Length == 1)
            {
                // addgem <amount>
                if (!int.TryParse(args[0], out amount))
                {
                    return GameMaster.Result.Fail($"Invalid amount: {args[0]}");
                }
            }
            else
            {
                // addgem <amount> <playerId> or addgem <playerId> <amount>
                if (int.TryParse(args[0], out amount))
                {
                    // First arg is amount, second is playerId
                    playerId = args[1];
                }
                else if (int.TryParse(args[1], out amount))
                {
                    // First arg is playerId, second is amount
                    playerId = args[0];
                }
                else
                {
                    return GameMaster.Result.Fail($"Invalid amount. Usage: addgem <amount> [playerId]");
                }
            }

            return GameMaster.AddGem(playerId, amount);
        }

        private static void ShowHelp()
        {
            Utils.Debug.Log.Info("GM", "=== Available Commands ===");
            Utils.Debug.Log.Info("GM", "  addgem <amount> [playerId] - Add gems to player");
            Utils.Debug.Log.Info("GM", "  help                       - Show this help");
            Utils.Debug.Log.Info("GM", "=== Hotkeys ===");
            Utils.Debug.Log.Info("GM", "  F1-F4  - Teleport to NPC");
            Utils.Debug.Log.Info("GM", "  F5     - Execute behavior tree");
            Utils.Debug.Log.Info("GM", "  F6-F9  - Play plot");
            Utils.Debug.Log.Info("GM", "  F10    - Export life attributes");
            Utils.Debug.Log.Info("GM", "  F11    - Toggle TCP log");
            Utils.Debug.Log.Info("GM", "  Delete - Clear console");
            Utils.Debug.Log.Info("GM", "  ESC    - Shutdown server");
        }
    }
}
