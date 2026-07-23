#if ANIMANCER
using Animancer;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Drives a <see cref="LinearMixerTransition"/> from planar character speed while the owning state is active.
    /// Use on posture states such as Moving (idle / walk / run thresholds on the mixer asset).
    /// </summary>
    [AddComponentMenu("NobunAtelier/States/Modules/StateModule: Animancer Locomotion")]
    public class StateModule_AnimancerLocomotion : StateComponentModule
    {
        [SerializeField] private AnimancerComponent m_Animancer;
        [SerializeField] private Character m_Character;

        [Tooltip("Transition Asset wrapping a Linear Mixer (idle / walk / run). Thresholds should use planar speed units (m/s).")]
        [SerializeField] private TransitionAsset m_LocomotionMixer;

        [Tooltip("When > 0, smooths mixer parameter changes. 0 = instant.")]
        [SerializeField, Min(0f)] private float m_ParameterSmoothing = 12f;

        private LinearMixerState m_MixerState;
        private float m_SmoothedParameter;

        public override void Enter()
        {
            if (m_Character == null)
            {
                m_Character = GetComponentInParent<Character>();
            }

            if (m_Animancer == null)
            {
                Debug.LogError($"[{nameof(StateModule_AnimancerLocomotion)}] No AnimancerComponent assigned on {gameObject.name}", this);
                return;
            }

            if (m_LocomotionMixer == null || m_LocomotionMixer.GetTransition() == null)
            {
                Debug.LogWarning($"[{nameof(StateModule_AnimancerLocomotion)}] No locomotion mixer assigned on {gameObject.name}", this);
                return;
            }

            m_MixerState = m_Animancer.Play(m_LocomotionMixer) as LinearMixerState;
            if (m_MixerState == null)
            {
                Debug.LogError(
                    $"[{nameof(StateModule_AnimancerLocomotion)}] '{m_LocomotionMixer.name}' is not a Linear Mixer transition on {gameObject.name}",
                    this);
                return;
            }

            m_SmoothedParameter = m_Character != null ? m_Character.GetMoveSpeed() : 0f;
            m_MixerState.Parameter = m_SmoothedParameter;
        }

        public override void Tick(float deltaTime)
        {
            if (m_MixerState == null || m_Character == null)
            {
                return;
            }

            float target = m_Character.GetMoveSpeed();
            if (m_ParameterSmoothing <= 0f)
            {
                m_SmoothedParameter = target;
            }
            else
            {
                m_SmoothedParameter = Mathf.Lerp(
                    m_SmoothedParameter,
                    target,
                    1f - Mathf.Exp(-m_ParameterSmoothing * deltaTime));
            }

            m_MixerState.Parameter = m_SmoothedParameter;
        }

        public override void Exit()
        {
            m_MixerState = null;
        }

        private void Reset()
        {
            m_Animancer = GetComponentInParent<AnimancerComponent>();
            m_Character = GetComponentInParent<Character>();
        }
    }
}
#endif
