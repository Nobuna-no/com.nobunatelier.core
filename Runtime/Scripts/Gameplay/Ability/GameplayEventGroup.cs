using System;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Maps a <see cref="GameplayEventDefinition"/> to one or more <see cref="EffectEntry"/>s.
    /// Groups all effects that fire on the same content event.
    /// </summary>
    [Serializable]
    public class GameplayEventGroup
    {
        [Tooltip("The gameplay event that triggers all effects in this group.")]
        [SerializeField] private GameplayEventDefinition m_Event;

        [Tooltip("Effects to execute when this event fires.")]
        [SerializeField] private List<EffectEntry> m_Effects;

        public GameplayEventDefinition Event => m_Event;
        public IReadOnlyList<EffectEntry> Effects => m_Effects;
    }
}
