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

    [AddComponentMenu("NobunAtelier/Character/Modules/Animation Sequence Controller")]
    public class AnimSequenceController : AnimationModule
    {
        [Header("Anim Sequence Controller")]
        [SerializeField]
        private AnimSequenceDefinition m_sequenceToPlay;

        [SerializeField]
        private float m_frozenAnimatorSpeed;

        [SerializeField, Tooltip("You can explicitly define segments to be listened to with Unity events.")]
        private SegmentTrigger[] m_segmentTriggers;

        [Header("Debug")]
        [SerializeField] private ContextualLogManager.LogSettings m_LogSettings;

        private Dictionary<AnimSegmentDefinition, AnimSequenceDefinition.Segment> m_animationSegmentsMap;
        private Dictionary<AnimSegmentDefinition, SegmentTrigger> m_segmentTriggerMap = new Dictionary<AnimSegmentDefinition, SegmentTrigger>();

        private bool m_isFrozen = false;
        private float m_cachedAnimatorSpeed = 1f;
        private AnimSegmentDefinition m_lastSegmentRaised = null;
        public ContextualLogManager.LogPartition Log { get; private set; }

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

        public void OnAnimationSegmentTrigger(AnimSegmentDefinition segmentDefinition)
        {
            Log.Record($"AnimSegment <b>{segmentDefinition}</b> triggered.");

            // This usually happens when chaining different animations together.
            if (segmentDefinition.ExpectedPriorSegment != null && segmentDefinition.ExpectedPriorSegment != m_lastSegmentRaised)
            {
                Log.Record($"AnimSegment <b>{segmentDefinition}</b> triggered and " +
                    $"was expecting prior segment to be '<b>{segmentDefinition.ExpectedPriorSegment}</b>' " +
                    $"but was '<b>{m_lastSegmentRaised}</b>'. Skipping.");
                return;
            }

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
                    Log.Record($"animatorSpeedChange from '{Animator.speed}' to '{m_cachedAnimatorSpeed}'.");
                    Animator.speed = m_cachedAnimatorSpeed;
                }
            }

            // Lazy init.
            if (!m_segmentTriggerMap.ContainsKey(segmentDefinition))
            {
                m_segmentTriggerMap.Add(segmentDefinition, new SegmentTrigger()
                {
                    onSegmentTrigger = new AnimationSegmentEvent(),
                    segment = segmentDefinition
                });
            }

            // If the segment is not found, dictionary still return default value for the type (aka. null).
            m_animationSegmentsMap.TryGetValue(segmentDefinition, out var value);
            m_segmentTriggerMap[segmentDefinition].onSegmentTrigger?.Invoke(value);

            m_lastSegmentRaised = segmentDefinition;
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
            m_sequenceToPlay = animationDefinition;

            m_animationSegmentsMap.Clear();

            foreach (var segment in m_sequenceToPlay.segments)
            {
                if (!m_segmentTriggerMap.ContainsKey(segment.SegmentDefinition))
                {
                    m_segmentTriggerMap.Add(segment.SegmentDefinition, new SegmentTrigger()
                    {
                        onSegmentTrigger = new AnimationSegmentEvent(),
                        segment = segment.SegmentDefinition
                    });
                }
            }

            for (int i = m_sequenceToPlay.segments.Length - 1; i >= 0; i--)
            {
                m_animationSegmentsMap.Add(m_sequenceToPlay.segments[i].SegmentDefinition, m_sequenceToPlay.segments[i]);
            }
            m_lastSegmentRaised = null;
        }

        public void PlayAnimSequence(AnimSequenceDefinition animationDefinition)
        {
            SetAnimSequence(animationDefinition);
            PlayAnimSequence();
        }

        public void PlayAnimSequence()
        {
            if (m_sequenceToPlay == null)
            {
                Log.Record($"Provided {m_sequenceToPlay.GetType().Name} is not valid.", ContextualLogManager.LogTypeFilter.Warning);
                return;
            }

            ResetAnimationSpeed();

            // Might be improve with more params...
            Animator.CrossFadeInFixedTime(m_sequenceToPlay.stateNameHash, m_sequenceToPlay.crossFadeDuration, 0);
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

        private void Awake()
        {
            m_segmentTriggerMap = new Dictionary<AnimSegmentDefinition, SegmentTrigger>(m_segmentTriggers.Length);

            for (int i = m_segmentTriggers.Length - 1; i >= 0; i--)
            {
                if (m_segmentTriggerMap.ContainsKey(m_segmentTriggers[i].segment))
                {
                    Log.Record($"'{m_segmentTriggers[i].segment}' already exist! SegmentTriggers[{i}]'s event won't be triggered.",
                        ContextualLogManager.LogTypeFilter.Error);
                    continue;
                }

                m_segmentTriggerMap.Add(m_segmentTriggers[i].segment, m_segmentTriggers[i]);
            }

            m_animationSegmentsMap = new Dictionary<AnimSegmentDefinition, AnimSequenceDefinition.Segment>();
            if (m_sequenceToPlay)
            {
                SetAnimSequence(m_sequenceToPlay);
            }
        }

        private void Start()
        {
            if (Animator == null)
            {
                Log.Record("Animator reference is not set!", ContextualLogManager.LogTypeFilter.Error);
                return;
            }
        }

        private void OnEnable()
        {
            Log = ContextualLogManager.Register(this, m_LogSettings);
        }

        private void OnDisable()
        {
            ContextualLogManager.Unregister(Log);
        }

#if UNITY_EDITOR
        [Button, ContextMenu("Debug_PlayAnimationSequence")]
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