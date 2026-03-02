using Data;

namespace Logic.Battle
{
    public static class Round
    {
        public static void Do(Life life)
        {
            if (life == null) return;
            
            if (life.State.Is(global::Data.Life.States.Unconscious))
            {
                life.Round++;
                return;
            }
            
            var targets = Target.Get(life);
            if (targets.Count == 0)
            {
                life.Round++;
                return;
            }
            
            var defender = targets.FirstOrDefault();
            if (!(defender is Life targetLife))
            {
                life.Round++;
                return;
            }
            
            var movement = SelectMovement(life);
            if (movement == null)
            {
                life.Round++;
                return;
            }
            
            if (CheckDodge(targetLife, life))
            {
                ExecuteDodge(targetLife, life);
            }
            else
            {
                ExecuteAttack(life, targetLife, movement);
            }

            life.Round++;
        }

        public static void DoAttack(Life attacker, Life defender)
        {
            if (attacker == null || defender == null) return;

            var movement = SelectMovement(attacker);
            if (movement != null)
            {
                ExecuteAttack(attacker, defender, movement);
            }
        }

        private static Movement SelectMovement(Life attacker)
        {
            if (attacker == null) return null;

            Movement movement = null;
            
            if (attacker.Content.Has<Skill>())
            {
                var skills = attacker.GetAllSkills();
                
                foreach (var skill in skills)
                {
                    movement = skill.Content.RandomGet<Movement>(m => 
                        Cast.Agent.HasDamage(m) && 
                        Cast.Agent.IsCooldownReady(m) && 
                        skill.Config.IsMovementUnlocked(m.Config.Id, skill.Level) &&
                        (m.Config.require == null || m.Config.require.Evaluate(attacker)));
                    if (movement != null)
                    {
                        break;
                    }
                }
                
                if (movement == null)
                {
                    movement = attacker.Content.RandomGet<Movement>(m => 
                        Cast.Agent.HasDamage(m) && 
                        Cast.Agent.IsCooldownReady(m) &&
                        (m.Config.require == null || m.Config.require.Evaluate(attacker)));
                }
            }
            else
            {
                movement = attacker.Content.RandomGet<Movement>(m => 
                    Cast.Agent.HasDamage(m) && 
                    Cast.Agent.IsCooldownReady(m) &&
                    (m.Config.require == null || m.Config.require.Evaluate(attacker)));
            }
            
            return movement;
        }

        private static bool CheckDodge(Life defender, Life attacker)
        {
            return false;
        }

        private static void ExecuteDodge(Life defender, Life attacker)
        {
        }

        private static void ExecuteAttack(Life attacker, Life defender, Movement movement)
        {
            if (attacker == null || defender == null || movement == null) return;

            if (Cast.Agent.Has(movement, Movement.Effect.Cleave))
            {
                int targetCount = 3;
                if (movement.Parent is Skill skill)
                {
                    targetCount = movement.CalculateCleaveTargetCount(skill.Level);
                    movement.CleaveTargetCount = targetCount;
                }
                else
                {
                    targetCount = movement.CleaveTargetCount;
                }

                var targets = Target.Get(attacker);
                foreach (var target in targets.Take(targetCount))
                {
                    if (target is Life life)
                    {
                        var part = Target.Aim(movement, life);
                        Cast.Agent.Do(attacker, movement, target, part);
                    }
                }
            }
            else
            {
                var part = Target.Aim(movement, defender);
                Cast.Agent.Do(attacker, movement, defender, part);
            }
        }
    }
}

