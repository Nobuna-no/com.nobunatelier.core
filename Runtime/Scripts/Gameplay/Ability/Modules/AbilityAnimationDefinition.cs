using NaughtyAttributes;
using NobunAtelier;
using UnityEngine;

// This one is a bit tricky and require custom AnimationDrivenBattleAbilityController.
public class AbilityAnimationDefinition : AbilityModuleDefinition
{
    [SerializeField] private AnimSequenceDefinition m_animSequence;
    [SerializeField] private AbilityLoadableParticleSystem[] m_VFX;
    [SerializeField] private AbilityLoadableAudioSource[] m_SFX;

    public override bool IsInstanceAbilityProcessor => true;

    public override IAbilityModuleInstance CreateInstance(AbilityController controller)
    {
        return new Instance(controller, this);
    }

    public class Instance : AbilityModuleInstance<AbilityAnimationDefinition>, IModularAbilityProcessor
    {
        // Just for now, but might change in the future...
        public override bool RunUpdate => false;

        private AbilityLoadableSFXFactory m_sfxFactory;
        private AbilityLoadableVFXFactory m_vfxFactory;
        private AnimSequenceController m_animationModule;
        private bool m_isRegistered;

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

        public void RequestExecution()
        {
            Controller.EnqueueAbilityExecution();
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
    }
}
