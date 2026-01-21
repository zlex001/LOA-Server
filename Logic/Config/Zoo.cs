using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Linq;
using Basic;
using Utils;

namespace Logic.Config
{
    public class Manager : Basic.Ability
    {
        #region Singleton 
        private static Manager instance;
        public static Manager Instance { get { if (instance == null) { instance = new Manager(); } return instance; } }
        #endregion

        // 添加配置索引缓存
        private Dictionary<int, Logic.Config.Skill> _skillCache = new Dictionary<int, Logic.Config.Skill>();
        private Dictionary<int, Logic.Config.Item> _itemCache = new Dictionary<int, Logic.Config.Item>();
        private Dictionary<int, Logic.Config.Movement> _movementCache = new Dictionary<int, Logic.Config.Movement>();
        
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
            LoadByRow<Plot>($"{Utils.Paths.Config}/Plot.csv");
            BuildIndexCache();
        }
        
        private void BuildIndexCache()
        {
            // 构建技能索引
            foreach (var skill in Content.Gets<Logic.Config.Skill>())
            {
                _skillCache[skill.Id] = skill;
            }
            
            // 构建道具索引
            foreach (var item in Content.Gets<Logic.Config.Item>())
            {
                _itemCache[item.Id] = item;
            }
            
            // 构建招式索引
            foreach (var movement in Content.Gets<Logic.Config.Movement>())
            {
                _movementCache[movement.Id] = movement;
            }
        }
        
        // 快速查找方法
        public T GetCached<T>(int id) where T : class
        {
            if (typeof(T) == typeof(Logic.Config.Skill))
            {
                return _skillCache.TryGetValue(id, out var skill) ? skill as T : null;
            }
            else if (typeof(T) == typeof(Logic.Config.Item))
            {
                return _itemCache.TryGetValue(id, out var item) ? item as T : null;
            }
            else if (typeof(T) == typeof(Logic.Config.Movement))
            {
                return _movementCache.TryGetValue(id, out var movement) ? movement as T : null;
            }
            
            // 回退到原始查找方式
            return Content.Get<T>(c => (c as Logic.Config.Ability)?.Id == id);
        }
    }
}
