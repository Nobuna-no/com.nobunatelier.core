#if ANIMANCER
using Animancer;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Plays an Animancer clip when the owning state enters.
    /// Optional clip-end callback drives Manual <see cref="StateWithTransition{T,TCollection}"/> exits.
    /// </summary>
    [AddComponentMenu("NobunAtelier/States/Modules/StateModule: Animancer Clip")]
    public class StateModule_AnimancerClip : StateComponentModule
    {
        [SerializeField] private AnimancerComponent m_Animancer;

        [Tooltip("Clip transition played on state enter.")]
        [SerializeField] private TransitionAsset m_Transition;

        [Tooltip("Override clip length. Use -1 for the transition default.")]
        [SerializeField] private float m_Duration = -1f;

        [Header("Manual transition")]
        [Tooltip("When enabled, SetState is called on clip end using Next State On Clip End.")]
        [SerializeField] private bool m_CompleteStateOnClipEnd;

        [SerializeField] private StateDefinition m_NextStateOnClipEnd;

        private AnimancerState m_AnimancerState;

        public override void Enter()
        {
            if (m_Animancer == null)
            {
                Debug.LogError($"[{nameof(StateModule_AnimancerClip)}] No AnimancerComponent assigned on {gameObject.name}", this);
                return;
            }

            if (m_Transition == null || m_Transition.GetTransition() == null)
            {
                Debug.LogWarning($"[{nameof(StateModule_AnimancerClip)}] No transition assigned on {gameObject.name}", this);
                return;
            }

            m_AnimancerState = m_Animancer.Play(m_Transition);
            if (m_Duration > 0f)
            {
                m_AnimancerState.Duration = m_Duration;
            }

            if (m_CompleteStateOnClipEnd && m_NextStateOnClipEnd != null)
            {
                m_AnimancerState.Events(this).OnEnd += OnClipEnd;
            }
        }

        public override void Exit()
        {
            if (m_AnimancerState != null)
            {
                m_AnimancerState.Events(this).OnEnd -= OnClipEnd;
                m_AnimancerState = null;
            }
        }

        private void OnClipEnd()
        {
            if (ModuleOwner == null || m_NextStateOnClipEnd == null)
            {
                return;
            }

            ModuleOwner.SetState(m_NextStateOnClipEnd.GetType(), m_NextStateOnClipEnd);
        }

        private void Reset()
        {
            m_Animancer = GetComponentInParent<AnimancerComponent>();
        }
    }
}
#endif
