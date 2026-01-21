using Logic;

namespace Domain.Operation
{
    public enum Type
    {
        Talk = 2000,
        Attack = 2001,
        Settings = 2002,
        Signs = 2004,
        Pick = 2005,
        Drop = 2006,
        Equip = 2007,
        UnEquip = 2008,
        Give = 2009,
        Abandon = 2010,
        Use = 2011,
        Follow = 2012,
        UnFollow = 2013,
        Buy = 2015,
        Sell = 2016,
        Cook = 2018,
        Brew = 2019,
        Forge = 2020,
        Sewing = 2021,
        Alchemy = 2022,
        Enter = 2023,
        Mall = 2024,
        GoTo = 2025,
    }
    public class Agent
    {
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }

        // Check if target is in the same map as player (required for most interactions)
        private static bool IsSameMap(Player player, Logic.Ability target)
        {
            if (player?.Map == null) return false;
            if (target is Logic.Item) return true; // Items are always accessible (including nested items in containers)
            if (target is Character character)
            {
                return character.Map == player.Map;
            }
            return true; // Non-character targets are always accessible
        }

        private bool CanAttack(Player player, Logic.Ability target)
        {
            if (!IsSameMap(player, target)) return false; // Attack requires same map
            if (target is Life life)
            {
                return Battle.Agent.Instance.CanHostile(player, life);
            }
            else if (target is Logic.Item item)
            {
                return IsAttackableResource(item);
            }
            return false;
        }

        private bool IsAttackableResource(Logic.Item item)
        {
            if (item == null) return false;
            return item.Parent is not Logic.Part;
        }

        public void Init()
        {
            // Talk: requires same map
            Display.Agent.Instance.Register(Type.Talk, (p, t) => IsSameMap(p, t) && t is Life life && Talk.Agent.Instance.Can(p, life), (p, t) => new List<Option.Item> { Item.Talk(p, t) }, (p, t, i) => Excute.Talk(p, t));
            // Use: requires same map
            Display.Agent.Instance.Register(Type.Use, (p, t) => IsSameMap(p, t) && t is Logic.Item item && Use.Agent.Instance.Can(p, item), (p, t) => new List<Option.Item> { Item.Use(p, t) }, (p, t, i) => Excute.Use(p, t));
            // Production: always false for now
            Display.Agent.Instance.Register(Type.Cook, (p, t) => false, (p, t) => new List<Option.Item> { Item.Cook(p, t) }, (p, t, i) => Excute.Cook(p, t));
            Display.Agent.Instance.Register(Type.Brew, (p, t) => false, (p, t) => new List<Option.Item> { Item.Brew(p, t) }, (p, t, i) => Excute.Brew(p, t));
            Display.Agent.Instance.Register(Type.Forge, (p, t) => false, (p, t) => new List<Option.Item> { Item.Forge(p, t) }, (p, t, i) => Excute.Forge(p, t));
            Display.Agent.Instance.Register(Type.Sewing, (p, t) => false, (p, t) => new List<Option.Item> { Item.Sewing(p, t) }, (p, t, i) => Excute.Sewing(p, t));
            Display.Agent.Instance.Register(Type.Alchemy, (p, t) => false, (p, t) => new List<Option.Item> { Item.Alchemy(p, t) }, (p, t, i) => Excute.Alchemy(p, t));
            // Give: requires same map
            Display.Agent.Instance.Register(Type.Give, (p, t) => IsSameMap(p, t) && Exchange.Give.Can(p, t), (p, t) => new List<Option.Item> { Item.Give(p, t) }, (p, t, i) => Excute.Give(p, t));
            // Pick: requires same map
            Display.Agent.Instance.Register(Type.Pick, (p, t) => IsSameMap(p, t) && Exchange.Pick.Can(p, t), (p, t) => new List<Option.Item> { Item.Pick(p, t) }, (p, t, i) => Excute.Pick(p, t));
            // Drop: operates on own items, no map check needed
            Display.Agent.Instance.Register(Type.Drop, (p, t) => Exchange.Drop.Can(p, t), (p, t) => new List<Option.Item> { Item.Drop(p, t) }, (p, t, i) => Excute.Drop(p, t));
            // Equip/UnEquip: operates on own items, no map check needed
            Display.Agent.Instance.Register(Type.Equip, (p, t) => Exchange.Equip.Instance.Can(p, t as Logic.Item), (p, t) => new List<Option.Item> { Item.Equip(p, t) }, (p, t, i) => Excute.Equip(p, t));
            Display.Agent.Instance.Register(Type.UnEquip, (p, t) => Exchange.Unequip.Can(p, t as Logic.Item), (p, t) => new List<Option.Item> { Item.Unequip(p, t) }, (p, t, i) => Excute.Unequip(p, t));
            // Attack: requires same map (checked in CanAttack)
            Display.Agent.Instance.Register(Type.Attack, (p, t) => CanAttack(p, t), (p, t) => new List<Option.Item> { Item.Attack(p, t) }, (p, t, i) => Excute.Attack(p, t));
            // Follow: requires same map
            Display.Agent.Instance.Register(Type.Follow, (p, t) => IsSameMap(p, t) && Move.Follow.Can(p, t), (p, t) => new List<Option.Item> { Item.Follow(p, t) }, (p, t, i) => Excute.Follow(p, t));
            // UnFollow: operates on leader relationship, no map check needed
            Display.Agent.Instance.Register(Type.UnFollow, (p, t) => p.Leader != null && t == p.Leader, (p, t) => new List<Option.Item> { Item.UnFollow(p, t) }, (p, t, i) => Excute.UnFollow(p, t));
            // Enter: requires same map
            Display.Agent.Instance.Register(Type.Enter, (p, t) => IsSameMap(p, t) && t is Logic.Item item && Move.Enter.Can(p, item), (p, t) => new List<Option.Item> { Item.Enter(p, t) }, (p, t, i) => Excute.Enter(p, t));
            // Settings/Mall: operates on self, no map check needed
            Display.Agent.Instance.Register(Type.Settings, (p, t) => p == t, (p, t) => new List<Option.Item> { Item.Settings(p, t) }, (p, t, i) => Excute.Settings(p, t));
            Display.Agent.Instance.Register(Type.Mall, (p, t) => p == t, (p, t) => new List<Option.Item> { Item.Mall(p, t) }, (p, t, i) => Excute.Mall(p, t));
            // GoTo: only for remote targets (different map)
            Display.Agent.Instance.Register(Type.GoTo, (p, t) => !IsSameMap(p, t) && t is Character, (p, t) => new List<Option.Item> { Item.GoTo(p, t) }, (p, t, i) => Excute.GoTo(p, t));
        }


    }
}
