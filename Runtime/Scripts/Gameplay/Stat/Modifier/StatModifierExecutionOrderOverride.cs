using UnityEngine;

#if UNITY_EDITOR

using System.Linq;

#endif

namespace NobunAtelier.Gameplay
{
    public class StatModifierExecutionOrderOverride : MonoBehaviourService<StatModifierExecutionOrderOverride>
    {
        [Tooltip("The order of the modifiers. The first modifier will be executed first, the last modifier will be executed last.")]
        [SerializeField] private StatModifierDefinition[] m_ModifierExecutionOrder;

        private int m_ExecutionOrderSpacing = 100;

        public int GetExecutionOrder(StatModifierDefinition modifier)
        {
            if (m_ModifierExecutionOrder == null) return modifier.DefaultExecutionOrder;

            for (int i = 0; i < m_ModifierExecutionOrder.Length; i++)
            {
                if (m_ModifierExecutionOrder[i] == modifier)
                    return i * m_ExecutionOrderSpacing; // Spacing between orders
            }

            return modifier.DefaultExecutionOrder; // Fallback
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            if (m_ModifierExecutionOrder != null)
            {
                var duplicates = m_ModifierExecutionOrder
                    .GroupBy(x => x)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                foreach (var duplicate in duplicates)
                {
                    Debug.LogWarning($"Duplicate modifier in execution order: {duplicate.name}");
                }
            }
        }

#endif
    }
}
