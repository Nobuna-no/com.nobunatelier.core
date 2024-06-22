using NaughtyAttributes;
using NobunAtelier;
using UnityEngine;

// This one is a bit tricky and require custom AnimationDrivenBattleAbilityController.
public class AbilityAnimatorTriggerDefinition : AbilityModuleDefinition
{
#if UNITY_EDITOR
    [SerializeField] private Animator m_animatorPrefab;
#endif
    // For now let's start small... Could be extended to support bool later...
    [SerializeField, AnimatorParam("m_animatorPrefab", AnimatorControllerParameterType.Trigger)]
    private string m_triggerName;

    public override IAbilityModuleInstance CreateInstance(AbilityController controller)
    {
        return new Instance(controller, this);
    }

    public class Instance : AbilityModuleInstance<AbilityAnimatorTriggerDefinition>
    {
        public override bool RunUpdate => false;

        private Animator m_animator;

        public Instance(AbilityController controller, AbilityAnimatorTriggerDefinition data)
            : base(controller, data)
        {
            if (controller.ModuleOwner.TryGetAbilityModule<AnimSequenceController>(out var animationModule))
            {
                m_animator = animationModule.Animator;
            }
            else
            {
                Debug.LogWarning($"Failed to get animator for a {Data}.", controller);
            }
        }

        public override void InitiateExecution()
        {
        }

        public override void ExecuteEffect()
        {
            if (m_animator == null)
            {
                return;
            }

            m_animator.SetTrigger(Data.m_triggerName);
        }

        public override void Stop()
        {
            if (m_animator == null)
            {
                return;
            }

            m_animator.ResetTrigger(Data.m_triggerName);
        }
    }
}
