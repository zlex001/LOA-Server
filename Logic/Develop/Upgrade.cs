
using Data;

namespace Logic.Develop
{
    public class Upgrade
    {
        // Pre-calculated experience tables based on cultivation system anchors
        private static int[] _characterExpTable;
        private static int[] _skillExpTable;

        public static void Init()
        {
            InitExpTables();

            global::Data.Agent.Instance.Content.Add.Register(typeof(global::Data.Life), OnAddLife);
            global::Data.Agent.Instance.Content.Remove.Register(typeof(global::Data.Life), OnRemoveLife);

            global::Data.Agent.Instance.Content.Add.Register(typeof(global::Data.Skill), OnAddSkill);
            global::Data.Agent.Instance.Content.Remove.Register(typeof(global::Data.Skill), OnRemoveSkill);

            global::Data.Agent.Instance.Content.Add.Register(typeof(global::Data.Player), OnAddPlayer);
            global::Data.Agent.Instance.Content.Remove.Register(typeof(global::Data.Player), OnRemovePlayer);
        }

        private static void InitExpTables()
        {
            int maxLevel = global::Data.Constant.CharacterMaxLevel;
            int ceilingMax = global::Data.Constant.CharacterCeilingMax;
            int ceilingExponent = global::Data.Constant.CharacterCeilingExponent;
            int battleDuration = global::Data.Constant.CharacterBattleDuration;
            int premiumMultiplier = global::Data.Constant.CharacterPremiumMultiplier;
            int expPerKillMultiplier = global::Data.Constant.CharacterExpPerKillMultiplier;
            int useDuration = global::Data.Constant.SkillUseDuration;

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
            if (nextLevel > global::Data.Constant.CharacterMaxLevel)
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
                string message = Logic.Text.Agent.Instance.GetDynamic(
                    global::Data.Text.Labels.LevelUp, 
                    player.Language, 
                    ("level", v.ToString()));
                Net.Tcp.Instance.Send(player, new Net.Protocol.FlyTip(message));
                Logic.Broadcast.Instance.System(player, [message]);
            }
        }

        private static void OnAddLife(params object[] args)
        {
            global::Data.Life life = (global::Data.Life)args[1];
            SetNextExp(life);
            life.data.after.Register(global::Data.Life.Data.Level, AfterLifeLevelChanged);
            life.data.after.Register(global::Data.Life.Data.Exp, AfterLifeExpChanged);
        }

        private static void OnRemoveLife(params object[] args)
        {
            global::Data.Life life = (global::Data.Life)args[1];
            life.data.after.Unregister(global::Data.Life.Data.Level, AfterLifeLevelChanged);
            life.data.after.Unregister(global::Data.Life.Data.Exp, AfterLifeExpChanged);
        }

        private static void SetNextExp(global::Data.Skill skill)
        {
            int nextLevel = skill.Level + 1;
            if (nextLevel > global::Data.Constant.SkillMaxLevel)
            {
                skill.NextExp = int.MaxValue;
                return;
            }
            skill.NextExp = _skillExpTable[nextLevel];
        }

        private static void AfterSkillExpChanged(params object[] args)
        {
            int v = (int)args[0];
            global::Data.Skill skill = (global::Data.Skill)args[1];
            if (v >= skill.NextExp)
            {
                skill.Level += 1;
            }
        }

        private static void AfterSkillLevelChanged(params object[] args)
        {
            int v = (int)args[0];
            global::Data.Skill skill = (global::Data.Skill)args[1];
            SetNextExp(skill);
        }

        private static void OnAddSkill(params object[] args)
        {
            global::Data.Skill skill = (global::Data.Skill)args[1];
            SetNextExp(skill);
            skill.data.after.Register(global::Data.Skill.Data.Level, AfterSkillLevelChanged);
            skill.data.after.Register(global::Data.Skill.Data.Exp, AfterSkillExpChanged);
        }

        private static void OnRemoveSkill(params object[] args)
        {
            global::Data.Skill skill = (global::Data.Skill)args[1];
            skill.data.after.Unregister(global::Data.Skill.Data.Level, AfterSkillLevelChanged);
            skill.data.after.Unregister(global::Data.Skill.Data.Exp, AfterSkillExpChanged);
        }

        private static void OnAddPlayer(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[1];
            player.Content.Add.Register(typeof(global::Data.Skill), OnPlayerAddSkill);
            player.Content.Remove.Register(typeof(global::Data.Skill), OnPlayerRemoveSkill);
        }

        private static void OnRemovePlayer(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[1];
            player.Content.Add.Unregister(typeof(global::Data.Skill), OnPlayerAddSkill);
            player.Content.Remove.Unregister(typeof(global::Data.Skill), OnPlayerRemoveSkill);
        }

        private static void OnPlayerAddSkill(params object[] args)
        {
            global::Data.Skill skill = (global::Data.Skill)args[1];
            skill.monitor.Register(global::Data.Skill.Event.Upgrade, OnPlayerSkillUpgrade);
        }

        private static void OnPlayerRemoveSkill(params object[] args)
        {
            global::Data.Skill skill = (global::Data.Skill)args[1];
            skill.monitor.Unregister(global::Data.Skill.Event.Upgrade, OnPlayerSkillUpgrade);
        }

        private static void OnPlayerSkillUpgrade(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[0];
            global::Data.Skill skill = (global::Data.Skill)args[1];
            int o = (int)args[2];
            int v = (int)args[3];
            Broadcast.Instance.System(player, new object[] { Logic.Text.Agent.Instance.Id(global::Data.Text.Labels.SkillUpgrade) }, ("skill", skill), ("level", v.ToString()));
        }

        // Public accessors for experience tables (for debugging/display purposes)
        public static int GetCharacterExpForLevel(int level)
        {
            if (level < 0 || level > global::Data.Constant.CharacterMaxLevel) return 0;
            return _characterExpTable[level];
        }

        public static int GetSkillExpForLevel(int level)
        {
            if (level < 0 || level > global::Data.Constant.SkillMaxLevel) return 0;
            return _skillExpTable[level];
        }
    }
}
