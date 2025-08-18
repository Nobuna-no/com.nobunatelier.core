# Stat System Documentation

## Table of Contents

### 1. Overview

### 2. Core Concepts
- **2.1** Stat Definitions (`IStatDefinition`)
- **2.2** Stat Modifiers (`IStatModifier`) 
- **2.3** Stat Managers (`StatManager<T>`)
- **2.4** Stat Handlers (`IStatHandler`)

### 3. Basic Usage
- **3.1** Creating Your First Stat Definition
- **3.2** Setting Up a Stat Manager
- **3.3** Adding and Removing Modifiers
- **3.4** Reading Stat Values
- **3.5** Listening to Stat Changes

### 4. Stat Modifiers Deep Dive
- **4.1** Types of Modifiers
- **4.2** Execution Order and Priority
- **4.3** Modifier Sources and Cleanup
- **4.4** Creating Custom Modifiers

### 5. Dependencies Between Stat Systems
- **5.1** Why Use Dependencies?
- **5.2** Setting Up Stat Dependencies
- **5.3** Execution Order and Hierarchies
- **5.4** Compatible Stats and Stat Mapping
- **5.5** Dependency Best Practices

### 6. Practical Examples
- **6.1** Basic Character Stats (Health, Mana, Damage)
- **6.2** Equipment System Integration
- **6.3** Temporary Buffs and Debuffs
- **6.4** Complex Multi-System Setup (Character + Weapon + Equipment)

### 7. Performance and Optimization
- **7.1** Lazy Evaluation and Caching
- **7.2** When Stats are Recalculated
- **7.3** Performance Tips
- **7.4** Memory Considerations

### 8. Debugging and Troubleshooting
- **8.1** Common Issues and Solutions
- **8.2** Debugging Stat Calculations
- **8.3** Tracing Modifier Application
- **8.4** Dependency Chain Debugging

### 9. API Reference
- **9.1** `IStatDefinition` Interface
- **9.2** `IStatModifier` Interface  
- **9.3** `StatManager<T>` Class
- **9.4** `IStatHandler` Interface
- **9.5** Extension Methods

### 10. Advanced Topics
- **10.1** Custom Stat Definition Types
- **10.2** Integrating with Unity Inspector
- **10.3** Serialization Considerations
- **10.4** Thread Safety Notes

---

## 1. Overview

The Stat System is a flexible, performance-optimized framework for managing game statistics (health, damage, speed, etc.) with support for modifiers, dependencies between different stat systems, and automatic caching. It provides a unified way to handle all stat-related calculations in your game.

**Core Philosophy:**
- **Data-Driven**: Stats can be defined as ScriptableObjects, making them easy to balance and modify
- **Lazy Evaluation**: Values are only computed when requested, with intelligent caching
- **Dependency-Aware**: Different stat systems can depend on each other (e.g., weapon stats can use character stats)
- **Modifier-Based**: All stat changes happen through modifiers, making the system predictable and debuggable

> Please note that in this document Health refer to the "Health" stat (aka MaxHealth) and not the current Health that a particular unit have at a given frame. This should be handled by a separated system.

---

## 2. Core Concepts

### 2.1 Stat Definitions (`IStatDefinition`)

Stat Definitions describe what a stat is and its constraints. They define the "blueprint" for stats like MaxHealth, Attack, or Speed.

```csharp
public interface IStatDefinition
{
    string Name { get; }
    float DefaultValue { get; }
    float MinValue { get; }
    float MaxValue { get; }
    bool IsSameStatAs(IStatDefinition other);
}
```

**Key Properties:**
- **Name**: Human-readable identifier for the stat
- **DefaultValue**: Base value before any modifiers
- **MinValue/MaxValue**: Constraints that final computed values must respect
- **IsSameStatAs()**: Identity comparison for stat matching across systems

### 2.2 Stat Modifiers (`IStatModifier`)

Modifiers change stat values. They represent temporary or permanent changes like equipment bonuses, spell effects, or character progression.

```csharp
public interface IStatModifier
{
    float Value { get; }
    object Source { get; }
    int ExecutionOrder { get; }
    float ApplyModifier(float baseValue, float currentValue);
}
```

**Key Properties:**
- **Value**: The modifier's magnitude (e.g., +10 damage, +0.5 for 50% increase)
- **Source**: What created this modifier (for debugging and cleanup)
- **ExecutionOrder**: When this modifier applies relative to others
- **ApplyModifier()**: The actual calculation method

### 2.3 Stat Managers (`StatManager<T>`)

StatManagers orchestrate everything. They store stat values, manage modifiers, handle dependencies, and provide the main API for stat operations.

```csharp
public class StatManager<T> : StatManager where T : class, IStatDefinition
{
    public bool TryGetStatValue(T stat, out float value);
    public bool AddModifier(T stat, IStatModifier modifier);
    public bool RemoveModifier(T stat, IStatModifier modifier);
    public event Action<IStatDefinition> OnStatInvalidated;
}
```

**Responsibilities:**
- Store and cache computed stat values
- Manage modifier addition/removal
- Handle dependencies with other StatManagers
- Notify listeners when stats change
- Ensure thread-safe access to cached values

### 2.4 Stat Handlers (`IStatHandler`)

StatHandlers provide a unified interface for systems that own StatManagers. They simplify access and provide extension methods for common operations.

```csharp
public interface IStatHandler
{
    StatManager StatModule { get; }
    event OnStatChangedDelegate OnStatChanged;
}
```

---

## 3. Basic Usage

### 3.1 Creating Your First Stat Definition

Create stat definitions as ScriptableObjects by inheriting from `StatDefinition`:

```csharp
[CreateAssetMenu(fileName = "HealthStat", menuName = "Game/Stats/Health")]
public class HealthStat : StatDefinition
{
    // StatDefinition provides all the IStatDefinition implementation
    // You can add custom properties here if needed
}
```

In the Unity Editor:
1. Right-click in Project window
2. Create → Game → Stats → Health
3. Configure DefaultValue, MinValue, MaxValue in Inspector

### 3.2 Setting Up a Stat Manager

```csharp
public class CharacterStats : MonoBehaviour, IStatHandler
{
    [SerializeField] private HealthStat m_healthStat;
    [SerializeField] private DamageStat m_damageStat;
    
    private StatManager<StatDefinition> m_statManager;
    
    public StatManager StatModule => m_statManager;
    public event IStatHandler.OnStatChangedDelegate OnStatChanged;
    
    void Awake()
    {
        var availableStats = new[] { m_healthStat, m_damageStat };
        m_statManager = new StatManager<StatDefinition>(availableStats);
        
        // Forward events from StatManager to IStatHandler
        m_statManager.OnStatInvalidated += (stat) => OnStatChanged?.Invoke(stat);
    }
}
```

### 3.3 Adding and Removing Modifiers

```csharp
// Create a modifier (usually from a ScriptableObject definition)
var damageBonus = new GenericStatModifier(
    value: 10f,
    source: this, // for cleanup later
    executionOrder: 100,
    mode: GenericStatModifier.ApplicationMode.Flat
);

// Add the modifier
m_statManager.AddModifier(m_damageStat, damageBonus);

// Remove specific modifier
m_statManager.RemoveModifier(m_damageStat, damageBonus);

// Remove all modifiers from a source
m_statManager.ClearModifiers(this);
```

### 3.4 Reading Stat Values

```csharp
// Get the final computed value
if (m_statManager.TryGetStatValue(m_healthStat, out float currentHealth))
{
    Debug.Log($"Current health: {currentHealth}");
}

// Using the extension method (more convenient)
float damage = this.ComputeStat(m_damageStat, m_damageStat.DefaultValue);
```

### 3.5 Listening to Stat Changes

```csharp
void Start()
{
    // Listen to stat invalidation events
    m_statManager.OnStatInvalidated += OnStatChanged;
}

private void OnStatChanged(IStatDefinition stat)
{
    Debug.Log($"Stat {stat.Name} was invalidated, will recompute on next access");
    
    // Optionally fetch new value immediately
    if (m_statManager.TryGetStatValue(stat as StatDefinition, out float newValue))
    {
        Debug.Log($"New value: {newValue}");
    }
}
```

---

## 4. Stat Modifiers Deep Dive

### 4.1 Types of Modifiers

The system provides several built-in modifier types through `GenericStatModifier.ApplicationMode`:

#### Override (Order: 0)
Replaces the current value entirely.
```csharp
// Set health to exactly 50, ignoring all other modifiers
new GenericStatModifier(50f, source, 0, ApplicationMode.Override)
```

#### Flat/Additive (Order: 100)
Adds a fixed amount to the current value.
```csharp
// Add 10 damage
new GenericStatModifier(10f, source, 100, ApplicationMode.Flat)
// Result: currentValue + 10
```

#### Multiplicative (Order: 200)
Increases by a percentage of the current value.
```csharp
// Increase by 50%
new GenericStatModifier(0.5f, source, 200, ApplicationMode.Multiplicative)
// Result: currentValue * (1 + 0.5) = currentValue * 1.5
```

#### Percentage (Order: 300)
Direct percentage-based scaling.
```csharp
// Scale to 75% of current value
new GenericStatModifier(75f, source, 300, ApplicationMode.Percentage)
// Result: currentValue * (75 / 100) = currentValue * 0.75
```

#### Scale (Order: 300)
Direct multiplication.
```csharp
// Double the current value
new GenericStatModifier(2f, source, 300, ApplicationMode.Scale)
// Result: currentValue * 2
```

### 4.2 Execution Order and Priority

Modifiers are applied in execution order (lowest to highest). The recommended pattern is:

```
0-99:   Override modifiers (base value replacement)
100-199: Flat modifiers (additive bonuses)
200-299: Multiplicative modifiers (percentage increases)
300-399: Scaling modifiers (final adjustments)
```

If multiple modifiers have the same execution order, the system automatically increments the order to avoid conflicts.

### 4.3 Modifier Sources and Cleanup

Always specify a source when creating modifiers:

```csharp
public class Equipment : MonoBehaviour
{
    void OnEquip()
    {
        var modifier = new GenericStatModifier(5f, this, 100, ApplicationMode.Flat);
        characterStats.StatModule.AddModifier(damageStat, modifier);
    }
    
    void OnUnequip()
    {
        // Remove all modifiers from this equipment piece
        characterStats.StatModule.ClearModifiers(this);
    }
}
```

### 4.4 Creating Custom Modifiers

For complex behaviors, implement `IStatModifier` directly:

```csharp
public class DamageBasedOnHealthModifier : IStatModifier
{
    public float Value { get; private set; }
    public object Source { get; private set; }
    public int ExecutionOrder { get; private set; }
    
    private readonly IStatHandler m_characterStats;
    private readonly IStatDefinition m_healthStat;
    
    public DamageBasedOnHealthModifier(IStatHandler characterStats, IStatDefinition healthStat, object source)
    {
        m_characterStats = characterStats;
        m_healthStat = healthStat;
        Source = source;
        ExecutionOrder = 250; // After flat, before final scaling
    }
    
    public float ApplyModifier(float baseValue, float currentValue)
    {
        // Damage increases as health decreases
        if (m_characterStats.StatModule.TryGetStatValue(m_healthStat, out float currentHealth))
        {
            float healthPercent = currentHealth / m_healthStat.DefaultValue;
            float damageBonus = (1f - healthPercent) * 20f; // Up to +20 damage at 0% health
            return currentValue + damageBonus;
        }
        return currentValue;
    }
}
```

---

## 5. Dependencies Between Stat Systems

### 5.1 Why Use Dependencies?

Dependencies allow one stat system to use modifiers from another. Common scenarios:

- **Weapon damage** based on **Character strength**
- **Movement speed** affected by **Equipment weight**
- **Spell power** scaling with **Character intelligence**
- **Final damage** combining **Base weapon damage** + **Character bonuses** + **Temporary buffs**

### 5.2 Setting Up Stat Dependencies

Dependencies are configured during StatManager construction:

```csharp
public class WeaponStats : MonoBehaviour, IStatHandler
{
    private StatManager<WeaponStatDefinition> m_statManager;
    
    void Initialize(CharacterStats characterStats, EquipmentStats equipmentStats)
    {
        var weaponStats = new[] { firerateStat, damageStat };
        
        var dependencies = new[]
        {
            new KeyValuePair<int, StatManager<WeaponStatDefinition>.Dependency>(
                100, // Character stats execute first
                new StatManager<WeaponStatDefinition>.Dependency(
                    characterStats.StatModule,
                    CreateCharacterDependencyMapping
                )
            ),
            new KeyValuePair<int, StatManager<WeaponStatDefinition>.Dependency>(
                200, // Equipment stats execute second
                new StatManager<WeaponStatDefinition>.Dependency(
                    equipmentStats.StatModule,
                    CreateEquipmentDependencyMapping
                )
            )
        };
        
        m_statManager = new StatManager<WeaponStatDefinition>(
            weaponStats,
            dependencies,
            cacheModifierExecutionOrder: 300 // Weapon's own modifiers execute last
        );
    }
}
```

### 5.3 Execution Order and Hierarchies

Dependencies create a hierarchy where lower execution orders execute first:

```
Character Stats (100) → Equipment Stats (200) → Weapon Stats (300)
```

Within each level, modifiers are processed by their individual execution orders.

### 5.4 Compatible Stats and Stat Mapping

The dependency mapping function defines which stats from dependencies affect which local stats:

```csharp
private IReadOnlyDictionary<WeaponStatDefinition, IReadOnlyList<IStatDefinition>> 
    CreateCharacterDependencyMapping(IEnumerable<IStatDefinition> characterStats)
{
    var mapping = new Dictionary<WeaponStatDefinition, IReadOnlyList<IStatDefinition>>();
    
    // Weapon damage can use character strength modifiers
    var strengthStat = characterStats.FirstOrDefault(s => s.Name == "Strength");
    if (strengthStat != null)
    {
        mapping[weaponDamageStat] = new[] { strengthStat };
    }
    
    // Weapon firerate can use character dexterity modifiers
    var dexterityStat = characterStats.FirstOrDefault(s => s.Name == "Dexterity");
    if (dexterityStat != null)
    {
        mapping[weaponFirerateStat] = new[] { dexterityStat };
    }
    
    return mapping;
}
```

### 5.5 Dependency Best Practices

**✅ DO:**
- Use execution orders in increments of 100 (100, 200, 300) to leave room for insertion
- Create clear hierarchies: Base Stats → Equipment → Temporary Effects
- Document your dependency chains in code comments
- Test circular dependency scenarios

**❌ DON'T:**
- Create circular dependencies (A depends on B, B depends on A)
- Use the same execution order for multiple dependencies
- Make deep dependency chains (more than 3-4 levels)
- Change dependency structure at runtime

---

## 6. Practical Examples

### 6.1 Basic Character Stats (Health, Mana, Damage)

```csharp
[System.Serializable]
public class CharacterStatsComponent : MonoBehaviour, IStatHandler
{
    [Header("Stat Definitions")]
    [SerializeField] private HealthStatDefinition m_healthStat;
    [SerializeField] private ManaStatDefinition m_manaStat;
    [SerializeField] private DamageStatDefinition m_damageStat;
    
    private StatManager<CharacterStatDefinition> m_statManager;
    
    public StatManager StatModule => m_statManager;
    public event IStatHandler.OnStatChangedDelegate OnStatChanged;
    
    // Convenient properties for common access
    public float CurrentHealth => this.ComputeStat(m_healthStat, m_healthStat.DefaultValue);
    public float CurrentMana => this.ComputeStat(m_manaStat, m_manaStat.DefaultValue);
    public float CurrentDamage => this.ComputeStat(m_damageStat, m_damageStat.DefaultValue);
    
    void Awake()
    {
        InitializeStatManager();
    }
    
    private void InitializeStatManager()
    {
        var stats = new CharacterStatDefinition[] { m_healthStat, m_manaStat, m_damageStat };
        m_statManager = new StatManager<CharacterStatDefinition>(stats);
        m_statManager.OnStatInvalidated += stat => OnStatChanged?.Invoke(stat);
    }
    
    // Public API for character progression
    public void ApplyLevelUpBonus(int level)
    {
        var healthBonus = new GenericStatModifier(level * 10f, this, 100, GenericStatModifier.ApplicationMode.Flat);
        var manaBonus = new GenericStatModifier(level * 5f, this, 100, GenericStatModifier.ApplicationMode.Flat);
        
        m_statManager.AddModifier(m_healthStat, healthBonus);
        m_statManager.AddModifier(m_manaStat, manaBonus);
    }
}
```

### 6.2 Equipment System Integration

```csharp
public class EquipmentPiece : MonoBehaviour
{
    [Header("Stat Modifications")]
    [SerializeField] private StatModifierData[] m_statModifiers;
    
    [System.Serializable]
    public class StatModifierData
    {
        public StatDefinition targetStat;
        public GenericStatModifierDefinition modifierDefinition;
        public float value;
    }
    
    private List<IStatModifier> m_activeModifiers = new();
    
    public void OnEquip(IStatHandler target)
    {
        foreach (var modData in m_statModifiers)
        {
            var modifier = modData.modifierDefinition.CreateRuntimeModifier(modData.value, this);
            target.StatModule.AddModifier(modData.targetStat, modifier);
            m_activeModifiers.Add(modifier);
        }
    }
    
    public void OnUnequip(IStatHandler target)
    {
        // Clean up all modifiers from this equipment
        target.StatModule.ClearModifiers(this);
        m_activeModifiers.Clear();
    }
}
```

### 6.3 Temporary Buffs and Debuffs

```csharp
public class TemporaryEffect : MonoBehaviour
{
    [Header("Effect Configuration")]
    [SerializeField] private float m_duration = 10f;
    [SerializeField] private StatModifierData[] m_modifiers;
    
    private IStatHandler m_target;
    private float m_startTime;
    
    public void ApplyTo(IStatHandler target)
    {
        m_target = target;
        m_startTime = Time.time;
        
        // Apply all modifiers
        foreach (var modData in m_modifiers)
        {
            var modifier = modData.modifierDefinition.CreateRuntimeModifier(modData.value, this);
            target.StatModule.AddModifier(modData.targetStat, modifier);
        }
        
        // Schedule cleanup
        StartCoroutine(CleanupAfterDuration());
    }
    
    private IEnumerator CleanupAfterDuration()
    {
        yield return new WaitForSeconds(m_duration);
        
        // Remove all modifiers from this effect
        m_target?.StatModule.ClearModifiers(this);
        
        Destroy(gameObject);
    }
    
    // Public API for early removal (e.g., dispel effects)
    public void RemoveEffect()
    {
        m_target?.StatModule.ClearModifiers(this);
        StopAllCoroutines();
        Destroy(gameObject);
    }
}
```

### 6.4 Complex Multi-System Setup (Character + Weapon + Equipment)

```csharp
public class CombatSystem : MonoBehaviour
{
    [Header("Stat Systems")]
    [SerializeField] private CharacterStatsComponent m_characterStats;
    [SerializeField] private WeaponStats m_weaponStats;
    [SerializeField] private EquipmentStats m_equipmentStats;
    
    void Start()
    {
        InitializeDependencyChain();
    }
    
    private void InitializeDependencyChain()
    {
        // Character Stats (Base layer - no dependencies)
        // Already initialized in CharacterStatsComponent
        
        // Equipment Stats (Depends on Character)
        var equipmentDependencies = new[]
        {
            new KeyValuePair<int, StatManager<EquipmentStatDefinition>.Dependency>(
                100,
                new StatManager<EquipmentStatDefinition>.Dependency(
                    m_characterStats.StatModule,
                    CreateEquipmentToCharacterMapping
                )
            )
        };
        
        m_equipmentStats.Initialize(equipmentDependencies, 200);
        
        // Weapon Stats (Depends on Character + Equipment)
        var weaponDependencies = new[]
        {
            new KeyValuePair<int, StatManager<WeaponStatDefinition>.Dependency>(
                100,
                new StatManager<WeaponStatDefinition>.Dependency(
                    m_characterStats.StatModule,
                    CreateWeaponToCharacterMapping
                )
            ),
            new KeyValuePair<int, StatManager<WeaponStatDefinition>.Dependency>(
                200,
                new StatManager<WeaponStatDefinition>.Dependency(
                    m_equipmentStats.StatModule,
                    CreateWeaponToEquipmentMapping
                )
            )
        };
        
        m_weaponStats.Initialize(weaponDependencies, 300);
    }
    
    // Calculate final damage for combat
    public float GetFinalDamageOutput()
    {
        float baseDamage = m_characterStats.CurrentDamage;
        float weaponDamage = m_weaponStats.GetWeaponDamage();
        float equipmentBonus = m_equipmentStats.GetDamageBonus();
        
        // All modifiers are already applied through the dependency system
        return baseDamage + weaponDamage + equipmentBonus;
    }
}
```

---

## 7. Performance and Optimization

### 7.1 Lazy Evaluation and Caching

The StatManager uses lazy evaluation to optimize performance:

- **Stat values are only computed when requested** via `TryGetStatValue()`
- **Values are cached** until modifiers change
- **Dirty state tracking** ensures minimal recomputation
- **Dependency invalidation** propagates changes efficiently

```csharp
// This is fast - returns cached value if available
float health = characterStats.GetStatValue(healthStat);

// This triggers recomputation only if modifiers changed
characterStats.AddModifier(healthStat, healthBonus);
float newHealth = characterStats.GetStatValue(healthStat); // Recomputed
float sameHealth = characterStats.GetStatValue(healthStat); // Cached
```

### 7.2 When Stats are Recalculated

Stats are marked as dirty and recalculated when:

1. **Modifiers are added or removed** from the local StatManager
2. **Dependency stats change** and affect compatible stats
3. **First access after being marked dirty**

Stats are NOT recalculated when:
- The same value is accessed multiple times without changes
- Unrelated stats change
- Modifiers are added to incompatible stats

### 7.3 Performance Tips

**✅ DO:**
- Cache frequently accessed stat values in local variables
- Use `OnStatInvalidated` events to update UI only when needed
- Batch modifier additions/removals when possible
- Keep dependency chains shallow (2-3 levels max)

**❌ AVOID:**
- Calling `TryGetStatValue()` every frame for the same stat
- Creating/destroying modifiers frequently (pool them instead)
- Deep dependency chains (each level adds complexity)
- Adding modifiers during tight loops

```csharp
// ✅ Good: Cache and update on change
float m_cachedHealth;

void Start()
{
    UpdateCachedHealth();
    characterStats.OnStatChanged += OnHealthChanged;
}

void OnHealthChanged(IStatDefinition stat)
{
    if (stat == healthStat)
        UpdateCachedHealth();
}

void UpdateCachedHealth()
{
    characterStats.TryGetStatValue(healthStat, out m_cachedHealth);
}

// ❌ Bad: Compute every frame
void Update()
{
    float health = characterStats.GetStatValue(healthStat); // Expensive!
    UpdateHealthBar(health);
}
```

### 7.4 Memory Considerations

The StatManager maintains several internal collections:

- **Computed Values Cache**: `Dictionary<T, float>` - One entry per stat
- **Local Modifiers**: `Dictionary<T, SortedList<int, IStatModifier>>` - Grows with modifiers
- **Cached Stat Modifiers**: `Dictionary<T, List<IStatModifier>>` - Includes dependency modifiers
- **Dirty State Tracking**: `HashSet<T>` - Small, temporary collection

**Memory Tips:**
- StatManagers have fixed overhead per stat (4 dictionary entries)
- Memory grows linearly with number of active modifiers
- Dependency relationships add minimal memory overhead
- Consider object pooling for frequently created/destroyed modifiers

---

## 8. Debugging and Troubleshooting

### 8.1 Common Issues and Solutions

*This section will be populated as debugging strategies are developed.*

### 8.2 Debugging Stat Calculations

*This section will be populated as debugging strategies are developed.*

### 8.3 Tracing Modifier Application

*This section will be populated as debugging strategies are developed.*

### 8.4 Dependency Chain Debugging

*This section will be populated as debugging strategies are developed.*

---

## 9. API Reference

### 9.1 `IStatDefinition` Interface

```csharp
public interface IStatDefinition
{
    string Name { get; }
    float DefaultValue { get; }
    float MinValue { get; }
    float MaxValue { get; }
    bool IsSameStatAs(IStatDefinition other);
}
```

**Properties:**
- `Name`: Display name for the stat
- `DefaultValue`: Base value before any modifiers
- `MinValue`: Minimum allowed value after modifier application
- `MaxValue`: Maximum allowed value after modifier application

**Methods:**
- `IsSameStatAs(other)`: Identity comparison for stat matching

### 9.2 `IStatModifier` Interface

```csharp
public interface IStatModifier
{
    float Value { get; }
    object Source { get; }
    int ExecutionOrder { get; }
    float ApplyModifier(float baseValue, float currentValue);
}
```

**Properties:**
- `Value`: The modifier's magnitude
- `Source`: Object that created this modifier (for cleanup)
- `ExecutionOrder`: Priority for modifier application

**Methods:**
- `ApplyModifier(baseValue, currentValue)`: Calculates modified value

### 9.3 `StatManager<T>` Class

```csharp
public class StatManager<T> : StatManager where T : class, IStatDefinition
{
    // Constructor
    public StatManager(
        IReadOnlyList<T> availableStats,
        IReadOnlyList<KeyValuePair<int, Dependency>> dependencies = null,
        int cacheModifierExecutionOrder = 0
    );
    
    // Core Methods
    public bool TryGetStatValue(T stat, out float value);
    public bool AddModifier(T stat, IStatModifier modifier);
    public bool RemoveModifier(T stat, IStatModifier modifier);
    public void ClearModifiers();
    public void ClearModifiers(T stat);
    public void ClearModifiers(object source);
    
    // Query Methods
    public bool TryGetStatModifiers(T stat, out IReadOnlyList<IStatModifier> statModifiers);
    public IEnumerable<IStatDefinition> GetAvailableStats();
    
    // Events
    public event Action<IStatDefinition> OnStatInvalidated;
}
```

### 9.4 `IStatHandler` Interface

```csharp
public interface IStatHandler
{
    delegate void OnStatChangedDelegate(IStatDefinition stat);
    
    event OnStatChangedDelegate OnStatChanged;
    StatManager StatModule { get; }
}
```

### 9.5 Extension Methods

```csharp
public static class StatHandlerExtensions
{
    public static float ComputeStat(this IStatHandler handler, IStatDefinition stat, float baseValue);
}
```

**Usage:**
```csharp
float finalDamage = characterStats.ComputeStat(damageStat, damageStat.DefaultValue);
```

---

## 10. Advanced Topics

### 10.1 Custom Stat Definition Types

You can create specialized stat definitions with additional properties:

```csharp
[CreateAssetMenu(fileName = "ResourceStat", menuName = "Game/Stats/Resource")]
public class ResourceStatDefinition : StatDefinition
{
    [Header("Resource Settings")]
    [SerializeField] private float m_regenerationRate = 1f;
    [SerializeField] private Color m_displayColor = Color.green;
    
    public float RegenerationRate => m_regenerationRate;
    public Color DisplayColor => m_displayColor;
}

// Use specialized StatManager
StatManager<ResourceStatDefinition> m_resourceStats;
```

### 10.2 Integrating with Unity Inspector

Create custom PropertyDrawers for better Inspector integration:

```csharp
[System.Serializable]
public class StatReference
{
    [SerializeField] private StatDefinition m_statDefinition;
    [SerializeField] private float m_debugCurrentValue;
    
    public StatDefinition Definition => m_statDefinition;
    
    public void UpdateDebugValue(IStatHandler handler)
    {
        if (handler.StatModule.TryGetStatValue(m_statDefinition, out float value))
        {
            m_debugCurrentValue = value;
        }
    }
}
```

### 10.3 Serialization Considerations

**What serializes:**
- StatDefinition references (as asset references)
- Modifier definitions (as ScriptableObjects)
- Configuration data (execution orders, dependencies)

**What doesn't serialize:**
- Runtime StatManager instances
- Active modifiers
- Cached computed values
- Event subscriptions

**Best Practices:**
- Store stat references, not computed values
- Recreate StatManagers in Awake/Start
- Use ScriptableObjects for modifier templates
- Implement proper cleanup in OnDestroy

### 10.4 Thread Safety Notes

**Thread-Safe Operations:**
- Reading cached stat values (if not currently being computed)
- Creating modifier instances

**Non-Thread-Safe Operations:**
- Adding/removing modifiers
- Computing stat values
- Dependency resolution
- Event firing

**Recommendations:**
- Only access StatManagers from the main thread
- Use Unity's job system for bulk stat computations if needed
- Consider copying values to job-safe structures for background processing 