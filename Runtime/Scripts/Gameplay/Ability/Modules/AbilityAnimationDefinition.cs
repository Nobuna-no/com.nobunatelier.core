using NaughtyAttributes;
using NobunAtelier;
using UnityEngine;

// Segment-driven timing driver for AnimSequenceController.
public class AbilityAnimationDefinition : AbilityExecutionDriverModuleDefinition<AbilityAnimationDefinition>
{
    [SerializeField] private AnimSequenceDefinition m_animSequence;
    [SerializeField] private AnimSegmentDefinition m_EffectStartSegment;
    [SerializeField] private AnimSegmentDefinition m_EffectStopSegment;
    [SerializeField] private AnimSegmentDefinition m_CompletedSegment;
    [SerializeField] private AbilityLoadableParticleSystem[] m_VFX;
    [SerializeField] private AbilityLoadableAudioSource[] m_SFX;

    public override IAbilityModuleInstance CreateInstance(AbilityController controller)
    {
        return new Instance(controller, this);
    }

    public class Instance : ExecutionDriverInstance
    {
        // Just for now, but might change in the future...
        public override bool RunUpdate => false;

        private AbilityLoadableSFXFactory m_sfxFactory;
        private AbilityLoadableVFXFactory m_vfxFactory;
        private AnimSequenceController m_animationModule;
        private bool m_isRegistered;
        private bool m_isListening;
        private AnimationSegmentEvent m_effectStartEvent;
        private AnimationSegmentEvent m_effectStopEvent;
        private AnimationSegmentEvent m_executionCompleteEvent;

        public Instance(AbilityController controller, AbilityAnimationDefinition data)
            : base(controller, data)
        {
            if (!controller.ModuleOwner.TryGetAbilityModule(out m_animationModule))
            {
                Debug.LogError($"{controller.name} can't use {Data} as it's Character doesn't provide an AnimationModule.");
                return;
            }

            m_sfxFactory = new AbilityLoadableSFXFactory(Data.m_SFX);
            m_vfxFactory = new AbilityLoadableVFXFactory(Data.m_VFX);
            m_isRegistered = false;
        }

        public override void InitiateExecution()
        {
            if (m_animationModule == null)
            {
                return;
            }

            Stop();

            m_animationModule.PlayAnimSequence(Data.m_animSequence);
            m_sfxFactory.RegisterResources();
            m_vfxFactory.RegisterResources();
            m_isRegistered = true;
        }

        protected override void OnExecutionRequested()
        {
            RefreshAnimSegmentsListeners();
        }

        public override void ExecuteEffect()
        {
            if (m_animationModule == null)
            {
                return;
            }

            if (!m_isRegistered)
            {
                InitiateExecution();
            }

            m_sfxFactory.PlayAll(m_animationModule.transform);
            m_vfxFactory.PlayAll(m_animationModule.transform);
        }

        public override void Stop()
        {
            if (m_animationModule == null)
            {
                return;
            }

            m_sfxFactory.UnregisterResources();
            m_vfxFactory.UnregisterResources();
            m_isRegistered = false;
        }

        protected override void OnTimingDriverReset()
        {
            RemoveListenersFromAnimSegments();

            if (m_isRegistered)
            {
                m_sfxFactory.UnregisterResources();
                m_vfxFactory.UnregisterResources();
                m_isRegistered = false;
            }

            if (m_animationModule != null)
            {
                m_animationModule.ResetAnimationSpeed();
            }
        }

        private void RefreshAnimSegmentsListeners()
        {
            if (m_isListening || m_animationModule == null)
            {
                return;
            }

            RemoveListenersFromAnimSegments();
            if (Data.m_EffectStartSegment != null && m_animationModule.TryGetAnimationEventForSegment(Data.m_EffectStartSegment, out m_effectStartEvent))
            {
                m_effectStartEvent.AddListener(OnAbilityEffectStartSegment);
            }
            if (Data.m_EffectStopSegment != null && m_animationModule.TryGetAnimationEventForSegment(Data.m_EffectStopSegment, out m_effectStopEvent))
            {
                m_effectStopEvent.AddListener(OnAbilityEffectStopSegment);
            }
            if (Data.m_CompletedSegment != null && m_animationModule.TryGetAnimationEventForSegment(Data.m_CompletedSegment, out m_executionCompleteEvent))
            {
                m_executionCompleteEvent.AddListener(OnAbilityCompletedSegment);
            }

            m_isListening = true;
        }

        private void RemoveListenersFromAnimSegments()
        {
            if (!m_isListening || m_animationModule == null)
            {
                return;
            }

            if (m_effectStartEvent != null)
            {
                m_effectStartEvent.RemoveListener(OnAbilityEffectStartSegment);
                m_effectStartEvent = null;
            }
            if (m_effectStopEvent != null)
            {
                m_effectStopEvent.RemoveListener(OnAbilityEffectStopSegment);
                m_effectStopEvent = null;
            }
            if (m_executionCompleteEvent != null)
            {
                m_executionCompleteEvent.RemoveListener(OnAbilityCompletedSegment);
                m_executionCompleteEvent = null;
            }

            m_isListening = false;
        }

        private void OnAbilityEffectStartSegment(AnimSequenceDefinition.Segment arg0)
        {
            Controller.Log.Record();
            FireEffectStart();
        }

        private void OnAbilityEffectStopSegment(AnimSequenceDefinition.Segment arg0)
        {
            Controller.Log.Record();
            FireEffectStop();
        }

        private void OnAbilityCompletedSegment(AnimSequenceDefinition.Segment arg0)
        {
            Controller.Log.Record();
            FireExecutionComplete();
            RemoveListenersFromAnimSegments();
        }
    }
}
