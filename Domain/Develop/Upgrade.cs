
using Logic;

namespace Domain.Develop
{
    public class Upgrade
    {
        // Pre-calculated experience tables based on cultivation system anchors
        private static int[] _characterExpTable;
        private static int[] _skillExpTable;

        public static void Init()
        {
            InitExpTables();

            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Life), OnAddLife);
            Logic.Agent.Instance.Content.Remove.Register(typeof(Logic.Life), OnRemoveLife);

            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Skill), OnAddSkill);
            Logic.Agent.Instance.Content.Remove.Register(typeof(Logic.Skill), OnRemoveSkill);

            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Player), OnAddPlayer);
            Logic.Agent.Instance.Content.Remove.Register(typeof(Logic.Player), OnRemovePlayer);
        }

        private static void InitExpTables()
        {
            int maxLevel = Logic.Constant.CharacterMaxLevel;
            int ceilingMax = Logic.Constant.CharacterCeilingMax;
            int ceilingExponent = Logic.Constant.CharacterCeilingExponent;
            int battleDuration = Logic.Constant.CharacterBattleDuration;
            int premiumMultiplier = Logic.Constant.CharacterPremiumMultiplier;
            int expPerKillMultiplier = Logic.Constant.CharacterExpPerKillMultiplier;
            int useDuration = Logic.Constant.SkillUseDuration;

            double denominator = Math.Pow(maxLevel, ceilingExponent) - 1;

            // Character experience table
            // Exp(level) = cumulative experience needed to reach that level
            _characterExpTable = new int[maxLevel + 2];
            _characterExpTable[0] = 0;
            _characterExpTable[1] = 0;

            for (int level = 2; level <= maxLevel; level++)
            {
                // Ceiling(level) and Ceiling(level-1)
                int ceiling = (int)(ceilingMax / denominator * (Math.Pow(level, ceilingExponent) - 1));
                int ceilingPrev = (int)(ceilingMax / denominator * (Math.Pow(level - 1, ceilingExponent) - 1));

                // Floor = Ceiling * PremiumMultiplier
                int floor = ceiling * premiumMultiplier;
                int floorPrev = ceilingPrev * premiumMultiplier;

                // ΔFloor
                int deltaFloor = floor - floorPrev;

                // ΔKills = max(1, ΔFloor / BattleDuration)
                int deltaKills = Math.Max(1, deltaFloor / battleDuration);

                // ΔExp = ΔKills × (level-1) × ExpPerKillMultiplier
                int deltaExp = deltaKills * (level - 1) * expPerKillMultiplier;

                // Cumulative experience
                _characterExpTable[level] = _characterExpTable[level - 1] + deltaExp;
            }

            // Skill experience table
            // Exp(level) = cumulative experience (uses) needed to reach that level
            _skillExpTable = new int[maxLevel + 2];
            _skillExpTable[0] = 0;
            _skillExpTable[1] = 0;

            for (int level = 2; level <= maxLevel; level++)
            {
                int ceiling = (int)(ceilingMax / denominator * (Math.Pow(level, ceilingExponent) - 1));
                int ceilingPrev = (int)(ceilingMax / denominator * (Math.Pow(level - 1, ceilingExponent) - 1));
                int floor = ceiling * premiumMultiplier;
                int floorPrev = ceilingPrev * premiumMultiplier;
                int deltaFloor = floor - floorPrev;

                // ΔUses = max(1, ΔFloor / UseDuration)
                int deltaUses = Math.Max(1, deltaFloor / useDuration);

                // ΔExp = ΔUses (ExpPerUse = 1)
                _skillExpTable[level] = _skillExpTable[level - 1] + deltaUses;
            }
        }

        private static void SetNextExp(Life life)
        {
            int nextLevel = life.Level + 1;
            if (nextLevel > Logic.Constant.CharacterMaxLevel)
            {
                life.NextExp = int.MaxValue;
                return;
            }
            life.NextExp = _characterExpTable[nextLevel];
        }

        private static void AfterLifeExpChanged(params object[] args)
        {
            int v = (int)args[0];
            Life life = (Life)args[1];
            if (v >= life.NextExp)
            {
                life.Level += 1;
            }
        }

        private static void AfterLifeLevelChanged(params object[] args)
        {
            int v = (int)args[0];
            Life life = (Life)args[1];
            SetNextExp(life);
            Agent.UpdateAttributes(life);
            
            // Send level up notification for Player
            if (life is Player player)
            {
                string message = Domain.Text.Agent.Instance.GetDynamic(
                    Logic.Text.Labels.LevelUp, 
                    player.Language, 
                    ("level", v.ToString()));
                Net.Tcp.Instance.Send(player, new Net.Protocol.FlyTip(message));
                Domain.Broadcast.Instance.System(player, [message]);
            }
        }

        private static void OnAddLife(params object[] args)
        {
            Logic.Life life = (Logic.Life)args[1];
            SetNextExp(life);
            life.data.after.Register(Logic.Life.Data.Level, AfterLifeLevelChanged);
            life.data.after.Register(Logic.Life.Data.Exp, AfterLifeExpChanged);
        }

        private static void OnRemoveLife(params object[] args)
        {
            Logic.Life life = (Logic.Life)args[1];
            life.data.after.Unregister(Logic.Life.Data.Level, AfterLifeLevelChanged);
            life.data.after.Unregister(Logic.Life.Data.Exp, AfterLifeExpChanged);
        }

        private static void SetNextExp(Logic.Skill skill)
        {
            int nextLevel = skill.Level + 1;
            if (nextLevel > Logic.Constant.SkillMaxLevel)
            {
                skill.NextExp = int.MaxValue;
                return;
            }
            skill.NextExp = _skillExpTable[nextLevel];
        }

        private static void AfterSkillExpChanged(params object[] args)
        {
            int v = (int)args[0];
            Logic.Skill skill = (Logic.Skill)args[1];
            if (v >= skill.NextExp)
            {
                skill.Level += 1;
            }
        }

        private static void AfterSkillLevelChanged(params object[] args)
        {
            int v = (int)args[0];
            Logic.Skill skill = (Logic.Skill)args[1];
            SetNextExp(skill);
        }

        private static void OnAddSkill(params object[] args)
        {
            Logic.Skill skill = (Logic.Skill)args[1];
            SetNextExp(skill);
            skill.data.after.Register(Logic.Skill.Data.Level, AfterSkillLevelChanged);
            skill.data.after.Register(Logic.Skill.Data.Exp, AfterSkillExpChanged);
        }

        private static void OnRemoveSkill(params object[] args)
        {
            Logic.Skill skill = (Logic.Skill)args[1];
            skill.data.after.Unregister(Logic.Skill.Data.Level, AfterSkillLevelChanged);
            skill.data.after.Unregister(Logic.Skill.Data.Exp, AfterSkillExpChanged);
        }

        private static void OnAddPlayer(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[1];
            player.Content.Add.Register(typeof(Logic.Skill), OnPlayerAddSkill);
            player.Content.Remove.Register(typeof(Logic.Skill), OnPlayerRemoveSkill);
        }

        private static void OnRemovePlayer(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[1];
            player.Content.Add.Unregister(typeof(Logic.Skill), OnPlayerAddSkill);
            player.Content.Remove.Unregister(typeof(Logic.Skill), OnPlayerRemoveSkill);
        }

        private static void OnPlayerAddSkill(params object[] args)
        {
            Logic.Skill skill = (Logic.Skill)args[1];
            skill.monitor.Register(Logic.Skill.Event.Upgrade, OnPlayerSkillUpgrade);
        }

        private static void OnPlayerRemoveSkill(params object[] args)
        {
            Logic.Skill skill = (Logic.Skill)args[1];
            skill.monitor.Unregister(Logic.Skill.Event.Upgrade, OnPlayerSkillUpgrade);
        }

        private static void OnPlayerSkillUpgrade(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[0];
            Logic.Skill skill = (Logic.Skill)args[1];
            int o = (int)args[2];
            int v = (int)args[3];
            Broadcast.Instance.System(player, new object[] { Domain.Text.Agent.Instance.Id(Logic.Text.Labels.SkillUpgrade) }, ("skill", skill), ("level", v.ToString()));
        }

        // Public accessors for experience tables (for debugging/display purposes)
        public static int GetCharacterExpForLevel(int level)
        {
            if (level < 0 || level > Logic.Constant.CharacterMaxLevel) return 0;
            return _characterExpTable[level];
        }

        public static int GetSkillExpForLevel(int level)
        {
            if (level < 0 || level > Logic.Constant.SkillMaxLevel) return 0;
            return _skillExpTable[level];
        }
    }
}
