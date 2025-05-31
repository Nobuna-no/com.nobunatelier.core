using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    public class AnimSegmentDefinition : DataDefinition
    {
        [InfoBox("Set a segment that needs to be called before in order to raise the AnimSequenceController Event.")]
        [SerializeField, FormerlySerializedAs("m_requiresPriorSegment")]
        private AnimSegmentDefinition m_RequiresPriorSegment;

        public AnimSegmentDefinition ExpectedPriorSegment => m_RequiresPriorSegment;
    }
}