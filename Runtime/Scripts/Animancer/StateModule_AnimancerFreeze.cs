#if ANIMANCER
using Animancer;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Freezes the current Animancer state on enter and restores its speed on exit.
    /// Use for statue / hit-stun poses that lock the current frame.
    /// </summary>
    [AddComponentMenu("NobunAtelier/States/Modules/StateModule: Animancer Freeze")]
    public class StateModule_AnimancerFreeze : StateComponentModule
    {
        [SerializeField] private AnimancerComponent m_Animancer;
        [SerializeField, Min(0)] private int m_LayerIndex;

        private AnimancerState m_FrozenState;
        private float m_PreviousSpeed = 1f;

        public override void Enter()
        {
            if (m_Animancer == null)
            {
                Debug.LogError($"[{nameof(StateModule_AnimancerFreeze)}] No AnimancerComponent assigned on {gameObject.name}", this);
                return;
            }

            m_FrozenState = m_Animancer.Layers[m_LayerIndex].CurrentState;
            if (m_FrozenState == null)
            {
                return;
            }

            m_PreviousSpeed = m_FrozenState.Speed;
            m_FrozenState.Speed = 0f;
        }

        public override void Exit()
        {
            if (m_FrozenState == null)
            {
                return;
            }

            m_FrozenState.Speed = m_PreviousSpeed;
            m_FrozenState = null;
        }

        private void Reset()
        {
            m_Animancer = GetComponentInParent<AnimancerComponent>();
        }
    }
}
#endif
