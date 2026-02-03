using Logic;

namespace Domain.Operation
{
    public class Excute
    {
        public static void Talk(Player player, Logic.Ability target)
        {
            if (target is Life life)
            {
                Domain.Talk.Agent.Instance.Do(player, life);
            }
        }

        public static void Use(Player player, Logic.Ability target)
        {
            if (target is Logic.Item item)
            {
                Domain.Use.Agent.Instance.Do(player, item);
            }
        }

        public static void Give(Player player, Logic.Ability target)
        {
            player.Create<Logic.Option>(Logic.Option.Types.Give, player, target);
        }

        public static void Pick(Player player, Logic.Ability target)
        {
            if (target is Logic.Item item)
            {
                if (item.Count > 1)
                {
                    player.Create<Option>(Option.Types.PickOrder, player, item);
                }
                else
                {
                    Exchange.Pick.Do(player, item, item.Count);
                    player.OptionBackward();
                }
            }
            else if (target is Life life)
            {
                Exchange.Pick.Do(player, life);
                player.OptionBackward();
            }
        }

        public static void Drop(Player player, Logic.Ability target)
        {
            if (target is Logic.Item item)
            {
                if (item.Count > 1)
                {
                    player.Create<Logic.Option>(Logic.Option.Types.DropOrder, player, target);
                }
                else
                {
                    Exchange.Drop.Do(player, item, item.Count);
                    player.OptionBackward();
                }
            }
            else if (target is Life life)
            {
                life.Bearer = null;
                player.OptionBackward();
            }
        }

        public static void Equip(Player player, Logic.Ability target)
        {
            Exchange.Equip.Instance.Do(player, target as Logic.Item);
        }

        public static void Unequip(Player player, Logic.Ability target)
        {
            Exchange.Unequip.Do(player, target as Logic.Item);
        }

        public static void Cook(Player player, Logic.Ability target)
        {
        }

        public static void Brew(Player player, Logic.Ability target)
        {
        }

        public static void Forge(Player player, Logic.Ability target)
        {
        }

        public static void Sewing(Player player, Logic.Ability target)
        {
        }

        public static void Alchemy(Player player, Logic.Ability target)
        {
        }

        public static void Attack(Player player, Logic.Ability target)
        {
            if (target is Life life)
            {
                Battle.Agent.Instance.Hostile(player, life);
            }
            else if (target is Logic.Item item)
            {
                AttackItem(player, item);
            }
            player.Remove<Logic.Option>();
        }

        private static void AttackItem(Player player, Logic.Item item)
        {
            var movement = SelectMovementForItem(player);
            if (movement != null)
            {
                Cast.Agent.Do(player, movement, item, null);
            }
            else
            {
                item.Durability = 0;
                Cast.Helper.ApplyDamageToItem(player, item);
            }
        }

        private static Movement SelectMovementForItem(Player player)
        {
            if (player.Content.Has<Skill>())
            {
                var skills = player.GetAllSkills();
                foreach (var skill in skills)
                {
                    var movement = skill.Content.RandomGet<Movement>(m => 
                        Cast.Agent.HasDamage(m) && 
                        Cast.Agent.IsCooldownReady(m) && 
                        skill.Config.IsMovementUnlocked(m.Config.Id, skill.Level) &&
                        (m.Config.require == null || m.Config.require.Evaluate(player)));
                    if (movement != null) return movement;
                }
            }
            
            return player.Content.RandomGet<Movement>(m => 
                Cast.Agent.HasDamage(m) && 
                Cast.Agent.IsCooldownReady(m) &&
                (m.Config.require == null || m.Config.require.Evaluate(player)));
        }

        public static void Follow(Player player, Logic.Ability target)
        {
            if (target is Life life) 
            {
                Move.Follow.Do(player, life);
                player.OptionBackward();
            }
        }

        public static void UnFollow(Player player, Logic.Ability target)
        {
            Move.Follow.DoUnFollow(player);
            player.OptionBackward();
        }

        public static void Enter(Player player, Logic.Ability target)
        {
            if (target is Logic.Item item) 
            {
                Move.Enter.Do(player, item);
                player.OptionBackward();
            }
        }

        public static void Settings(Player player, Logic.Ability target)
        {
            player.Create<Logic.Option>(Logic.Option.Types.Settings, player, player);
        }

        public static void Mall(Player player, Logic.Ability target)
        {
            player.Create<Logic.Option>(Logic.Option.Types.Mall, player, player);
        }

        public static void GoTo(Player player, Logic.Ability target)
        {
            if (player.State.Is(Logic.Life.States.Unconscious))
            {
                string tip = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.CannotWalkWhileUnconscious, player.Language);
                Net.Tcp.Instance.Send(player, new Net.Protocol.FlyTip(tip));
                return;
            }

            if (target is Character character && character.Map != null)
            {
                // Fire global event for player going to character (tutorial, etc.)
                Logic.Agent.Instance.monitor.Fire(Logic.Character.Event.GoTo, player, character);
                
                player.ClickTarget = character.Map;
                BehaviorTree.Agent.SetBehaviorTree(player, Logic.Constant.OneTimePathfinding);
                player.Remove<Logic.Option>();
            }
        }
    }
}
