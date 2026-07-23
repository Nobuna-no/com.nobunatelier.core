#if ANIMANCER
using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Freezes all active Animancer states on every layer on enter and restores their speed on exit.
    /// Use for statue / hit-stun poses that lock the current frame.
    /// </summary>
    [AddComponentMenu("NobunAtelier/States/Modules/StateModule: Animancer Freeze")]
    public class StateModule_AnimancerFreeze : StateComponentModule
    {
        private struct FrozenSpeed
        {
            public AnimancerState State;
            public float PreviousSpeed;
        }

        [SerializeField] private AnimancerComponent m_Animancer;

        private readonly List<FrozenSpeed> m_FrozenStates = new List<FrozenSpeed>();

        public override void Enter()
        {
            m_FrozenStates.Clear();

            if (m_Animancer == null)
            {
                Debug.LogError($"[{nameof(StateModule_AnimancerFreeze)}] No AnimancerComponent assigned on {gameObject.name}", this);
                return;
            }

            var layers = m_Animancer.Layers;
            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                FreezeActiveStates(layers[layerIndex]);
            }
        }

        public override void Exit()
        {
            for (int i = 0; i < m_FrozenStates.Count; i++)
            {
                var frozen = m_FrozenStates[i];
                if (frozen.State == null)
                {
                    continue;
                }

                frozen.State.Speed = frozen.PreviousSpeed;
            }

            m_FrozenStates.Clear();
        }

        private void FreezeActiveStates(AnimancerLayer layer)
        {
            if (layer == null)
            {
                return;
            }

            var activeStates = layer.ActiveStates;
            for (int i = 0; i < activeStates.Count; i++)
            {
                var state = activeStates[i];
                if (state == null)
                {
                    continue;
                }

                m_FrozenStates.Add(new FrozenSpeed
                {
                    State = state,
                    PreviousSpeed = state.Speed,
                });
                state.Speed = 0f;
            }
        }

        private void Reset()
        {
            m_Animancer = GetComponentInParent<AnimancerComponent>();
        }
    }
}
#endif
