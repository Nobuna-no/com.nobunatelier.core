using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Steps to bake AnimSequenceDefinition:
///     1. Add the AnimationModule_AnimSequence component on a model with an animator already setup.
///     2. Open the target animation and add events where you want to create segments
///     3. Set animation clip event with `OnAnimationSegmentTrigger` and assign your Segment
///     4. Open Window > NobunAnim Segment Baker
///         - Assign the AnimatorController to see the available animation segment to bake
///         - Assign the AnimSequenceCollection in which you want to bake the segment
///     5. Add all segment you want and press the Add to Collection button
/// </summary>

namespace NobunAtelier
{
    [System.Serializable]
    public class AnimationSegmentEvent : UnityEvent<AnimSequenceDefinition.Segment>
    { }

    [AddComponentMenu("NobunAtelier/Character/Modules/Animation Sequence")]
    public class AnimationModule_AnimSequence : AnimationModule
    {
        [SerializeField]
        private AnimSequenceDefinition m_animSeqDefinition;

        [SerializeField]
        private float m_frozenAnimatorSpeed;

        [SerializeField]
        private SegmentTrigger[] m_segmentTriggers;

        private Dictionary<AnimSegmentDefinition, AnimSequenceDefinition.Segment> m_animationSegmentsMap;
        private Dictionary<AnimSegmentDefinition, SegmentTrigger> m_segmentTriggerMap = new Dictionary<AnimSegmentDefinition, SegmentTrigger>();

        private bool m_isFrozen = false;
        private float m_cachedAnimatorSpeed = 1f;

        public bool TryGetAnimationEventForSegment(AnimSegmentDefinition segmentDefinition, out AnimationSegmentEvent segmentEvent)
        {
            segmentEvent = null;
            if (segmentDefinition == null)
            {
                return false;
            }

            if (m_segmentTriggerMap.ContainsKey(segmentDefinition))
            {
                segmentEvent = m_segmentTriggerMap[segmentDefinition].onSegmentTrigger;
                return true;
            }

            return false;
        }

        private void Awake()
        {
            m_segmentTriggerMap = new Dictionary<AnimSegmentDefinition, SegmentTrigger>(m_segmentTriggers.Length);

            for (int i = m_segmentTriggers.Length - 1; i >= 0; i--)
            {
                if (m_segmentTriggerMap.ContainsKey(m_segmentTriggers[i].segment))
                {
                    Debug.LogError($"{this.name}: SegmentTriggers[{i}] '{m_segmentTriggers[i].segment}' already exist! The event won't be triggered.");
                    continue;
                }

                m_segmentTriggerMap.Add(m_segmentTriggers[i].segment, m_segmentTriggers[i]);
            }

            m_animationSegmentsMap = new Dictionary<AnimSegmentDefinition, AnimSequenceDefinition.Segment>();
            if (m_animSeqDefinition)
            {
                SetAnimSequence(m_animSeqDefinition);
            }
        }

        private void Start()
        {
            if (Animator == null)
            {
                Debug.LogError("Animator reference not set!");
                return;
            }
        }

        public void OnAnimationSegmentTrigger(AnimSegmentDefinition segmentDefinition)
        {
            if (m_animationSegmentsMap.ContainsKey(segmentDefinition))
            {
                bool animatorSpeedChange = false;
                var currentSegment = m_animationSegmentsMap[segmentDefinition];
                switch (currentSegment.Modifier)
                {
                    case AnimSequenceDefinition.Segment.ModifierType.ForceDuration:
                        m_cachedAnimatorSpeed = currentSegment.Duration / currentSegment.NewDuration;
                        animatorSpeedChange = true;
                        break;

                    case AnimSequenceDefinition.Segment.ModifierType.ForceAnimatorSpeed:
                        m_cachedAnimatorSpeed = currentSegment.AnimatorSpeed;
                        animatorSpeedChange = true;
                        break;

                    case AnimSequenceDefinition.Segment.ModifierType.ResetAnimatorSpeed:
                    case AnimSequenceDefinition.Segment.ModifierType.None:
                        m_cachedAnimatorSpeed = 1;
                        animatorSpeedChange = true;
                        break;
                }

                if (animatorSpeedChange && !m_isFrozen)
                {
                    Animator.speed = m_cachedAnimatorSpeed;
                }
            }

            if (!m_segmentTriggerMap.ContainsKey(segmentDefinition))
            {
                m_segmentTriggerMap.Add(segmentDefinition, new SegmentTrigger()
                {
                    onSegmentTrigger = new AnimationSegmentEvent(),
                    segment = segmentDefinition
                });
            }

            if (m_animationSegmentsMap.ContainsKey(segmentDefinition))
            {
                m_segmentTriggerMap[segmentDefinition].onSegmentTrigger?.Invoke(m_animationSegmentsMap[segmentDefinition]);
            }
            else
            {
                m_segmentTriggerMap[segmentDefinition].onSegmentTrigger?.Invoke(null);
            }
        }

        public void FreezeAnimator()
        {
            m_cachedAnimatorSpeed = Animator.speed;
            Animator.speed = m_frozenAnimatorSpeed;
            m_isFrozen = true;
        }

        public void UnfreezeAnimation()
        {
            Animator.speed = m_cachedAnimatorSpeed;
            m_isFrozen = false;
        }

        public void SetAnimSequence(AnimSequenceDefinition animationDefinition)
        {
            m_animSeqDefinition = animationDefinition;

            m_animationSegmentsMap.Clear();
            for (int i = m_animSeqDefinition.segments.Length - 1; i >= 0; i--)
            {
                m_animationSegmentsMap.Add(m_animSeqDefinition.segments[i].SegmentDefinition, m_animSeqDefinition.segments[i]);
            }
        }

        public void PlayAnimSequence(AnimSequenceDefinition animationDefinition)
        {
            SetAnimSequence(animationDefinition);
            PlayAnimSequence();
        }

        public void PlayAnimSequence()
        {
            if (m_animSeqDefinition == null)
            {
                Debug.LogWarning($"{this.name}.PlayAnimationDefintion: AnimationDefintion is not valid.");
                return;
            }

            ResetAnimationSpeed();

            // Might be improve with more params...
            Animator.CrossFadeInFixedTime(m_animSeqDefinition.stateNameHash, m_animSeqDefinition.crossFadeDuration, 0);
        }

        public override void ResetAnimationSpeed()
        {
            m_cachedAnimatorSpeed = 1;

            if (m_isFrozen)
            {
                return;
            }
            base.ResetAnimationSpeed();
        }

#if UNITY_EDITOR

        [Button]
        private void Debug_PlayAnimationSequence()
        {
            PlayAnimSequence();
        }

#endif

        [System.Serializable]
        private struct SegmentTrigger
        {
            public AnimSegmentDefinition segment;
            public AnimationSegmentEvent onSegmentTrigger;
        }
    }
}