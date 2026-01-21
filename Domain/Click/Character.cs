using System;
using Logic;
using Utils;

namespace Domain.Click
{
    public class Character
    {
        private static void Do(Player player, Ability target)
        {
            player.Create<Logic.Option>(Logic.Option.Types.Operation, player, target);
        }
        public static  void On(params object[] args)
        {
            Player player = (Player)args[0];
            Ability element = (Ability)args[1];
            Do(player, element);
        }

    }
}
