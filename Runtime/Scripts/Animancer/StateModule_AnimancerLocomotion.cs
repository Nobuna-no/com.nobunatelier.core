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

        [Tooltip("Animancer layer index. 0 = base body locomotion.")]
        [SerializeField, Min(0)] private int m_LayerIndex;
        [SerializeField] private Character m_Character;

        [Tooltip("Transition Asset wrapping a Linear Mixer (idle / walk / run). Thresholds should use planar speed units (m/s).")]
        [SerializeField] private TransitionAsset m_LocomotionMixer;

        [Tooltip("When > 0, smooths mixer parameter changes. 0 = instant.")]
        [SerializeField, Min(0f)] private float m_ParameterSmoothing = 12f;

        [Tooltip("When enabled, higher layers that already have playing states keep full weight (e.g. action overlay on layer 1 while base locomotion restarts on layer 0).")]
        [SerializeField] private bool m_PreserveActiveOverlayLayers = true;

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

            m_MixerState = m_Animancer.Layers[m_LayerIndex].Play(m_LocomotionMixer) as LinearMixerState;
            if (m_MixerState == null)
            {
                Debug.LogError(
                    $"[{nameof(StateModule_AnimancerLocomotion)}] '{m_LocomotionMixer.name}' is not a Linear Mixer transition on {gameObject.name}",
                    this);
                return;
            }

            m_SmoothedParameter = m_Character != null ? m_Character.GetMoveSpeed() : 0f;
            m_MixerState.Parameter = m_SmoothedParameter;
            PreserveActiveOverlayLayers();
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

        /// <summary>
        /// Re-plays the locomotion mixer on the configured layer.
        /// </summary>
        public void Refresh()
        {
            if (m_Character == null)
            {
                m_Character = GetComponentInParent<Character>();
            }

            if (m_Animancer == null || m_LocomotionMixer == null || m_LocomotionMixer.GetTransition() == null)
            {
                return;
            }

            m_MixerState = m_Animancer.Layers[m_LayerIndex].Play(m_LocomotionMixer) as LinearMixerState;
            if (m_MixerState == null)
            {
                return;
            }

            m_SmoothedParameter = m_Character != null ? m_Character.GetMoveSpeed() : 0f;
            m_MixerState.Parameter = m_SmoothedParameter;
            PreserveActiveOverlayLayers();
        }

        private void PreserveActiveOverlayLayers()
        {
            if (!m_PreserveActiveOverlayLayers || m_Animancer == null)
            {
                return;
            }

            var layers = m_Animancer.Layers;
            for (int i = m_LayerIndex + 1; i < layers.Count; i++)
            {
                var layer = layers[i];
                if (layer == null || !LayerHasActiveOverlay(layer))
                {
                    continue;
                }

                layer.CancelFade();
                if (layer.Weight < 1f)
                {
                    layer.Weight = 1f;
                }
            }
        }

        private static bool LayerHasActiveOverlay(AnimancerLayer layer)
        {
            var activeStates = layer.ActiveStates;
            for (int i = 0; i < activeStates.Count; i++)
            {
                var state = activeStates[i];
                if (state != null && state.IsPlaying && state.Weight > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        private void Reset()
        {
            m_Animancer = GetComponentInParent<AnimancerComponent>();
            m_Character = GetComponentInParent<Character>();
        }
    }
}
#endif
