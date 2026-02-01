using System;
using Logic;
using Utils;

namespace Domain.Click
{
    public class Character
    {
        private static void Do(Player player, Ability target)
        {
            Utils.Debug.Log.Info("OPTION", $"[Click.Character] Creating Operation Option for target={target?.GetType().Name}, targetHash={target?.GetHashCode()}");
            player.Create<Logic.Option>(Logic.Option.Types.Operation, player, target);
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
