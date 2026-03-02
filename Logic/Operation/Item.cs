using Data;

namespace Logic.Operation
{
    public class Item
    {
        public static global::Data.Option.Item Talk(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Talk, Text.Agent.Instance.Get((int)Type.Talk, player));
        }

        public static global::Data.Option.Item Use(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Use, Text.Agent.Instance.Get((int)Type.Use, player));
        }

        public static global::Data.Option.Item Give(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Give, Text.Agent.Instance.Get((int)Type.Give, player));
        }

        public static global::Data.Option.Item Pick(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Pick, Text.Agent.Instance.Get((int)Type.Pick, player));
        }

        public static global::Data.Option.Item Drop(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Drop, Text.Agent.Instance.Get((int)Type.Drop, player));
        }

        public static global::Data.Option.Item Equip(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Equip, Text.Agent.Instance.Get((int)Type.Equip, player));
        }

        public static global::Data.Option.Item Unequip(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.UnEquip, Text.Agent.Instance.Get((int)Type.UnEquip, player));
        }

        public static global::Data.Option.Item Cook(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Cook, Text.Agent.Instance.Get((int)Type.Cook, player));
        }

        public static global::Data.Option.Item Brew(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Brew, Text.Agent.Instance.Get((int)Type.Brew, player));
        }

        public static global::Data.Option.Item Forge(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Forge, Text.Agent.Instance.Get((int)Type.Forge, player));
        }

        public static global::Data.Option.Item Sewing(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Sewing, Text.Agent.Instance.Get((int)Type.Sewing, player));
        }

        public static global::Data.Option.Item Alchemy(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Alchemy, Text.Agent.Instance.Get((int)Type.Alchemy, player));
        }

        public static global::Data.Option.Item Attack(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Attack, Text.Agent.Instance.Get((int)Type.Attack, player));
        }

        public static global::Data.Option.Item Follow(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Follow, Text.Agent.Instance.Get((int)Type.Follow, player));
        }

        public static global::Data.Option.Item UnFollow(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.UnFollow, Text.Agent.Instance.Get((int)Type.UnFollow, player));
        }

        public static global::Data.Option.Item Enter(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Enter, Text.Agent.Instance.Get((int)Type.Enter, player));
        }

        public static global::Data.Option.Item Settings(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Settings, Text.Agent.Instance.Get((int)Type.Settings, player));
        }

        public static global::Data.Option.Item Mall(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.Mall, Text.Agent.Instance.Get((int)Type.Mall, player));
        }

        public static global::Data.Option.Item GoTo(Player player, global::Data.Ability target)
        {
            return Data.OptionHelper.BuildButton(Type.GoTo, Text.Agent.Instance.Get((int)Type.GoTo, player));
        }
    }
}
