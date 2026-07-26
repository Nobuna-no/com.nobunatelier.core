using System;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    /// <summary>
    /// Subscribes to <see cref="GameplayEventDefinition.Raised"/> and invokes inspector-wired
    /// <see cref="UnityEvent"/>s, bridging data-driven event tokens into scene callbacks.
    /// </summary>
    /// <remarks>
    /// Raise events from ability drivers, <see cref="GameplayEventDefinition.Raise"/>, or UnityEvents
    /// targeting a <see cref="GameplayEventDefinition"/> asset. Register matching definitions here.
    /// </remarks>
    [AddComponentMenu("NobunAtelier/Gameplay/Gameplay Event Listener")]
    public class GameplayEventListener : MonoBehaviour
    {
        [Serializable]
        public class GameplayEventBinding
        {
            [Tooltip("Gameplay event asset to listen for (reference equality).")]
            [SerializeField] private GameplayEventDefinition m_Event;

            [SerializeField] private UnityEvent m_OnInvoked;

            public GameplayEventDefinition Event => m_Event;

            public UnityEvent OnInvoked => m_OnInvoked;
        }

        [SerializeField] private GameplayEventBinding[] m_Bindings;

        private void OnEnable()
        {
            GameplayEventDefinition.Raised += HandleRaised;
        }

        private void OnDisable()
        {
            GameplayEventDefinition.Raised -= HandleRaised;
        }

        private void HandleRaised(GameplayEventDefinition gameplayEvent)
        {
            if (gameplayEvent == null || m_Bindings == null || m_Bindings.Length == 0)
            {
                return;
            }

            for (int i = 0; i < m_Bindings.Length; i++)
            {
                GameplayEventBinding binding = m_Bindings[i];
                if (binding == null || binding.Event != gameplayEvent)
                {
                    continue;
                }

                binding.OnInvoked?.Invoke();
            }
        }
    }
}
