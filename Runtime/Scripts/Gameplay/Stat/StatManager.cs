using System;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier.Gameplay
{
    /// <summary>
    /// Interface for a stat definition used by <see cref="StatManager"/>.
    /// </summary>
    public interface IStatDefinition
    {
        string Name { get; }
        float DefaultValue { get; }
        float MinValue { get; }
        float MaxValue { get; }

        bool IsSameStatAs(IStatDefinition other);
    }

    /// <summary>
    /// Manages stat definitions, computes final stat values by applying modifiers, and coordinates 
    /// dependencies between different stat systems. Orchestrates any stat-related operations 
    /// within a specific domain (e.g., character stats, weapon stats).
    /// Uses lazy evaluation with caching to optimize performance - stat values are only computed 
    /// when requested via <see cref="TryGetStatValue"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Dependencies:</b></para>
    /// StatManagers can depend on other StatManagers through a hierarchical execution system.
    /// When a dependency's stat changes, this manager automatically invalidates and recomputes 
    /// any affected stats. Dependencies are processed in execution order, allowing predictable 
    /// modifier application (e.g., base character stats -> equipment stats -> temporary buffs).
    /// 
    /// <para><b>Usage Pattern:</b></para>
    /// 1. Create with available stats and optional dependencies<br/>
    /// 2. Add/remove modifiers as game state changes<br/>
    /// 3. Listen to <see cref="OnStatInvalidated"/> for invalidation notifications<br/>
    /// 4. Fetch new effective values with <see cref="TryGetStatValue"/> as needed
    /// </remarks>
    public abstract class StatManager
    {
        /// <summary>
        /// Event raised when a the modifiers of a stat are changed.
        /// Users are then expected to pull the stat value from the cache, hence
        /// evaluating the stat modifiers and caching the new value.
        /// </summary>
        #pragma warning disable CS0067
        public virtual event Action<IStatDefinition> OnStatInvalidated;
        #pragma warning restore CS0067

        /// <summary>
        /// Returns the value of the stat. If the stat is not available in the cache, it will return 0.
        /// </summary>
        /// <param name="stat">The stat to get the value of.</param>
        /// <param name="value"></param>
        /// <returns>The value of the stat.</returns>
        public abstract bool TryGetStatValue(IStatDefinition stat, out float value);

        /// <summary>
        /// Returns the stat modifiers for the stat.
        /// </summary>
        /// <param name="stat">The stat to get the modifiers of.</param>
        /// <param name="statModifiers">The stat modifiers.</param>
        /// <returns>True if the stat modifiers were found, false otherwise.</returns>
        public abstract bool TryGetStatModifiers(IStatDefinition stat, out IReadOnlyList<IStatModifier> statModifiers);

        /// <summary>
        /// Refreshes the stat value cache if needed.
        /// </summary>
        public abstract void RefreshStats();

        public abstract IEnumerable<IStatDefinition> GetAvailableStats();

        protected abstract void OnDependencyStatChanged(IStatDefinition stat);
    }

    public class StatManager<T> : StatManager
        where T : class, IStatDefinition
    {
        public override event Action<IStatDefinition> OnStatInvalidated;

        // The latest evaluated stats values.
        private readonly Dictionary<T, float> m_LatestStatsValue;

        // The stat modifiers of the stat for the cache. Doesn't include dependencies.
        private Dictionary<T, SortedList<int, IStatModifier>> m_ModifiersPerStat;

        // The stat modifiers of the stat for the cache sorted by execution order including dependencies.
        private readonly Dictionary<T, List<IStatModifier>> m_LatestFlattenedModifiersPerStat;

        /// <summary>
        /// The list of dependencies. The key is the execution order of the dependency.
        /// </summary>
        private SortedList<int, Dependency> m_DependencyList = new();

        /// <summary>
        /// The compatible stats per stat.
        /// A compatible stat means that the stat can use the dependency stat modifiers to compute its value.
        /// For example, a weapon stat (Firerate) can use the character stats (Cooldown) to compute its value.
        /// </summary>
        private Dictionary<T, HashSet<IStatDefinition>> m_CompatibleStatDefsPerStat = new();

        // Working set (buffer) for the stat modifiers.
        private readonly SortedList<int, IList<IStatModifier>> m_StatModifierWorkingSet = new(4);

        // The list of stats that are dirty and need to be refreshed.
        // Used for lazy refresh of the stats value cache.
        private readonly HashSet<T> m_InvalidatedStats = new(4);

        // The list of stat modifiers that are dirty and need to be refreshed.
        private readonly List<IStatModifier> m_InvalidatedModifiers = new(4);

        private int m_CacheExecutionOrder = 0;
        
        #region Public API
        /// <summary>
        /// Constructor for the stat cache. Note that each dependency execution order needs to be unique.
        /// The execution order is used to determine the order of execution of the dependencies.
        /// The lower the value, the earlier the dependency is executed.
        /// </summary>
        /// <param name="availableStats">The list of stats that are available in the cache.</param>
        /// <param name="dependencies">The list of dependencies that are used to compute stats values. The key is the execution order of the dependency.</param>
        /// <param name="cacheModifierExecutionOrder">The execution order of the stat cache.</param>
        /// <example>
        /// For a given:
        /// - Dependency 1 "Character stats" with execution order 100
        /// - Dependency 2 "Weapon stats" with execution order 200,
        ///     -> Cache with execution order 0 will execute before the dependencies.
        ///     -> Cache with execution order 300 will execute after the dependencies.
        ///     -> Cache with execution order 100 or 200 will throw an exception.
        /// </example>
        public StatManager(
            IReadOnlyList<T> availableStats,
            IReadOnlyList<KeyValuePair<int, Dependency>> dependencies = null, 
            int cacheModifierExecutionOrder = 0)
        {
            m_CacheExecutionOrder = cacheModifierExecutionOrder;
            m_LatestStatsValue = new Dictionary<T, float>(availableStats.Count);
            m_LatestFlattenedModifiersPerStat = new Dictionary<T, List<IStatModifier>>(availableStats.Count);
            m_ModifiersPerStat = new Dictionary<T, SortedList<int, IStatModifier>>(availableStats.Count);

            foreach (var stat in availableStats)
            {
                m_LatestStatsValue[stat] = stat.DefaultValue;
                m_LatestFlattenedModifiersPerStat[stat] = new List<IStatModifier>();
                m_ModifiersPerStat[stat] = new SortedList<int, IStatModifier>();
                m_InvalidatedStats.Add(stat);
            }

            InjectDependencies(dependencies);

            RefreshStatsValueCache();
        }

        public void RemoveDependency(int executionOrder)
        {
            if (m_DependencyList.TryGetValue(executionOrder, out var dependency))
            {
                foreach (var compatibleStat in dependency.StatDependencies)
                {
                    RemoveDependencyCompatibleStats(compatibleStat.Key, compatibleStat.Value);
                }

                dependency.DependencyProcessor.OnStatInvalidated -= OnDependencyStatChanged;
                m_DependencyList.Remove(executionOrder);
            }
        }

        public override bool TryGetStatValue(IStatDefinition stat, out float value)
        {
            return TryGetStatValue(stat as T, out value);
        }

        /// <summary>
        /// Returns the value of the stat. If the stat is not available in the cache, it will return 0.
        /// </summary>
        /// <param name="stat">The stat to get the value of.</param>
        /// <param name="value"></param>
        /// <returns>The value of the stat.</returns>
        public bool TryGetStatValue(T stat, out float value)
        {
            if (stat == null || !m_LatestStatsValue.ContainsKey(stat))
            {
                value = 0;
                return false;
            }

            // Refresh the stat value cache (if needed).
            RefreshStatValueCache(stat);
            value = m_LatestStatsValue[stat];
            return true;
        }

        public override bool TryGetStatModifiers(IStatDefinition stat, out IReadOnlyList<IStatModifier> statModifiers)
        {
            return TryGetStatModifiers(stat as T, out statModifiers);
        }

        /// <summary>
        /// Returns all the stat modifier (including dependencies) that affect the stat sorted by order of execution.
        /// This is expensive and it is recommended to cache the list or modified stat.
        /// </summary>
        /// <param name="stat">The stat to get the modifiers of.</param>
        /// <param name="statModifiers"></param>
        /// <returns>The stat modifiers.</returns>
        public bool TryGetStatModifiers(T stat, out IReadOnlyList<IStatModifier> statModifiers)
        {
            statModifiers = null;

            // If invalid or not available in the cache, early out.
            if (stat == null || !m_LatestStatsValue.ContainsKey(stat))
            {
                return false;
            }

            // Refresh only the stat value cache (if needed).
            RefreshStatValueCache(stat);

            // If the list of stat modifiers is already cached, return it.
            if (m_LatestFlattenedModifiersPerStat.TryGetValue(stat, out var cachedStatModifiers))
            {
                statModifiers = cachedStatModifiers;
                return true;
            }

            return false;
        }

        public override IEnumerable<IStatDefinition> GetAvailableStats()
        {
            return m_LatestStatsValue.Keys;
        }

        /// <summary>
        /// Adds a modifier to the stat.
        /// </summary>
        /// <param name="stat">The stat to add the modifier to.</param>
        /// <param name="modifier">The modifier to add.</param>
        /// <param name="refresh">If true, recompute the stat.</param>
        /// <returns>True if the modifier was added, false otherwise.</returns>
        public bool AddModifier(T stat, IStatModifier modifier)
        {
            if (stat == null || modifier == null || !m_ModifiersPerStat.ContainsKey(stat))
            {
                return false;
            }

            // Find the next available execution order.
            int executionOrder = modifier.ExecutionOrder;
            while (m_ModifiersPerStat[stat].ContainsKey(executionOrder))
            {
                ++executionOrder;
            }

            m_ModifiersPerStat[stat].Add(executionOrder, modifier);
            m_InvalidatedStats.Add(stat);
            OnStatInvalidated?.Invoke(stat);
            return true;
        }

        /// <summary>
        /// Removes a modifier from the stat.
        /// </summary>
        /// <param name="stat">The stat to remove the modifier from.</param>
        /// <param name="modifier">The modifier to remove.</param>
        /// <returns>True if the modifier was removed, false otherwise.</returns>
        public bool RemoveModifier(T stat, IStatModifier modifier)
        {
            if (stat == null || modifier == null || !m_ModifiersPerStat.ContainsKey(stat))
            {
                return false;
            }

            if (m_ModifiersPerStat[stat].Remove(modifier.ExecutionOrder))
            {
                m_InvalidatedStats.Add(stat);
                OnStatInvalidated?.Invoke(stat);
                //OnCachedStatChanged?.Invoke(stat);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears all the modifiers from the cache.
        /// </summary>
        public void ClearModifiers()
        {
            foreach (var stat in m_ModifiersPerStat.Keys)
            {
                ClearModifiers(stat);
            }
        }

        /// <summary>
        /// Clears all the modifiers from the stat.
        /// </summary>
        /// <param name="stat">The stat to clear the modifiers from.</param>
        public void ClearModifiers(T stat)
        {
            if (stat == null)
            {
                return;
            }

            m_ModifiersPerStat[stat].Clear();
            m_InvalidatedStats.Add(stat);
        }

        /// <summary>
        /// Clears all the modifiers from the cache.
        /// </summary>
        /// <param name="source">The source of the modifiers to clear.</param>
        public void ClearModifiers(object source)
        {
            if (source == null)
            {
                return;
            }

            foreach (var stat in m_ModifiersPerStat.Keys)
            {
                foreach (var modifier in m_ModifiersPerStat[stat])
                {
                    if (modifier.Value.Source == source)
                    {
                        // We use the internal method to avoid refreshing the stat value.
                        // We will refresh the stat value only once after all the modifiers have been removed.
                        RemoveModifier_Internal(stat, modifier.Value);
                    }
                }
            }
        }       

        public override void RefreshStats()
        {
            RefreshStatsValueCache();
        }

        // Same as RemoveModifier but without refreshing the stat value.
        private bool RemoveModifier_Internal(T stat, IStatModifier modifier)
        {
            if (stat == null || modifier == null || !m_ModifiersPerStat.ContainsKey(stat))
            {
                return false;
            }

            if (m_ModifiersPerStat[stat].Remove(modifier.ExecutionOrder))
            {
                m_InvalidatedStats.Add(stat);
                return true;
            }

            return false;
        }
        #endregion

        #region Modifier Application
        /// <summary>
        /// Applies the modifiers on the stat and returns the new value.
        /// </summary>
        /// <param name="stat">The stat to apply the modifiers on.</param>
        /// <returns>The new value of the stat.</returns>
        private float ApplyModifiersOnStat(T stat)
        {
            float baseValue = stat.DefaultValue;
            float currentValue = baseValue;

            // Apply stat modifiers
            if (!TryGetStatModifiers(stat, out var statModifiers))
            {
                return currentValue;
            }

            foreach (var modifier in statModifiers)
            {
                currentValue = modifier.ApplyModifier(baseValue, currentValue);
            }

            // Apply stat-specific constraints
            currentValue = Mathf.Clamp(currentValue, stat.MinValue, stat.MaxValue);

            return currentValue;
        }
        #endregion

        #region Caching Logic
        private void RefreshStatsValueCache()
        {
            if (m_InvalidatedStats.Count == 0)
            {
                return;
            }

            var statsToRefresh = new List<T>(m_InvalidatedStats);
            foreach (var stat in statsToRefresh)
            {
                RefreshStatValueCache(stat);
            }
        }

        private void RefreshStatValueCache(T stat)
        {
            if (!m_LatestStatsValue.ContainsKey(stat))
            {
                return;
            }

            if (RefreshStatModifiersCache(stat))
            {
                m_LatestStatsValue[stat] = ApplyModifiersOnStat(stat);
            }
        }

        private bool RefreshStatModifiersCache(T stat)
        {
            // If the stat is not available in the cache or is not dirty, early out.
            if (stat == null || !m_LatestStatsValue.ContainsKey(stat) || !m_InvalidatedStats.Contains(stat))
            {
                return false;
            }

            // Generate a list of stat modifiers sorted by execution order.
            m_StatModifierWorkingSet.Clear();
            m_StatModifierWorkingSet.Add(m_CacheExecutionOrder, m_ModifiersPerStat[stat].Values);

            foreach (var dependency in m_DependencyList)
            {
                if (m_StatModifierWorkingSet.ContainsKey(dependency.Key))
                {
                    Debug.LogWarning($"[DataDrivenStatCache] Dependency {dependency.Value.GetType()} execution order {dependency.Key} is already used.");
                    continue;
                }

                // Get the compatible stats from the cache stat and retrieve them from the dependencies.
                if (m_CompatibleStatDefsPerStat.TryGetValue(stat, out var compatibleStats))
                {
                    // Combine all modifiers from all compatible stats for this dependency
                    m_InvalidatedModifiers.Clear();
                    foreach (var compatibleStat in compatibleStats)
                    {
                        if (dependency.Value.DependencyProcessor.TryGetStatModifiers(compatibleStat, out var statModifiers))
                        {
                            m_InvalidatedModifiers.AddRange(statModifiers);
                        }
                    }

                    Debug.Log($"Adding new stat modifier at {dependency.Key} priority for stat '{stat.Name}'.");
                    m_StatModifierWorkingSet.Add(dependency.Key, m_InvalidatedModifiers);
                }
            }

            // Flatten the list of stat modifiers.
            m_LatestFlattenedModifiersPerStat[stat].Clear();
            foreach (var statModifier in m_StatModifierWorkingSet.Values)
            {
                m_LatestFlattenedModifiersPerStat[stat].AddRange(statModifier);
            }

            // This is (and should be) the only place where we remove a stat from the dirty stats list.
            m_InvalidatedStats.Remove(stat);

            return true;
        }
        #endregion

        #region Dependency Management
        protected override void OnDependencyStatChanged(IStatDefinition stat)
        {
            if (stat is T typedStat)
            {
                m_InvalidatedStats.Add(typedStat);
                OnStatInvalidated?.Invoke(typedStat);
            }
            else
            {
                foreach (var compatibleStat in m_CompatibleStatDefsPerStat)
                {
                    foreach (var statsToCheck in compatibleStat.Value)
                    {
                        if (statsToCheck == stat)
                        {
                            m_InvalidatedStats.Add(compatibleStat.Key);
                            OnStatInvalidated?.Invoke(compatibleStat.Key);
                            break; // We stop at the first found stat as the key is now dirty
                        }

                        // But we still need to check the other stat with compatibility in case several
                        // T stats depends on the same IStatDefinition.
                    }
                }
            }
        }
        
        private void AddDependencyCompatibleStats(T stat, IReadOnlyList<IStatDefinition> compatibleStats)
        {
            if (stat == null || compatibleStats == null)
            {
                return;
            }

            if (!m_CompatibleStatDefsPerStat.ContainsKey(stat))
            {
                m_CompatibleStatDefsPerStat[stat] = new HashSet<IStatDefinition>();
            }

            foreach (var compatibleStat in compatibleStats)
            {
                m_CompatibleStatDefsPerStat[stat].Add(compatibleStat);
            }

            m_InvalidatedStats.Add(stat);
        }

        private void RemoveDependencyCompatibleStats(T stat, IReadOnlyList<IStatDefinition> compatibleStats)
        {
            if (stat == null || compatibleStats == null)
            {
                return;
            }

            if (m_CompatibleStatDefsPerStat.TryGetValue(stat, out var targetCompatibleStats))
            {
                foreach (var compatibleStat in compatibleStats)
                {
                    targetCompatibleStats.Remove(compatibleStat);
                }
            }

            m_InvalidatedStats.Add(stat);
        }

        private void InjectDependencies(IReadOnlyList<KeyValuePair<int, Dependency>> dependencies)
        {
            if (dependencies != null)
            {
                foreach (var dependency in dependencies)
                {
                    if (m_DependencyList.ContainsKey(dependency.Key))
                    {
                        Debug.LogWarning($"Dependency {dependency.Value.GetType()} execution order {dependency.Key} is already used.");
                        continue;
                    }

                    if (dependency.Value == null)
                    {
                        Debug.LogError($"Dependency at execution order {dependency.Key} is null.");
                        continue;
                    }

                    m_DependencyList.Add(dependency.Key, dependency.Value);

                    if (dependency.Value.StatDependencies != null)
                    {
                        foreach (var compatibleStat in dependency.Value.StatDependencies)
                        {
                            AddDependencyCompatibleStats(compatibleStat.Key, compatibleStat.Value);
                        }
                    }

                    dependency.Value.DependencyProcessor.OnStatInvalidated += OnDependencyStatChanged;
                }
            }
        }

        /// <summary>
        /// A dependency is a StatModule that is used to compute the value of the stat.
        /// You need to use a predicate to retrieve the compatible stats from the dependency.
        /// </summary>
        public class Dependency
        {
            public delegate IReadOnlyDictionary<T, IReadOnlyList<IStatDefinition>> StatMappingProvider(IEnumerable<IStatDefinition> stats);
            public IReadOnlyDictionary<T, IReadOnlyList<IStatDefinition>> StatDependencies { get; private set; }
            public StatManager DependencyProcessor { get; private set;  }

            public Dependency(StatManager dependency, StatMappingProvider statMapping)
            {
                DependencyProcessor = dependency;
                StatDependencies = statMapping?.Invoke(dependency.GetAvailableStats());
            }
        }
        #endregion
    }
}
