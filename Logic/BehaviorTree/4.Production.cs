using Logic.Battle;
using Logic.Exchange;
using Data;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace Logic.BehaviorTree
{
    /// <summary>
    /// 生产业务类别 - 对应004xxxx系列节点
    /// 包含烹饪、酿造、锻造、缝纫、采集、狩猎、肢解等生产相关的行为树节点
    /// </summary>
    public class Production
    {

        public static Life Prey(Life life, Func<Life, bool> predicate = null)
        {
            if (life == null) return null;

            return global::Data.Agent.Instance.Content.Gets<Life>()
                .Where(l => l != life)
                .Where(l => IsHuntableCategory(l.Category))
                .Where(l => predicate == null || predicate(l))
                .FirstOrDefault();
        }

        private static bool IsHuntableCategory(global::Data.Life.Categories category)
        {
            return category == global::Data.Life.Categories.Demon || 
                   category == global::Data.Life.Categories.Animal;
        }

        public static Life FindBestPreyForMaster(Life companion)
        {
            if (companion == null) return null;
            if (companion.Leader is not global::Data.Player player) return null;
            if (companion.Map?.Scene == null) return null;

            var candidates = global::Data.Agent.Instance.Content.Gets<Life>()
                .Where(l => l != companion)
                .Where(l => l != player)
                .Where(l => l.Category == global::Data.Life.Categories.Animal)
                .Where(l => !l.State.Is(global::Data.Life.States.Unconscious))
                .Where(l => l.Map?.Scene == companion.Map.Scene)
                .Where(l => l.Leader == null);

            return candidates.OrderBy(animal => Math.Abs(animal.Level - player.Level)).FirstOrDefault();
        }
        public static bool IsCrop(Item item)
        {
            if (item == null) return false;
            return item.Config.Tags.HasPrefix("Generate") && item.Content.Has<Item>();
        }

        public static bool IsOre(Item item)
        {
            if (item == null) return false;
            return item.Config.Tags.HasPrefix("Drop");
        }

        public static bool IsOreStone(Item item)
        {
            if (item == null) return false;
            if (item.Type != Item.Types.Material) return false;
            var tags = item.Config.Tags;
            return tags.Has("Hard") || tags.Has("Conductive") || tags.Has("Divinity");
        }


        /// <summary>
        /// Condition:0041001 - 烹饪点是否可烹饪
        /// 检查最近的烹饪点是否有足够的材料可以烹饪
        /// </summary>
        [BehaviorCondition(0041001)]
        public static bool CanCookingPointCook(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            var point = life.Map.Content.Get<Item>(Cast.Agent.Instance.cook.IsPoint);
            if (point == null) return false;
            return Cast.Agent.Instance.cook.Availables(point).Count > 0;
        }

        /// <summary>
        /// Condition:0041002 - 酿造点是否可酿造
        /// 检查最近的酿造点是否有足够的材料可以酿造
        /// </summary>
        [BehaviorCondition(0041002)]
        public static bool CanBrewingPointBrew(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            var point = life.Map.Content.Get<Item>(Cast.Agent.Instance.compound.IsPoint);
            if (point == null) return false;
            return Cast.Agent.Instance.compound.Availables(point).Count > 0;
        }

        /// <summary>
        /// Condition:0041003 - 锻造点是否可锻造
        /// 检查最近的锻造点是否有足够的材料可以锻造
        /// </summary>
        [BehaviorCondition(0041003)]
        public static bool CanForgePointForge(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            var point = life.Map.Content.Get<Item>(Cast.Agent.Instance.smith.IsPoint);
            if (point == null) return false;
            return Cast.Agent.Instance.smith.Availables(point).Count > 0;
        }

        /// <summary>
        /// Condition:0041004 - 缝纫点是否可缝纫
        /// 检查最近的缝纫点是否有足够的材料可以缝纫
        /// </summary>
        [BehaviorCondition(0041004)]
        public static bool CanSewingPointSew(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            var point = life.Map.Content.Get<Item>(Cast.Agent.Instance.sew.IsPoint);
            if (point == null) return false;
            return Cast.Agent.Instance.sew.Availables(point).Count > 0;
        }

        /// <summary>
        /// Condition:0041005 - 是否位于烹饪店
        /// 检查角色是否在烹饪店内
        /// </summary>
        [BehaviorCondition(0041005)]
        public static bool IsInCookingShop(Character character)
        {
            if (character == null) return false;
            if (character.Map == null) return false;
            return character.Map.Type == Map.Types.Restaurant;
        }

        /// <summary>
        /// Condition:0041006 - 是否位于酿造店
        /// 检查角色是否在酿造店内
        /// </summary>
        [BehaviorCondition(0041006)]
        public static bool IsInBrewingShop(Character character)
        {
            if (character == null) return false;
            if (character.Map == null) return false;
            return character.Map.Type == Map.Types.Restaurant;
        }

        /// <summary>
        /// Condition:0041007 - 是否位于锻造店
        /// 检查角色是否在锻造店内
        /// </summary>
        [BehaviorCondition(0041007)]
        public static bool IsInForgeShop(Character character)
        {
            if (character == null) return false;
            if (character.Map == null) return false;
            return character.Map.Type == Map.Types.HeavyGearShop;
        }

        /// <summary>
        /// Condition:0041008 - 是否位于缝纫店
        /// 检查角色是否在缝纫店内
        /// </summary>
        [BehaviorCondition(0041008)]
        public static bool IsInSewingShop(Character character)
        {
            if (character == null) return false;
            if (character.Map == null) return false;
            return character.Map.Type == Map.Types.LightGearShop;
        }

        /// <summary>
        /// Condition:41009 - 采集目标是否存在
        /// 动态查找采集目标（资源点），限制在出生地及相邻场景
        /// </summary>
        [BehaviorCondition(41009)]
        public static bool HasCollectionTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.WorkingScenes == null || life.WorkingScenes.Count == 0) return false;
            
            return life.WorkingScenes.Any(scene => scene.Content.Has<Map>(m => m.Content.Has<Item>(IsCrop)));
        }

        /// <summary>
        /// Condition:41010 - 采集目标是否相邻
        /// 动态查找并检查采集目标是否在当前地图中
        /// </summary>
        [BehaviorCondition(41010)]
        public static bool IsCollectionTargetAdjacent(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            return life.Map.Content.Has<Item>(IsCrop);
        }

        /// <summary>
        /// Condition:41011 - 农拾目标是否存在
        /// 动态查找地图上的掉落物品作为农拾目标，限制在出生地及相邻场景
        /// </summary>
        [BehaviorCondition(41011)]
        public static bool HasFarmPickupTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.WorkingScenes == null || life.WorkingScenes.Count == 0) return false;
            
            return life.WorkingScenes.Any(scene => scene.Content.Has<Map>(m => m.Content.Has<Item>(i => i.Type == Item.Types.Material)));
        }

        /// <summary>
        /// Condition:41012 - 农拾目标是否相邻
        /// 检查农拾目标是否在当前地图中（即相邻位置）
        /// </summary>
        [BehaviorCondition(41012)]
        public static bool IsFarmPickupTargetAdjacent(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            return life.Map.Content.Has<Item>(i => i.Type == Item.Types.Material);
        }

        /// <summary>
        /// Condition:41013 - 肢解目标是否存在
        /// 动态查找可肢解的目标（已死亡的生物），限制在出生地及相邻场景
        /// </summary>
        [BehaviorCondition(41013)]
        public static bool HasDismemberTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.WorkingScenes == null || life.WorkingScenes.Count == 0) return false;
            
            Life prey = Prey(life, l => l.State.Is(global::Data.Life.States.Unconscious) && life.WorkingScenes.Contains(l.Map?.Scene));
            return prey != null;
        }

        /// <summary>
        /// Condition:41014 - 肢解目标是否相邻
        /// 检查肢解目标是否在当前地图中（即相邻位置）
        /// </summary>
        [BehaviorCondition(41014)]
        public static bool IsDismemberTargetAdjacent(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null)
                return false;
            if (life.Map == null)
                return false;
            if (life.Map.Scene == null)
                return false;
            Life prey = Prey(life, l => l.State.Is(global::Data.Life.States.Unconscious) && l.Map == life.Map);
            if (prey == null)
                return false;
            return true;
        }

        /// <summary>
        /// Condition:41015 - 狩猎目标是否存在
        /// 动态查找狩猎目标（敌对生物），限制在出生地及相邻场景
        /// </summary>
        [BehaviorCondition(41015)]
        public static bool HasHuntTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.WorkingScenes == null || life.WorkingScenes.Count == 0) return false;
            
            Life prey = Prey(life, l => !l.State.Is(global::Data.Life.States.Unconscious) && life.WorkingScenes.Contains(l.Map?.Scene));
            return prey != null;
        }

        /// <summary>
        /// Condition:41016 - 狩猎目标是否相邻
        /// 动态查找并检查狩猎目标是否在当前地图中
        /// </summary>
        [BehaviorCondition(41016)]
        public static bool IsHuntTargetAdjacent(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null)
                return false;
            if (life.Map == null)
                return false;
            if (life.Map.Scene == null)
                return false;
            Life prey = Prey(life, l => !l.State.Is(global::Data.Life.States.Unconscious) && l.Map == life.Map);
            if (prey == null)
                return false;
            return true;
        }

        /// <summary>
        /// Action:0042001 - 烹饪
        /// 在烹饪点执行烹饪制作
        /// </summary>
        [BehaviorAction(0042001)]
        public static bool CookFood(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            var point = life.Map.Content.Get<Item>(Cast.Agent.Instance.cook.IsPoint);
            if (point == null) return false;
            //if (!Utils.Mathematics.Probability(3)) return false;
            if (!life.Content.Has(Cast.Agent.Instance.cook.IsSkill, out Skill skill)) return false;
            Movement movement = skill.Content.RandomGet<Movement>(Cast.Agent.Instance.cook.IsMovement);
            if (movement == null) return false;
            Cast.Agent.Do(life, movement, point, point.Content.RandomGet<Part>());
            return true;
        }

        /// <summary>
        /// Action:0042002 - 酿造
        /// 在酿造点执行酿造制作
        /// </summary>
        [BehaviorAction(0042002)]
        public static bool BrewBeverage(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            var point = life.Map.Content.Get<Item>(Cast.Agent.Instance.compound.IsPoint);
            if (point == null) return false;
            if (!life.Content.Has(Cast.Agent.Instance.compound.IsSkill, out Skill skill)) return false;
            Movement movement = skill.Content.RandomGet<Movement>(Cast.Agent.Instance.compound.IsMovement);
            if (movement == null) return false;
            Cast.Agent.Do(life, movement, point, point.Content.RandomGet<Part>());
            return true;
        }

        /// <summary>
        /// Action:0042003 - 锻造
        /// 在锻造点执行装备制作
        /// </summary>
        [BehaviorAction(0042003)]
        public static bool ForgeEquipment(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            var point = life.Map.Content.Get<Item>(Cast.Agent.Instance.smith.IsPoint);
            if (point == null) return false;
            if (!life.Content.Has(Cast.Agent.Instance.smith.IsSkill, out Skill skill)) return false;
            Movement movement = skill.Content.RandomGet<Movement>(Cast.Agent.Instance.smith.IsMovement);
            if (movement == null) return false;
            Cast.Agent.Do(life, movement, point, point.Content.RandomGet<Part>());
            return true;
        }

        /// <summary>
        /// Action:0042004 - 缝纫
        /// 在缝纫点执行衣物制作
        /// </summary>
        [BehaviorAction(0042004)]
        public static bool SewClothes(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            var point = life.Map.Content.Get<Item>(Cast.Agent.Instance.sew.IsPoint);
            if (point == null) return false;
            if (!life.Content.Has(Cast.Agent.Instance.sew.IsSkill, out Skill skill)) return false;
            Movement movement = skill.Content.RandomGet<Movement>(Cast.Agent.Instance.sew.IsMovement);
            if (movement == null) return false;
            Cast.Agent.Do(life, movement, point, point.Content.RandomGet<Part>());
            return true;
        }

        /// <summary>
        /// Action:0042005 - 采集
        /// 动态查找采集目标并从容器中获取物品
        /// </summary>
        [BehaviorAction(0042005)]
        public static bool Collect(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            Item crop = life.Map.Content.Get<Item>(IsCrop);
            if (crop == null) return false;
            Item item = crop.Content.RandomGet<Item>();
            Exchange.Pick.Do(life, item, item.Count);
            return true;
        }

        /// <summary>
        /// Action:0042006 - 自动寻路采集目标
        /// 动态查找采集目标并自动寻路，限制在出生地及相邻场景
        /// </summary>
        [BehaviorAction(0042006)]
        public static bool AutoPathToCollectionTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.WorkingScenes == null || life.WorkingScenes.Count == 0) return false;
            
            var targetMaps = life.WorkingScenes.SelectMany(scene => scene.Content.Gets<Map>(m => m.Content.Has<Item>(IsCrop))).ToList();
            if (targetMaps.Count == 0) return false;
            
            var nearestMap = targetMaps.OrderBy(m => Move.Distance.Get(life.Map, m)).FirstOrDefault();
            if (nearestMap != null)
            {
                Logic.Move.Walk.FollowShortest(life, nearestMap);
            }
            return true;
        }

        /// <summary>
        /// Action:0042007 - 农拾
        /// 搜寻并拾取地图上的掉落物品，优先拾取生肉等食材
        /// </summary>
        [BehaviorAction(0042007)]
        public static bool FarmerPickupDroppedItems(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            if (!life.Map.Content.Has<Item>(i => i.Type == Item.Types.Material, out Item material)) return false;
            Exchange.Pick.Do(life, material, material.Count);
            return true;
        }

        /// <summary>
        /// Action:0042008 - 自动寻路农拾目标
        /// 动态查找农拾目标并自动寻路到目标位置，限制在出生地及相邻场景
        /// </summary>
        [BehaviorAction(0042008)]
        public static bool AutoPathToFarmPickupTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.WorkingScenes == null || life.WorkingScenes.Count == 0) return false;
            
            var targetMaps = life.WorkingScenes.SelectMany(scene => scene.Content.Gets<Map>(m => m.Content.Has<Item>(i => i.Type == Item.Types.Material))).ToList();
            if (targetMaps.Count == 0) return false;
            
            var nearestMap = targetMaps.OrderBy(m => Move.Distance.Get(life.Map, m)).FirstOrDefault();
            if (nearestMap != null)
            {
                Logic.Move.Walk.FollowShortest(life, nearestMap);
            }
            return true;
        }


        /// <summary>
        /// Action:0042010 - 肢解
        /// 肢解已死亡的生物获取材料
        /// </summary>
        [BehaviorAction(0042010)]
        public static bool DismemberTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            if (life.Map.Scene == null) return false;
            Life prey = Prey(life, l => l.State.Is(global::Data.Life.States.Unconscious));
            if (prey == null) return false;
            Logic.Battle.Round.DoAttack(life, prey);
            return true;
        }

        /// <summary>
        /// Action:0042011 - 自动寻路肢解目标
        /// 动态查找肢解目标并自动寻路，限制在出生地及相邻场景
        /// </summary>
        [BehaviorAction(0042011)]
        public static bool AutoPathToDismemberTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.WorkingScenes == null || life.WorkingScenes.Count == 0) return false;
            
            Life prey = Prey(life, l => l.State.Is(global::Data.Life.States.Unconscious) && life.WorkingScenes.Contains(l.Map?.Scene));
            if (prey == null) return false;
            Logic.Move.Walk.FollowShortest(life, prey.Map);
            return true;
        }

        /// <summary>
        /// Action:0042012 - 狩猎
        /// 动态查找狩猎目标并攻击
        /// </summary>
        [BehaviorAction(0042012)]
        public static bool HuntTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null)
                return false;
            if (life.Map == null)
                return false;
            if (life.Map.Scene == null)
                return false;
            Life prey = Prey(life, l => !l.State.Is(global::Data.Life.States.Unconscious) && l.Map == life.Map);
            if (prey == null)
                return false;
            Logic.Battle.Agent.Instance.Hostile(life, prey);
            return true;
        }

        /// <summary>
        /// Action:0042013 - 自动寻路狩猎目标
        /// 动态查找狩猎目标并自动寻路，限制在出生地及相邻场景
        /// </summary>
        [BehaviorAction(0042013)]
        public static bool AutoPathToHuntTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.WorkingScenes == null || life.WorkingScenes.Count == 0) return false;
            
            Life prey = Prey(life, l => !l.State.Is(global::Data.Life.States.Unconscious) && life.WorkingScenes.Contains(l.Map?.Scene));
            if (prey == null) return false;
            Logic.Move.Walk.FollowShortest(life, prey.Map);
            return true;
        }

        /// <summary>
        /// Condition:41021 - 适合主人的猎物是否存在
        /// 查找当前Scene中与主人等级最接近的Animal
        /// </summary>
        [BehaviorCondition(41021)]
        public static bool HasSuitablePreyForMaster(Character character)
        {
            var companion = character as global::Data.Life;
            if (companion == null) return false;
            if (companion.Leader is not global::Data.Player) return false;
            
            return FindBestPreyForMaster(companion) != null;
        }

        /// <summary>
        /// Action:42018 - 带领主人狩猎
        /// 同伴查找合适的猎物并带领主人前往
        /// </summary>
        [BehaviorAction(42018)]
        public static bool LeadMasterToHunt(Character character)
        {
            var companion = character as global::Data.Life;
            if (companion == null) return false;
            if (companion.Leader is not global::Data.Player player) return false;

            var target = FindBestPreyForMaster(companion);
            if (target == null) return false;

            Broadcast.Instance.Local(companion, [Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.CompanionLeadHunt)], ("sub", companion), ("target", target));

            Logic.Move.Walk.FollowShortest(companion, target.Map);

            Logic.Time.Agent.Instance.Scheduler.Once(300, (_) =>
            {
                if (player != null && !player.State.Is(global::Data.Life.States.Unconscious))
                {
                    Logic.Move.Walk.FollowShortest(player, target.Map);
                    Broadcast.Instance.Local(player, [Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.CompanionLeadHuntFollow)], ("sub", player), ("target", companion));
                }
            });

            return true;
        }

        /// <summary>
        /// Condition:41017 - 挖掘目标是否存在
        /// 全世界查找矿脉
        /// </summary>
        [BehaviorCondition(41017)]
        public static bool HasMiningTarget(Character character)
        {
            if (character == null) return false;
            return global::Data.Agent.Instance.Content.Has<Map>(m => m.Content.Has<Item>(IsOre));
        }

        /// <summary>
        /// Condition:41018 - 挖掘目标是否相邻
        /// 动态查找并检查挖掘目标是否在当前地图中
        /// </summary>
        [BehaviorCondition(41018)]
        public static bool IsMiningTargetAdjacent(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            return life.Map.Content.Has<Item>(IsOre);
        }

        /// <summary>
        /// Condition:41019 - 矿拾目标是否存在
        /// 全世界查找掉落的矿石
        /// </summary>
        [BehaviorCondition(41019)]
        public static bool HasOrePickupTarget(Character character)
        {
            if (character == null) return false;
            return global::Data.Agent.Instance.Content.Has<Map>(m => m.Content.Has<Item>(IsOreStone));
        }

        /// <summary>
        /// Condition:41020 - 矿拾目标是否相邻
        /// 检查矿拾目标是否在当前地图中（即相邻位置）
        /// </summary>
        [BehaviorCondition(41020)]
        public static bool IsOrePickupTargetAdjacent(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            return life.Map.Content.Has<Item>(IsOreStone);
        }

        /// <summary>
        /// Action:42014 - 挖掘
        /// 攻击矿脉Item使其掉落矿石
        /// </summary>
        [BehaviorAction(42014)]
        public static bool Mine(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            Item ore = life.Map.Content.Get<Item>(IsOre);
            if (ore == null) return false;
            
            var movement = SelectMiningMovement(life);
            if (movement != null)
            {
                Cast.Agent.Do(life, movement, ore, null);
            }
            else
            {
                ore.Durability = 0;
                Cast.Helper.ApplyDamageToItem(life, ore);
            }
            return true;
        }

        private static Movement SelectMiningMovement(global::Data.Life life)
        {
            if (life == null) return null;
            
            if (life.Content.Has<Skill>())
            {
                var skills = life.GetAllSkills();
                foreach (var skill in skills)
                {
                    var movement = skill.Content.RandomGet<Movement>(m => 
                        Cast.Agent.HasDamage(m) && 
                        Cast.Agent.IsCooldownReady(m) && 
                        skill.Config.IsMovementUnlocked(m.Config.Id, skill.Level) &&
                        (m.Config.require == null || m.Config.require.Evaluate(life)));
                    if (movement != null) return movement;
                }
            }
            
            return life.Content.RandomGet<Movement>(m => 
                Cast.Agent.HasDamage(m) && 
                Cast.Agent.IsCooldownReady(m) &&
                (m.Config.require == null || m.Config.require.Evaluate(life)));
        }

        /// <summary>
        /// Action:42015 - 自动寻路挖掘目标
        /// 全世界查找最近的矿脉并自动寻路
        /// </summary>
        [BehaviorAction(42015)]
        public static bool AutoPathToMiningTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            
            var nearestMap = Move.Distance.Nearest(life.Map, m => m.Content.Has<Item>(IsOre));
            if (nearestMap == null) return false;
            
            Logic.Move.Walk.FollowShortest(life, nearestMap);
            return true;
        }

        /// <summary>
        /// Action:42016 - 矿拾
        /// 拾取地图上的矿石材料
        /// </summary>
        [BehaviorAction(42016)]
        public static bool PickupOre(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            if (!life.Map.Content.Has<Item>(IsOreStone, out Item oreStone)) return false;
            Exchange.Pick.Do(life, oreStone, oreStone.Count);
            return true;
        }

        /// <summary>
        /// Action:42017 - 自动寻路矿拾目标
        /// 全世界查找最近的矿石并自动寻路
        /// </summary>
        [BehaviorAction(42017)]
        public static bool AutoPathToOrePickupTarget(Character character)
        {
            var life = character as global::Data.Life;
            if (life == null) return false;
            
            var nearestMap = Move.Distance.Nearest(life.Map, m => m.Content.Has<Item>(IsOreStone));
            if (nearestMap == null) return false;
            
            Logic.Move.Walk.FollowShortest(life, nearestMap);
            return true;
        }
    }
}