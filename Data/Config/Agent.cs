using System;
using System.Collections.Generic;
using System.Linq;
using Basic;
using Utils;

namespace Data.Config
{

    public class Agent : Basic.Ability
    {
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }
        public readonly int[] Version = [10, 2, 12];
        public readonly int[] ClientVersion = [9, 7, 10];
        private Dictionary<int, global::Data.Config.Skill> _skillCache = new Dictionary<int, global::Data.Config.Skill>();
        private Dictionary<int, global::Data.Config.Item> _itemCache = new Dictionary<int, global::Data.Config.Item>();
        private Dictionary<int, global::Data.Config.Movement> _movementCache = new Dictionary<int, global::Data.Config.Movement>();
        public override void Init(params object[] args)
        {
            LoadByRow<BehaviorTree>($"{Utils.Paths.Config}/BehaviorTree.csv");
            LoadByRow<Item>($"{Utils.Paths.Config}/Item.csv");
            LoadByRow<Movement>($"{Utils.Paths.Config}/Movement.csv");
            LoadByRow<Skill>($"{Utils.Paths.Config}/Skill.csv");
            LoadByRow<Life>($"{Utils.Paths.Config}/Life.csv");
            LoadByRow<Scene>($"{Utils.Paths.Config}/Scene.csv");
            LoadByRow<Map>($"{Utils.Paths.Config}/Map.csv");
            LoadByRow<Maze>($"{Utils.Paths.Config}/Maze.csv");
            LoadByRow<Quest>($"{Utils.Paths.Config}/Quest.csv");
            LoadByRow<Dialogue>($"{Utils.Paths.Config}/Dialogue.csv");
            LoadByRow<Mall>($"{Utils.Paths.Config}/Mall.csv");
            BuildIndexCache();
        }
        private void BuildIndexCache()
        {
            foreach (var skill in Content.Gets<global::Data.Config.Skill>())
            {
                _skillCache[skill.Id] = skill;
            }

            foreach (var item in Content.Gets<global::Data.Config.Item>())
            {
                _itemCache[item.Id] = item;
            }
            foreach (var movement in Content.Gets<global::Data.Config.Movement>())
            {
                _movementCache[movement.Id] = movement;
            }
        }

        public T GetCached<T>(int id) where T : class
        {
            if (typeof(T) == typeof(global::Data.Config.Skill))
            {
                return _skillCache.TryGetValue(id, out var skill) ? skill as T : null;
            }
            else if (typeof(T) == typeof(global::Data.Config.Item))
            {
                return _itemCache.TryGetValue(id, out var item) ? item as T : null;
            }
            else if (typeof(T) == typeof(global::Data.Config.Movement))
            {
                return _movementCache.TryGetValue(id, out var movement) ? movement as T : null;
            }

            return Content.Get<T>(c => (c as global::Data.Config.Ability)?.Id == id);
        }

    }
}
