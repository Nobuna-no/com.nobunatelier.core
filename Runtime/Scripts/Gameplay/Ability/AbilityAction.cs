using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Composition unit: an <see cref="IAbilityActionDriver"/> (timing source) +
    /// <see cref="EventBinding"/>s (effect subscriptions).
    /// Replaces V7's ActionModel as a standalone, reusable SO.
    /// </summary>
    [CreateAssetMenu(menuName = "NobunAtelier/Ability/Action")]
    public class AbilityAction : ScriptableObject
    {
        [Tooltip("Timing driver that fires GameplayEvents during execution.")]
        [SerializeReference]
        private IAbilityActionDriver m_Driver;

        [Tooltip("Event-to-effect bindings. Multiple bindings can map to the same event.")]
        [SerializeField]
        private List<EventBinding> m_Bindings;

        public IAbilityActionDriver Driver => m_Driver;
        public IReadOnlyList<EventBinding> Bindings => m_Bindings;

        /// <summary>
        /// Returns all GameplayEvents the driver can fire.
        /// Useful for editor validation of binding configuration.
        /// </summary>
        public GameplayEventDefinition[] GetAvailableEvents()
        {
            return m_Driver?.GetAvailableEvents();
        }
    }
}
