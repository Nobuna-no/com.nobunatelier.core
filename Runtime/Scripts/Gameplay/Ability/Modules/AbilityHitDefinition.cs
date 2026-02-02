using NobunAtelier.Gameplay;
using UnityEngine;

namespace NobunAtelier
{
    public class AbilityHitDefinition : AbilityModuleDefinition
    {
        [SerializeField] private HitDefinition m_hitDefinition;
        [SerializeField] private TeamDefinition.Target m_hitTarget = TeamDefinition.Target.Enemies;
        [SerializeField] private AbilityLoadableHitbox[] m_hitbox;
        [SerializeField] private AbilityLoadableParticleSystem[] m_impactVFX;
        [SerializeField] private AbilityLoadableAudioSource[] m_impactSFX;
        [SerializeField] private EffectTarget m_fxOrigin = EffectTarget.Self;

        public override IAbilityModuleInstance CreateInstance(AbilityController controller)
        {
            return new Instance(controller, this);
        }

        public class Instance : AbilityModuleInstance<AbilityHitDefinition>
        {
            public override bool RunUpdate => true;

            private AbilityLoadableSFXFactory m_sfxFactory;
            private AbilityLoadableVFXFactory m_vfxFactory;
            private AbilityLoadableHitboxFactory m_hitboxFactory;
            private Transform m_target;
            private bool m_isRegistered;

            public Instance(AbilityController controller, AbilityHitDefinition data)
                : base(controller, data)
            {
                m_sfxFactory = new AbilityLoadableSFXFactory(Data.m_impactSFX);
                m_vfxFactory = new AbilityLoadableVFXFactory(Data.m_impactVFX);
                m_hitboxFactory = new AbilityLoadableHitboxFactory(Data.m_hitbox);
                m_isRegistered = false;
            }

            public override void InitiateExecution()
            {
                AbilityModuleUtility.TryGetTarget(Controller, Data.m_fxOrigin, out m_target);

                Stop();
                m_sfxFactory.RegisterResources();
                m_vfxFactory.RegisterResources();
                m_hitboxFactory.RegisterResources();

                m_hitboxFactory.AddListenerOnHit(OnHit);
                m_hitboxFactory.SetupHitboxes(m_target, Data.m_hitTarget, Controller.Team, Data.m_hitDefinition);
                // m_hitbox.OnHit.AddListener(OnHit);
                // SetupHitbox(m_target, controller.Team);
                m_isRegistered = true;
            }

            public override void ExecuteEffect()
            {
                if (!m_isRegistered)
                {
                    InitiateExecution();
                }
                m_hitboxFactory.PlayAll(m_target);
            }

            public override void Update(float deltaTime)
            {
                if (!m_isRegistered)
                {
                    return;
                }
                m_hitboxFactory.UpdateHitbox(m_target);
            }

            public override void Stop()
            {
                m_sfxFactory.UnregisterResources();
                m_vfxFactory.UnregisterResources();
                m_hitboxFactory.UnregisterResources();
                m_isRegistered = false;
            }

            private void OnHit(HitInfo hitInfo)
            {
                if (!m_isRegistered)
                {
                    return;
                }

                // TODO: Add a way to inject position?
                // m_FX.transform.position = hitInfo.ImpactLocation;
                m_vfxFactory.PlayAll(m_target);
                m_sfxFactory.PlayAll(m_target);
            }
        }
    }
}