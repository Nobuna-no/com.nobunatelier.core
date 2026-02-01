//using UnityEngine;
//
//namespace NobunAtelier
//{
//    public class AnimationDrivenAbilityController : AbilityController
//    {
//        [Header("Animation Driven")]
//        [SerializeField] private AnimSequenceController m_animationModule;
//
//        [SerializeField] private AnimSegmentDefinition m_abilityEffectStartSegment;
//        [SerializeField] private AnimSegmentDefinition m_abilityEffectStopSegment;
//        [SerializeField] private AnimSegmentDefinition m_abilityCompletedSegment;
//
//        public Animator Animator => m_animationModule.Animator;
//
//        private AnimationSegmentEvent m_currentAttackHitBeginEvent;
//        private AnimationSegmentEvent m_currentAttackHitEndEvent;
//        private AnimationSegmentEvent m_currentAttackEndEvent;
//        private bool m_isListening = false;
//
//        public override void ModuleInit(Character character)
//        {
//            base.ModuleInit(character);
//
//            ModuleOwner.TryGetAbilityModule(out m_animationModule);
//            Debug.Assert(m_animationModule, $"{this.name}: No animation module found in character {ModuleOwner.name}.", this);
//        }
//
//        internal override void OnAbilitySetup()
//        {
//            if (!enabled)
//            {
//                return;
//            }
//
//            // We always assume that we start from invalid state and reset.
//            // This is because we can't ensure the animation system to go through
//            // all the segment and do the clean up when abilities can be canceled.
//            RemoveListenersFromAnimSegments();
//        }
//
//        internal override void OnAbilityExecution()
//        {
//            RefreshAnimSegmentsListeners();
//        }
//
//        private void RefreshAnimSegmentsListeners()
//        {
//            if (m_isListening)
//            {
//                return;
//            }
//
//            Log.Record();
//
//            RemoveListenersFromAnimSegments();
//            if (m_animationModule.TryGetAnimationEventForSegment(m_abilityEffectStartSegment, out m_currentAttackHitBeginEvent))
//            {
//                m_currentAttackHitBeginEvent.AddListener(OnAbilityEffectStartSegment);
//            }
//            if (m_animationModule.TryGetAnimationEventForSegment(m_abilityEffectStopSegment, out m_currentAttackHitEndEvent))
//            {
//                m_currentAttackHitEndEvent.AddListener(OnAbilityEffectStopSegment);
//            }
//            if (m_animationModule.TryGetAnimationEventForSegment(m_abilityCompletedSegment, out m_currentAttackEndEvent))
//            {
//                m_currentAttackEndEvent.AddListener(OnAbilityCompletedSegment);
//            }
//
//            m_isListening = true;
//        }
//
//        private void RemoveListenersFromAnimSegments()
//        {
//            if (!m_isListening)
//            {
//                return;
//            }
//
//            Log.Record();
//
//            m_animationModule.ResetAnimationSpeed();
//            if (m_currentAttackHitBeginEvent != null)
//            {
//                m_currentAttackHitBeginEvent.RemoveListener(OnAbilityEffectStartSegment);
//                m_currentAttackHitBeginEvent = null;
//            }
//            if (m_currentAttackHitEndEvent != null)
//            {
//                m_currentAttackHitEndEvent.RemoveListener(OnAbilityEffectStopSegment);
//                m_currentAttackHitEndEvent = null;
//            }
//            if (m_currentAttackEndEvent != null)
//            {
//                m_currentAttackEndEvent.RemoveListener(OnAbilityCompletedSegment);
//                m_currentAttackEndEvent = null;
//            }
//
//            m_isListening = false;
//        }
//
//        private void OnAbilityEffectStartSegment(AnimSequenceDefinition.Segment arg0)
//        {
//            Log.Record();
//            StartAbilityEffect();
//        }
//
//        private void OnAbilityEffectStopSegment(AnimSequenceDefinition.Segment arg0)
//        {
//            Log.Record();
//            StopAbilityEffect();
//        }
//
//        private void OnAbilityCompletedSegment(AnimSequenceDefinition.Segment arg0)
//        {
//            Log.Record();
//            CompleteAbilityExecution();
//            m_animationModule.ResetAnimationSpeed();
//        }
//
//        //private void RefreshAnimData()
//        //{
//        //    // This is not going to work with a AbilityChainDefinition...
//        //    var modularAbilityDefinition = ActiveAbility as ModularAbilityDefinition;
//        //    if (modularAbilityDefinition == null)
//        //    {
//        //        Debug.LogError("Failed to get AnimationModule out of Active ability", this);
//        //        return;
//        //    }
//
//        //    // The animation module might come from anywhere... Charge attack, release, attack itself...
//        //    // Need a way to inject the anim data here...
//        //    foreach (var module in modularAbilityDefinition.Modules)
//        //    {
//        //        m_animData = module as BattleAbilityAnimationDefinition;
//        //        if (m_animData != null)
//        //        {
//        //            break;
//        //        }
//        //    }
//
//        //    if (m_animData == null)
//        //    {
//        //        Debug.LogWarning($"Trying to play {ActiveAbility} but no animation module found.", this);
//        //    }
//        //}
//    }
//}
//