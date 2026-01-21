using Logic;

namespace Domain.Operation
{
    public class Item
    {
        public static Logic.Option.Item Talk(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Talk, Text.Agent.Instance.Get((int)Type.Talk, player));
        }

        public static Logic.Option.Item Use(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Use, Text.Agent.Instance.Get((int)Type.Use, player));
        }

        public static Logic.Option.Item Give(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Give, Text.Agent.Instance.Get((int)Type.Give, player));
        }

        public static Logic.Option.Item Pick(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Pick, Text.Agent.Instance.Get((int)Type.Pick, player));
        }

        public static Logic.Option.Item Drop(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Drop, Text.Agent.Instance.Get((int)Type.Drop, player));
        }

        public static Logic.Option.Item Equip(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Equip, Text.Agent.Instance.Get((int)Type.Equip, player));
        }

        public static Logic.Option.Item Unequip(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.UnEquip, Text.Agent.Instance.Get((int)Type.UnEquip, player));
        }

        public static Logic.Option.Item Cook(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Cook, Text.Agent.Instance.Get((int)Type.Cook, player));
        }

        public static Logic.Option.Item Brew(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Brew, Text.Agent.Instance.Get((int)Type.Brew, player));
        }

        public static Logic.Option.Item Forge(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Forge, Text.Agent.Instance.Get((int)Type.Forge, player));
        }

        public static Logic.Option.Item Sewing(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Sewing, Text.Agent.Instance.Get((int)Type.Sewing, player));
        }

        public static Logic.Option.Item Alchemy(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Alchemy, Text.Agent.Instance.Get((int)Type.Alchemy, player));
        }

        public static Logic.Option.Item Attack(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Attack, Text.Agent.Instance.Get((int)Type.Attack, player));
        }

        public static Logic.Option.Item Follow(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Follow, Text.Agent.Instance.Get((int)Type.Follow, player));
        }

        public static Logic.Option.Item UnFollow(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.UnFollow, Text.Agent.Instance.Get((int)Type.UnFollow, player));
        }

        public static Logic.Option.Item Enter(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Enter, Text.Agent.Instance.Get((int)Type.Enter, player));
        }

        public static Logic.Option.Item Settings(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Settings, Text.Agent.Instance.Get((int)Type.Settings, player));
        }

        public static Logic.Option.Item Mall(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.Mall, Text.Agent.Instance.Get((int)Type.Mall, player));
        }

        public static Logic.Option.Item GoTo(Player player, Logic.Ability target)
        {
            return Logic.OptionHelper.BuildButton(Type.GoTo, Text.Agent.Instance.Get((int)Type.GoTo, player));
        }
    }
}
