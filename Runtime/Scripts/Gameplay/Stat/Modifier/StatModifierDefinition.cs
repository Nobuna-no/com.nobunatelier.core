using UnityEngine;

namespace NobunAtelier.Gameplay
{
    public abstract class StatModifierDefinition : DataDefinition
    {
#if UNITY_EDITOR

        [Header("Developper (EDITOR ONLY)")]
        [SerializeField] private string m_Description;

#endif

        [Header("Modifier")]
        [Tooltip("The order in which the modifier is applied.")]
        [SerializeField] private int m_ExecutionOrder;

        public int ExecutionOrder
        {
            get
            {
                return StatModifierExecutionOrderOverride.IsSingletonValid
                    ? StatModifierExecutionOrderOverride.Instance.GetExecutionOrder(this)
                    : m_ExecutionOrder; // Fallback
            }
        }

        public int DefaultExecutionOrder => m_ExecutionOrder;

        public abstract string DisplayName { get; }

        public abstract IStatModifier CreateRuntimeModifier(float value, object source = null);
    }
}
