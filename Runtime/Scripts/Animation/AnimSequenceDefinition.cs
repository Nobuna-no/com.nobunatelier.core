using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    public class AnimSequenceDefinition : DataDefinition
    {
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

            [SerializeField, AllowNesting, ReadOnly]
            public float duration;

            public AnimSegmentDefinition segmentDefinition;
            public SegmentModifier segmentModifier;

            [SerializeField, AllowNesting, ShowIf("ShowForceDuration")]
            public float segmentNewDuration;

            [SerializeField, AllowNesting, ShowIf("ShowForceAnimatorSpeed")]
            public float segmentAnimatorSpeed;

            public bool ShowForceDuration => segmentModifier == SegmentModifier.ForceDuration;
            public bool ShowForceAnimatorSpeed => segmentModifier == SegmentModifier.ForceAnimatorSpeed;
        }

        [Button()]
        private void RefreshStateNameHash()
        {
            stateNameHash = Animator.StringToHash(stateName);
        }
    }
}