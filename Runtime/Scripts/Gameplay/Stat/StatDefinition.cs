using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace NobunAtelier.Gameplay
{
    [MovedFrom(false, sourceNamespace: "Nemesis", sourceClassName: "StatDefinition")]
    public abstract class StatDefinition : DataDefinition, IStatDefinition
    {
#if UNITY_EDITOR

        [Header("Developper (EDITOR ONLY)")]
        [SerializeField, TextArea] private string m_Note;

#endif

        [Header("UI")]
        [SerializeField] private string m_DisplayName;

        [SerializeField] private string m_Description;
        [SerializeField] private Sprite m_Icon;

        [Header("Settings")]
        [SerializeField] private float m_DefaultValue = 0f;

        [SerializeField] private float m_MinValue = 0f;
        [SerializeField] private float m_MaxValue = 0f;

        public string DisplayName => m_DisplayName;
        public string Description => m_Description;
        public Sprite Icon => m_Icon;
        
        public string Name => string.IsNullOrEmpty(m_DisplayName) ? name : m_DisplayName;
        public float DefaultValue => m_DefaultValue;
        public float MinValue => m_MinValue;
        public float MaxValue => m_MaxValue;

        public bool IsSameStatAs(IStatDefinition other)
        {
            return other is StatDefinition stat && GetInstanceID() == stat.GetInstanceID();
        }
    }

    [System.Serializable]
    public enum AffixApplicationType
    {
        [Tooltip("current + value (standard addition)")]
        Additive,

        [Tooltip("current * (1 + value) (percentage increase from current)")]
        Multiplicative,

        [Tooltip("(1 + current) + value - 1 (for values that start at 1.0)")]
        AdditiveFromOne,

        [Tooltip("max(current + value, 0) (prevents negative values)")]
        AdditiveNonNegative,

        [Tooltip("value (replaces current value entirely)")]
        Override,

        [Tooltip("current * (value / 100) (percentage-based scaling)")]
        Percentage,

        [Tooltip("current * value (direct multiplication)")]
        Scale
    }
}
