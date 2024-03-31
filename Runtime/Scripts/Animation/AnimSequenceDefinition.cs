using NaughtyAttributes;
using UnityEngine;
using static NobunAtelier.AnimSequenceDefinition;
using static Unity.Cinemachine.CinemachineFreeLookModifier;

namespace NobunAtelier
{
    /// <summary>
    /// Stores additional information about an animation state within an AnimatorController.
    /// It stores segments of animation by on AnimSegmentDefinition. It is then possible to adds Modifier on
    /// each segments to modulate the animator speed or force the duration of a sequence.
    /// Data can be generated using NobunAtelier AnimationSequenceBaker tool.
    /// </summary>
    public class AnimSequenceDefinition : DataDefinition
    {
        [Header("AnimSequence")]
        [InfoBox("This name should correspond to the AnimatorState of the AnimatorController holding the animation clip.")]
        public string stateName;

        [AllowNesting, NaughtyAttributes.ReadOnly]
        public int stateNameHash;

        // public AnimationClip clip;
        public float crossFadeDuration = 0.1f;

        public Segment[] segments;

        [System.Serializable]
        public class Segment
        {
            public enum ModifierType
            {
                None,
                ForceAnimatorSpeed,
                ResetAnimatorSpeed,
                ForceDuration
            }

            public float Duration => m_duration;
            public float NewDuration => m_newDuration;
            public AnimSegmentDefinition SegmentDefinition => m_definition;
            public ModifierType Modifier => m_modifier;
            public float AnimatorSpeed => m_animatorSpeed;

            [SerializeField, AllowNesting, ReadOnly]
            private float m_duration;
            [SerializeField, AllowNesting, ReadOnly]
            private AnimSegmentDefinition m_definition;
            [SerializeField]
            private ModifierType m_modifier;
            [SerializeField, AllowNesting, ShowIf("ShowForceDuration")]
            private float m_newDuration;
            [SerializeField, AllowNesting, ShowIf("ShowForceAnimatorSpeed")]
            private float m_animatorSpeed;

#if UNITY_EDITOR
            // NaughtyAttribute's ShowIf arguments.
            public bool ShowForceDuration => m_modifier == ModifierType.ForceDuration;
            public bool ShowForceAnimatorSpeed => m_modifier == ModifierType.ForceAnimatorSpeed;
#endif

            public Segment(float duration, AnimSegmentDefinition segmentDef)
            {
                m_duration = duration;
                m_newDuration = duration;
                m_definition = segmentDef;
                m_animatorSpeed = 1;
            }

            public float GetEstimatedDuration()
            {
                switch (m_modifier)
                {
                    case AnimSequenceDefinition.Segment.ModifierType.None:
                    case AnimSequenceDefinition.Segment.ModifierType.ResetAnimatorSpeed:
                        return m_duration;

                    case AnimSequenceDefinition.Segment.ModifierType.ForceDuration:
                        return m_newDuration;

                    case AnimSequenceDefinition.Segment.ModifierType.ForceAnimatorSpeed:
                        return m_duration * m_animatorSpeed;
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
