using Logic;

namespace Domain.Perception
{
    public class Agent : Agent<Agent>
    {
        private static Agent instance;
        public static Agent Instance => instance ??= new Agent();

        // Space index: Characters grouped by Map (incremental maintenance)
        private readonly Dictionary<Map, HashSet<Character>> _mapIndex = new();

        // View cache: Visible characters for each Life (lazy calculation + cache)
        private readonly Dictionary<Life, ViewCache> _viewCache = new();
        
        // Track which character types (Config.Id) each player has seen
        private readonly Dictionary<int, HashSet<int>> _seenConfigIds = new();
        
        /// <summary>
        /// Event fired when a player first sees a character type (by Config.Id)
        /// Args: (Player player, Character character)
        /// </summary>
        public event Action<Player, Character> FirstSeen;

        public void Init()
        {
            // Set delegate for Net layer to get visible characters
            Logic.Player.GetVisibleCharacters = GetVisibleCharacters;
            
            // Listen for Map add/remove to register content listeners
            Logic.Agent.Instance.Content.Add.Register(typeof(Map), OnMapAdded);
            Logic.Agent.Instance.Content.Remove.Register(typeof(Map), OnMapRemoved);

            // Initialize for existing maps and their characters
            foreach (var map in Logic.Agent.Instance.Content.Gets<Map>())
            {
                RegisterMap(map);
            }
        }

        private void OnMapAdded(params object[] args)
        {
            if (args.Length < 2) return;
            Map map = args[1] as Map;
            if (map == null) return;
            RegisterMap(map);
        }

        private void RegisterMap(Map map)
        {
            if (map == null) return;
            
            // Listen for Character add/remove on this map
            map.Content.Add.Register(typeof(Character), OnMapAddCharacter);
            map.Content.Remove.Register(typeof(Character), OnMapRemoveCharacter);
            
            // Add existing characters to index
            foreach (var character in map.Content.Gets<Character>())
            {
                AddToMapIndex(character, map);
            }
            
        }

        private void OnMapRemoved(params object[] args)
        {
            if (args.Length < 2) return;
            Map map = args[1] as Map;
            if (map == null) return;
            
            // Unregister listeners
            map.Content.Add.Unregister(typeof(Character), OnMapAddCharacter);
            map.Content.Remove.Unregister(typeof(Character), OnMapRemoveCharacter);
            
            // Remove all characters from this map's index
            if (_mapIndex.ContainsKey(map))
            {
                _mapIndex.Remove(map);
            }
        }

        private void OnMapAddCharacter(params object[] args)
        {
            if (args.Length < 2) return;
            Map map = args[0] as Map;
            Character character = args[1] as Character;
            if (map == null || character == null) return;
            
            AddToMapIndex(character, map);
            
            // Mark affected view caches as dirty
            InvalidateCachesNearMap(map);
        }

        private void OnMapRemoveCharacter(params object[] args)
        {
            if (args.Length < 2) return;
            Map map = args[0] as Map;
            Character character = args[1] as Character;
            if (map == null || character == null) return;
            
            RemoveFromMapIndex(character, map);
            
            // Mark affected view caches as dirty
            InvalidateCachesNearMap(map);
            
            // Remove from view cache if it's a Life
            if (character is Life life)
            {
                _viewCache.Remove(life);
            }
        }

        private void InvalidateCachesNearMap(Map map)
        {
            foreach (var (viewer, cache) in _viewCache)
            {
                if (cache.IsDirty) continue;
                if (viewer.Map == null) continue;

                if (IsInViewRange(viewer, map))
                {
                    cache.IsDirty = true;
                }
            }
        }

        private void AddToMapIndex(Character character, Map map)
        {
            if (map == null) return;

            if (!_mapIndex.TryGetValue(map, out var set))
            {
                set = new HashSet<Character>();
                _mapIndex[map] = set;
            }
            set.Add(character);
        }

        private void RemoveFromMapIndex(Character character, Map map)
        {
            if (map == null) return;

            if (_mapIndex.TryGetValue(map, out var set))
            {
                set.Remove(character);
                if (set.Count == 0)
                {
                    _mapIndex.Remove(map);
                }
            }
        }

        private bool IsInViewRange(Life viewer, Map targetMap)
        {
            if (viewer.Map == null || targetMap == null) return false;
            if (viewer.Map == targetMap) return true;

            // Copy isolation: only see maps in same Copy (or both not in any Copy)
            if (viewer.Map.Copy != targetMap.Copy) return false;

            int distance = Move.Distance.Get(viewer.Map, targetMap);
            return distance <= viewer.ViewScale && distance != int.MaxValue;
        }

        /// <summary>
        /// Invalidate a specific viewer's cache (e.g., when ViewScale changes)
        /// </summary>
        public void InvalidateCache(Life viewer)
        {
            if (_viewCache.TryGetValue(viewer, out var cache))
            {
                cache.IsDirty = true;
            }
        }

        /// <summary>
        /// Core query interface: Get all visible characters for a viewer
        /// </summary>
        public List<Character> GetVisibleCharacters(Life viewer)
        {
            if (viewer?.Map == null)
            {
                return new List<Character>();
            }

            // Check if cache is valid - must check map change too!
            // If viewer moved to a different map, cache is invalid even if not marked dirty
            if (_viewCache.TryGetValue(viewer, out var cache) && !cache.IsDirty && cache.CachedAtMap == viewer.Map)
            {
                return SortCharacters(viewer, cache.Characters);
            }

            // Recalculate
            var visible = CalculateVisibleCharacters(viewer);

            // Check for first-seen characters (only for players)
            if (viewer is Player player)
            {
                CheckFirstSeen(player, visible);
            }

            // Update cache
            _viewCache[viewer] = new ViewCache
            {
                Characters = visible,
                ViewScale = viewer.ViewScale,
                CachedAtMap = viewer.Map,
                IsDirty = false
            };

            return SortCharacters(viewer, visible);
        }
        
        private void CheckFirstSeen(Player player, HashSet<Character> visible)
        {
            int playerId = player.GetHashCode();
            if (!_seenConfigIds.TryGetValue(playerId, out var seenIds))
            {
                seenIds = new HashSet<int>();
                _seenConfigIds[playerId] = seenIds;
            }
            
            foreach (var character in visible)
            {
                if (character == player) continue;
                
                int configId = GetCharacterConfigId(character);
                if (configId == 0) continue;
                
                if (!seenIds.Contains(configId))
                {
                    seenIds.Add(configId);
                    FirstSeen?.Invoke(player, character);
                }
            }
        }
        
        /// <summary>
        /// Get Config.Id from a Character (Life or Item)
        /// </summary>
        public static int GetCharacterConfigId(Character character)
        {
            if (character is Life life) return life.Config?.Id ?? 0;
            if (character is Item item) return item.Config?.Id ?? 0;
            return 0;
        }
        
        /// <summary>
        /// Clear seen history for a player (e.g., when entering tutorial copy)
        /// </summary>
        public void ClearSeenHistory(Player player)
        {
            _seenConfigIds.Remove(player.GetHashCode());
        }

        /// <summary>
        /// Sort characters: viewer first, then by distance (ascending), then by HashCode for stability
        /// Filter out characters that are no longer in any map (race condition protection)
        /// </summary>
        private List<Character> SortCharacters(Life viewer, HashSet<Character> characters)
        {
            return characters
                .Where(c => c != null && (c == viewer || c.Map != null))
                .OrderBy(c => c == viewer ? 0 : 1)
                .ThenBy(c => GetDistance(viewer, c))
                .ThenBy(c => c.GetHashCode())
                .ToList();
        }

        private HashSet<Character> CalculateVisibleCharacters(Life viewer)
        {
            var result = new HashSet<Character>();
            
            // Include viewer itself (player should see themselves in the list)
            result.Add(viewer);

            // Create a snapshot to avoid collection modification during iteration
            var snapshot = _mapIndex.ToList();
            
            foreach (var (map, characters) in snapshot)
            {
                if (map == null || !IsInViewRange(viewer, map)) continue;

                // Create a snapshot of characters to avoid modification during iteration
                var charSnapshot = characters.ToList();
                foreach (var character in charSnapshot)
                {
                    if (character != null)
                    {
                        result.Add(character);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get the distance from viewer to a character (for UI display)
        /// </summary>
        public int GetDistance(Life viewer, Character target)
        {
            if (viewer?.Map == null) return int.MaxValue;
            if (target == null) return int.MaxValue;
            
            if (target is Item item)
            {
                var effectiveMap = GetItemEffectiveMap(item, viewer);
                if (effectiveMap == null) return int.MaxValue;
                if (effectiveMap == viewer.Map) return 0;
                return Move.Distance.Get(viewer.Map, effectiveMap);
            }
            
            if (target.Map == null) return int.MaxValue;
            if (viewer.Map == target.Map) return 0;
            return Move.Distance.Get(viewer.Map, target.Map);
        }

        private Map GetItemEffectiveMap(Item item, Life viewer)
        {
            if (item.Map != null) return item.Map;
            
            if (item.Parent is Part part)
            {
                if (part.Parent == viewer) return viewer.Map;
                if (part.Parent is Life owner) return owner.Map;
            }
            
            if (item.Parent is Item container)
            {
                return GetItemEffectiveMap(container, viewer);
            }
            
            return null;
        }

        /// <summary>
        /// Check if a specific character is visible to the viewer
        /// </summary>
        public bool IsVisible(Life viewer, Character target)
        {
            if (viewer?.Map == null || target?.Map == null) return false;
            if (viewer == target) return false;
            return IsInViewRange(viewer, target.Map);
        }

        /// <summary>
        /// Check if a specific map is visible to the viewer
        /// </summary>
        public bool IsVisible(Life viewer, Map targetMap)
        {
            if (viewer?.Map == null || targetMap == null) return false;
            return IsInViewRange(viewer, targetMap);
        }
    }

    /// <summary>
    /// View cache structure
    /// </summary>
    public class ViewCache
    {
        public HashSet<Character> Characters { get; set; } = new();
        public double ViewScale { get; set; }
        public Map CachedAtMap { get; set; }
        public bool IsDirty { get; set; } = true;
    }
}

