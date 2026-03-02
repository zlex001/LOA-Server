using System;
using Data;
using Utils;

namespace Logic.Click
{
    public class Character
    {
        private static void Do(Player player, Ability target)
        {
            // Validate target before creating option
            if (target == null)
            {
                Utils.Debug.Log.Warning("OPTION", $"[Click.Character] Target is null, cannot create option");
                return;
            }
            
            // Check if target is destroyed or invalid
            if (target is global::Data.Character character && character.Map == null && !(character.Parent is Part))
            {
                Utils.Debug.Log.Warning("OPTION", $"[Click.Character] Target character has no map and is not equipped, may be destroyed. Type={target.GetType().Name}");
                return;
            }
            
            Utils.Debug.Log.Info("OPTION", $"[Click.Character] Creating Operation Option for target={target?.GetType().Name}, targetHash={target?.GetHashCode()}");
            player.Create<global::Data.Option>(global::Data.Option.Types.Operation, player, target);
        }
        public static  void On(params object[] args)
        {
            Player player = (Player)args[0];
            Ability element = (Ability)args[1];
            Utils.Debug.Log.Info("OPTION", $"[Click.Character.On] element={element?.GetType().Name}, elementHash={element?.GetHashCode()}");
            Do(player, element);
        }

    }
}
