using Logic;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.X509;

namespace Domain.Exchange
{
    public class Pick : Domain.Agent<Pick>
    {
        private static int _bearerChangeDepth = 0;
        private const int MaxBearerChangeDepth = 10;

        public static void Init()
        {
            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Life), OnAddLife);
        }

        public static bool Can(Life sub, Ability target)
        {
            if (target is Item item)
            {
                // 自己的物品不需要"拾取"（已在手上或背包里）
                if (Agent.GetItems(sub).Contains(item)) return false;
                // 自己装备的物品也不需要"拾取"
                if (Agent.GetEquipments(sub).Contains(item)) return false;
                
                double maxCarry = Mathematics.Instance.GetMaxCarry(sub);
                return item.Config.weight <= maxCarry;
            }

            if (target is Life obj)
            {
                return Can(sub, obj);
            }

            return false;
        }

        public static bool Can(Life sub, Life obj)
        {
            if (sub == null || obj == null || sub == obj) return false;
            if (!obj.State.Is(Life.States.Unconscious)) return false;
            if (sub.Map != obj.Map) return false;
            if (obj.Leader != null) return false;
            return true;
        }
        public static bool Can(Life sub, Item obj, int count)
        {
            if (sub == null) return false;
            if (obj == null) return false;
            if (count < 0) return false;
            if (count > obj.Count) return false;
            return true;
        }

        public static void Do(Life sub, Life obj)
        {
            if (Can(sub, obj))
            {
                obj.Bearer = sub;
                
                // 处理角色身上的装备抢劫
                List<Item> equipments = Agent.GetEquipments(obj);
                
                if (equipments != null && equipments.Count > 0)
                {
                    foreach (Item equipment in equipments)
                    {
                        if (equipment == null)
                        {
                            continue;
                        }
                        
                        // 一定触发抢劫判定
                        bool isRobbery = Justice.Robbery.Judgment(sub, obj, equipment);
                        
                        if (isRobbery)
                        {
                            // 获取双方Atk属性点
                            var takerAtkPoints = Mathematics.Instance.AttributePoint(sub.Grade, sub.Level);
                            var ownerAtkPoints = Mathematics.Instance.AttributePoint(obj.Grade, obj.Level);
                            
                            double takerAtkPoint = takerAtkPoints.ContainsKey(Life.Attributes.Atk) ? takerAtkPoints[Life.Attributes.Atk] : 0;
                            double ownerAtkPoint = ownerAtkPoints.ContainsKey(Life.Attributes.Atk) ? ownerAtkPoints[Life.Attributes.Atk] : 0;
                            
                            // 使用极限比例函数计算成功率（f参数设为1，可根据需要调整）
                            // 如果ownerAtkPoint为0，则成功率设为1（抢夺者必定成功）
                            double successRate = ownerAtkPoint > 0 ? Utils.Mathematics.Ratio(takerAtkPoint, ownerAtkPoint, 1.0) : 1.0;
                            
                            // 检查是否有目击者
                            bool hasWitnesses = Justice.Agent.HasWitnesses(sub, out _);
                            
                            // 概率判定
                            bool success = Utils.Mathematics.Probability(successRate);
                            
                            if (success)
                            {
                                // 成功：直接从Part移除装备并转移到抢夺者
                                if (equipment.Parent is Part part)
                                {
                                    part.Remove(equipment);
                                    Receive.Do(sub, equipment, equipment.Count);
                                    Broadcast.Instance.Local(sub, [Domain.Text.Agent.Instance.Id(Logic.Text.Labels.Pick)], ("sub", sub), ("target", equipment), ("count", equipment.Count.ToString()), ("obj", obj));
                                }
                            }
                            
                            // 无论成功与否都触发抢劫量刑
                            Justice.Robbery.Sentence(sub, equipment, hasWitnesses);
                        }
                    }
                }
            }
        }


        public static void Do(Life sub, Item obj, int count)
        {
            if (Can(sub, obj, count))
            {
                // 检查物品是否在角色身上（装备在Part上）
                Life owner = GetOwner(obj);
                if (owner != null && owner != sub && obj.Parent is Part)
                {
                    // 从角色身上拾取装备，触发抢劫逻辑
                    // 一定触发抢劫判定
                    if (Justice.Robbery.Judgment(sub, owner, obj))
                    {
                        // 获取双方Atk属性点
                        var takerAtkPoints = Mathematics.Instance.AttributePoint(sub.Grade, sub.Level);
                        var ownerAtkPoints = Mathematics.Instance.AttributePoint(owner.Grade, owner.Level);
                        
                        double takerAtkPoint = takerAtkPoints.ContainsKey(Life.Attributes.Atk) ? takerAtkPoints[Life.Attributes.Atk] : 0;
                        double ownerAtkPoint = ownerAtkPoints.ContainsKey(Life.Attributes.Atk) ? ownerAtkPoints[Life.Attributes.Atk] : 0;
                        
                        // 使用极限比例函数计算成功率（f参数设为1，可根据需要调整）
                        // 如果ownerAtkPoint为0，则成功率设为1（抢夺者必定成功）
                        double successRate = ownerAtkPoint > 0 ? Utils.Mathematics.Ratio(takerAtkPoint, ownerAtkPoint, 1.0) : 1.0;
                        
                        // 检查是否有目击者
                        bool hasWitnesses = Justice.Agent.HasWitnesses(sub, out _);
                        
                        // 概率判定
                        bool success = Utils.Mathematics.Probability(successRate);
                        
                        if (success)
                        {
                            // 成功：直接从Part移除装备并转移到抢夺者
                            if (obj.Parent is Part part)
                            {
                                part.Remove(obj);
                                Receive.Do(sub, obj, count);
                                Broadcast.Instance.Local(sub, [Domain.Text.Agent.Instance.Id(Logic.Text.Labels.Pick)], ("sub", sub), ("target", obj), ("count", count.ToString()), ("obj", owner));
                                obj.monitor.Fire(Item.Event.Picked, sub);
                            }
                        }
                        
                        // 无论成功与否都触发抢劫量刑
                        Justice.Robbery.Sentence(sub, obj, hasWitnesses);
                        
                        return;
                    }
                }
                
                // 原有的偷窃逻辑（物品在地图上）
                if (Justice.Theft.Judgment(sub, obj))
                {
                    if (sub.Content.Has(Cast.Steal.IsSkill, out Skill skill))
                    {
                        Movement movement = skill.Content.RandomGet<Movement>(Cast.Steal.IsMovement);
                        Cast.Agent.Do(sub, movement, obj, obj.Content.RandomGet<Part>());

                        if (Cast.Steal.Probability(sub, obj, count))
                        {
                            Broadcast.Instance.Local(sub, [Domain.Text.Agent.Instance.Id(Logic.Text.Labels.Pick)], ("sub", sub), ("target", obj), ("count", count.ToString()), ("obj", obj.Parent));
                            Receive.Do(sub, obj, count);
                            obj.monitor.Fire(Item.Event.Picked, sub);
                        }
                        else
                        {
                            Justice.Theft.Sentence(sub, obj);
                        }
                    }
                    else
                    {
                        Justice.Theft.Sentence(sub, obj);
                    }
                }
                else
                {
                    Broadcast.Instance.Local(sub, [Domain.Text.Agent.Instance.Id(Logic.Text.Labels.Pick)], ("sub", sub), ("target", obj), ("count", count.ToString()), ("obj", obj.Parent));
                    Receive.Do(sub, obj, count);
                    obj.monitor.Fire(Item.Event.Picked, sub);
                }
            }
        }

        private static Life GetOwner(Item item)
        {
            if (item == null)
                return null;

            if (item.Parent is Part part)
            {
                return part.Parent as Life;
            }

            if (item.Parent is Item container)
            {
                return GetOwner(container);
            }

            return null;
        }

        private static Movement GetStealMovement(Life life)
        {
            if (life == null)
                return null;

            var stealSkill = life.Content.Gets<Skill>(s => s.Content.Has<Movement>(m => m.Effects.Contains(Movement.Effect.Steal)))
                .OrderByDescending(s => s.Level)
                .FirstOrDefault();

            if (stealSkill == null)
                return null;

            return stealSkill.Content.Get<Movement>(m => m.Effects.Contains(Movement.Effect.Steal));
        }

        private static void OnAddLife(params object[] args)
        {
            Life life = (Life)args[1];
            life.data.before.Register(Life.Data.Bearer, OnBeforeLifeBearerChanged);
        }

        private static void OnBeforeLifeBearerChanged(params object[] args)
        {
            Life o = (Life)args[0];
            Life v = (Life)args[1];
            Life life = (Life)args[2];
            if (o != null)
            {
                Instance.Unregister(o.data.after, Basic.Element.Data.Parent);
                Broadcast.Instance.Local(life, [Domain.Text.Agent.Instance.Id(Logic.Text.Labels.Drop)], ("sub", o), ("obj", life));
            }

            if (v != null)
            {
                Instance.Register(v.data.after, Basic.Element.Data.Parent, life, OnAfterBearerParentChanged);
                Broadcast.Instance.Local(life, [Domain.Text.Agent.Instance.Id(Logic.Text.Labels.Carry)], ("sub", v), ("obj", life));
            }
        }

        private static void OnAfterBearerParentChanged(Life cargo, object[] args)
        {
            if (_bearerChangeDepth >= MaxBearerChangeDepth)
            {
                return;
            }

            try
            {
                _bearerChangeDepth++;

                if (args == null || args.Length == 0)
                {
                    return;
                }

                if (cargo == null)
                {
                    return;
                }

                if (cargo.Bearer == null)
                {
                    return;
                }

                Basic.Manager parent = args[0] as Basic.Manager;
                if (parent == null)
                {
                    return;
                }

                if (parent is Map map && cargo.Map != map)
                {
                    map.AddAsParent(cargo);
                }
            }
            finally
            {
                _bearerChangeDepth--;
            }
        }
    }
}