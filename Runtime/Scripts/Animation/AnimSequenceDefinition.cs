using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    public class AnimSequenceDefinition : DataDefinition
    {
        [Header("AnimSequence")]
        [InfoBox("This name should correspond to the AnimatorState of the AnimatorController holding the animation clip.")]
        public string stateName;

        [AllowNesting, NaughtyAttributes.ReadOnly]
        public int stateNameHash;

        // public AnimationClip clip;
        public float crossFadeDuration = .1f;

        public Segment[] segments;

        [System.Serializable]
        public class Segment
        {
            public enum SegmentModifier
            {
                None,
                ForceAnimatorSpeed,
                ResetAnimatorSpeed,
                ForceDuration
            }

            public float Duration => m_duration;

            [FormerlySerializedAs("duration")]
            [SerializeField, AllowNesting, ReadOnly]
            private float m_duration;

            public AnimSegmentDefinition segmentDefinition;
            public SegmentModifier segmentModifier;

            [SerializeField, AllowNesting, ShowIf("ShowForceDuration")]
            public float segmentNewDuration;

            [SerializeField, AllowNesting, ShowIf("ShowForceAnimatorSpeed")]
            public float segmentAnimatorSpeed;

            public bool ShowForceDuration => segmentModifier == SegmentModifier.ForceDuration;
            public bool ShowForceAnimatorSpeed => segmentModifier == SegmentModifier.ForceAnimatorSpeed;

            public float GetEstimatedDuration()
            {
                switch (segmentModifier)
                {
                    case AnimSequenceDefinition.Segment.SegmentModifier.None:
                    case AnimSequenceDefinition.Segment.SegmentModifier.ResetAnimatorSpeed:
                        return m_duration;

                    case AnimSequenceDefinition.Segment.SegmentModifier.ForceDuration:
                        return segmentNewDuration;

                    case AnimSequenceDefinition.Segment.SegmentModifier.ForceAnimatorSpeed:
                        return m_duration * segmentAnimatorSpeed;
                }

                return m_duration;
            }
        }

        [Button()]
        private void RefreshStateNameHash()
        {
            stateNameHash = Animator.StringToHash(stateName);
        }
    }
}