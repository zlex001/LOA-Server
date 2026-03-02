using Aop.Api.Domain;
using Data;
using Utils;

namespace Logic.Cast
{
    public class Agent
    {
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }

        private readonly Dictionary<Movement.Effect, Action<Life, Movement, Character, Part>> handlers = new();
        public Craft cook;
        public Craft compound;
        public Craft alchemize;
        public Craft sew;
        public Craft smith;

        public void Init()
        {
            cook = new Craft("Cook", Movement.Effect.Cook);
            compound = new Craft("Compound", Movement.Effect.Compound);
            alchemize = new Craft("Alchemize", Movement.Effect.Alchemize);
            sew = new Craft("Sew", Movement.Effect.Sew);
            smith = new Craft("Smith", Movement.Effect.Smith);
            Register(Movement.Effect.Incise, Incise.Do);
            Register(Movement.Effect.Contuse, Contuse.Do);
            Register(Movement.Effect.Dodge, Dodge.Do);
            Register(Movement.Effect.Block, Block.Do);
            Register(Movement.Effect.Steal, Steal.Do);
        }

        public void Register(Movement.Effect effect, Action<Life, Movement, Character, Part> handler)
        {
            if (handler == null) return;
            if (handlers.ContainsKey(effect))
            {
                handlers[effect] += handler;
            }
            else
            {
                handlers[effect] = handler;
            }
        }

        public static bool Has(Movement movement, Movement.Effect effect)
        {
            return movement?.Effects != null && movement.Effects.Contains(effect);
        }

        public static bool HasDamage(Movement movement)
        {
            if (movement?.Effects == null) return false;
            return movement.Effects.Contains(Movement.Effect.Incise) || movement.Effects.Contains(Movement.Effect.Contuse);
        }

        public static bool IsCooldownReady(Movement movement)
        {
            if (movement?.Config == null) return false;
            if (movement.Config.cd <= 0) return true;
            return (Logic.Time.Agent.Now - movement.LastCastTime).TotalSeconds >= movement.Config.cd;
        }

        public static void Do(Life sub, Movement movement, Character obj, Part part)
        {
            if (movement == null) return;

            movement.LastCastTime = Logic.Time.Agent.Now;
            movement.ResetPartHitIndexes();

            Skill parentSkill = movement.Parent as Skill;
            
            int hitCount = 1;
            if (Has(movement, Movement.Effect.MultiHit))
            {
                if (parentSkill != null)
                {
                    hitCount = movement.CalculateHitCount(parentSkill.Level);
                    movement.HitCount = hitCount;
                }
                else
                {
                    hitCount = movement.HitCount;
                }
            }
            
            Broadcast.Instance.Battle(obj, [movement.Config.description], ("sub", sub), ("obj", obj), ("movement", movement), ("weapon", Exchange.Agent.GetHandleItem(sub)), ("part", part));

            List<Character> availableTargets = null;
            if (hitCount > 1 && movement.MultiHitTargetPattern == Movement.TargetPattern.RandomTarget)
            {
                availableTargets = Battle.Target.Get(sub);
                if (availableTargets.Count == 0)
                {
                    availableTargets = new List<Character> { obj };
                }
            }

            for (int i = 0; i < hitCount; i++)
            {
                movement.CurrentHitIndex = i;
                
                Character currentTarget = obj;
                Part targetPart = part;
                
                if (hitCount > 1)
                {
                    if (movement.MultiHitTargetPattern == Movement.TargetPattern.RandomTarget && availableTargets != null)
                    {
                        currentTarget = availableTargets[Utils.Random.Instance.Next(availableTargets.Count)];
                        if (currentTarget is Life randomLife)
                        {
                            targetPart = Battle.Target.Aim(movement, randomLife);
                        }
                    }
                    else if (currentTarget is Life targetLife)
                    {
                        targetPart = Battle.Target.Aim(movement, targetLife);
                    }
                }
                
                foreach (var effect in movement.Effects)
                {
                    if (effect == Movement.Effect.MultiHit || effect == Movement.Effect.Cleave) continue;
                    if (Instance.handlers.TryGetValue(effect, out var handler))
                    {
                        handler(sub, movement, currentTarget, targetPart);
                    }
                }
            }
            
            movement.CurrentHitIndex = 0;

            if (parentSkill != null)
            {
                int center = 1;
                if (obj is Life life) { center = life.Level; }
                if (obj is Item item) { center = item.Config.value; }
                parentSkill.Exp += (int)Utils.Mathematics.Gaussian(parentSkill.Level, center, 1, center);
            }
        }
    }
}
