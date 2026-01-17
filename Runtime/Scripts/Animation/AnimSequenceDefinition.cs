using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

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
        [InfoBox("Steps to bake AnimSequenceDefinition:" +
            "\n\t1. Add the 'AnimSequenceController' component on a model with an animator already setup" +
            "\n\t2. Open the target animation and add events where you want to create segments" +
            "\n\t3. Set animation clip event with `OnAnimationSegmentTrigger` and assign your Segment" +
            "\n\t4. Open Window > NobunAtelier > Anim Segment Baker:" +
            "\n\t\t- Assign the AnimatorController to see the available animation segment to bake" +
            "\n\t\t- Assign the AnimSequenceCollection in which you want to bake the segment" +
            "\n\t5. Add all segment you want and press the Add to Collection button")]
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

            public float Duration => m_Duration;
            public float NewDuration => m_NewDuration;
            public AnimSegmentDefinition SegmentDefinition => m_Definition;
            public ModifierType Modifier => m_Modifier;
            public float AnimatorSpeed => m_AnimatorSpeed;

            [SerializeField, AllowNesting, ReadOnly, FormerlySerializedAs("m_duration")]
            private float m_Duration;
            [SerializeField, AllowNesting, ReadOnly, FormerlySerializedAs("m_definition")]
            private AnimSegmentDefinition m_Definition;
            [SerializeField, FormerlySerializedAs("m_modifier")]
            private ModifierType m_Modifier;
            [SerializeField, AllowNesting, ShowIf("ShowForceDuration"), FormerlySerializedAs("m_newDuration")]
            private float m_NewDuration;
            [SerializeField, AllowNesting, ShowIf("ShowForceAnimatorSpeed"), FormerlySerializedAs("m_animatorSpeed")]
            private float m_AnimatorSpeed;

#if UNITY_EDITOR
            // NaughtyAttribute's ShowIf arguments.
            public bool ShowForceDuration => m_Modifier == ModifierType.ForceDuration;
            public bool ShowForceAnimatorSpeed => m_Modifier == ModifierType.ForceAnimatorSpeed;
#endif

            public Segment(float duration, AnimSegmentDefinition segmentDef)
            {
                m_Duration = duration;
                m_NewDuration = duration;
                m_Definition = segmentDef;
                m_AnimatorSpeed = 1;
            }

            public float GetEstimatedDuration()
            {
                switch (m_Modifier)
                {
                    case AnimSequenceDefinition.Segment.ModifierType.None:
                    case AnimSequenceDefinition.Segment.ModifierType.ResetAnimatorSpeed:
                        return m_Duration;

                    case AnimSequenceDefinition.Segment.ModifierType.ForceDuration:
                        return m_NewDuration;

                    case AnimSequenceDefinition.Segment.ModifierType.ForceAnimatorSpeed:
                        return m_Duration * m_AnimatorSpeed;
                }

                return m_Duration;
            }
        }

        [Button()]
        private void RefreshStateNameHash()
        {
            stateNameHash = Animator.StringToHash(stateName);
        }
    }
}
