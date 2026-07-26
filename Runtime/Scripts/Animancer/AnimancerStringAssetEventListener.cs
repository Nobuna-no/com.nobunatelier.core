#if ANIMANCER
using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;
using UnityEngine.Events;
using UnityEvent = UnityEngine.Events.UnityEvent;
namespace NobunAtelier
{
    /// <summary>
    /// Listens for Animancer transition events bound by <see cref="StringAsset"/> name on an
    /// <see cref="AnimancerComponent"/> graph and invokes <see cref="UnityEvent"/>s.
    /// </summary>
    /// <remarks>
    /// On each clip/mixer child transition, set the event name to your <see cref="StringAsset"/>
    /// and use Animancer's bound callback (Invoke Bound Callback). Register the same
    /// <see cref="StringAsset"/> here and wire audio (or other logic) on <see cref="StringAssetEventBinding.OnInvoked"/>.
    /// </remarks>
    [AddComponentMenu("NobunAtelier/Animancer/Animancer String Asset Event Listener")]
    public class AnimancerStringAssetEventListener : MonoBehaviour
    {
        [Serializable]
        public class StringAssetEventBinding
        {
            [Tooltip("Must match the StringAsset used as the Animancer event name on the transition.")]
            [SerializeField] private StringAsset m_EventName;

            [SerializeField] private UnityEvent m_OnInvoked;

            public StringAsset EventName => m_EventName;

            public UnityEvent OnInvoked => m_OnInvoked;
        }

        [SerializeField] private AnimancerComponent m_Animancer;

        [SerializeField] private StringAssetEventBinding[] m_Bindings;

        private readonly List<RegisteredBinding> m_Registered = new List<RegisteredBinding>();

        private struct RegisteredBinding
        {
            public StringReference Name;
            public Action Handler;
        }

        private void Awake()
        {
            if (m_Animancer == null)
            {
                m_Animancer = GetComponentInParent<AnimancerComponent>();
            }
        }

        private void OnEnable()
        {
            RegisterBindings();
        }

        private void OnDisable()
        {
            UnregisterBindings();
        }

        private void RegisterBindings()
        {
            UnregisterBindings();

            if (m_Animancer == null || m_Bindings == null || m_Bindings.Length == 0)
            {
                return;
            }

            if (!m_Animancer.IsGraphInitialized)
            {
                m_Animancer.InitializeGraph();
            }

            var graphEvents = m_Animancer.Events;

            for (int i = 0; i < m_Bindings.Length; i++)
            {
                var binding = m_Bindings[i];
                if (binding == null || binding.EventName == null)
                {
                    continue;
                }

                StringReference eventName = binding.EventName;
                Action handler = binding.OnInvoked.Invoke;
                graphEvents.AddTo(eventName, handler);
                m_Registered.Add(new RegisteredBinding
                {
                    Name = eventName,
                    Handler = handler,
                });
            }
        }

        private void UnregisterBindings()
        {
            if (m_Registered.Count == 0 || m_Animancer == null || !m_Animancer.IsGraphInitialized)
            {
                m_Registered.Clear();
                return;
            }

            var graphEvents = m_Animancer.Events;
            for (int i = 0; i < m_Registered.Count; i++)
            {
                var registered = m_Registered[i];
                if (registered.Handler != null)
                {
                    graphEvents.Remove(registered.Name, registered.Handler);
                }
            }

            m_Registered.Clear();
        }
    }
}
#endif
