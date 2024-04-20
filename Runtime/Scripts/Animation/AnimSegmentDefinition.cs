using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    public class AnimSegmentDefinition : DataDefinition
    {
        [InfoBox("Set a segment that needs to be called before in order to raise the AnimSequenceController Event.")]
        [SerializeField] private AnimSegmentDefinition m_requiresPriorSegment;

        public AnimSegmentDefinition ExpectedPriorSegment => m_requiresPriorSegment;
    }
}