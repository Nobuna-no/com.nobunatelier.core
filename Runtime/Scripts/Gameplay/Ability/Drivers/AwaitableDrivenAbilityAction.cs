using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Time-driven ability action driver. Fires <see cref="GameplayEventDefinition"/>s at specified time offsets.
    /// Used for skills without animation (buffs, projectiles, etc.).
    /// </summary>
    [Serializable]
    public class AwaitableDrivenAbilityAction : IAbilityActionDriver
    {
        [Serializable]
        public class TimedEvent
        {
            [Tooltip("The GameplayEvent to fire.")]
            [SerializeField] private GameplayEventDefinition m_Event;

            [Tooltip("Time offset in seconds from execution start.")]
            [Min(0f)]
            [SerializeField] private float m_TimeOffset;

            public GameplayEventDefinition Event => m_Event;
            public float TimeOffset => m_TimeOffset;
        }

        [SerializeField] private List<TimedEvent> m_Events;

        private IAbilityActionDriverCallbacks m_Callbacks;
        private CancellationTokenSource m_Cts;
        private CancellationToken m_ExternalToken;

        public GameplayEventDefinition[] GetAvailableEvents()
        {
            if (m_Events == null || m_Events.Count == 0)
                return Array.Empty<GameplayEventDefinition>();

            var events = new GameplayEventDefinition[m_Events.Count];
            for (int i = 0; i < m_Events.Count; i++)
            {
                events[i] = m_Events[i].Event;
            }
            return events;
        }

        public void Initialize(in AbilityActionDriverContext context)
        {
            m_Callbacks = context.Callbacks;
            m_ExternalToken = context.Token;
        }

        public void RequestExecution()
        {
            CancelInternal();
            m_Cts = m_ExternalToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(m_ExternalToken)
                : new CancellationTokenSource();
            ExecuteAsync(m_Cts.Token).FireAndForget();
        }

        public void Reset()
        {
            CancelInternal();
            m_Callbacks = null;
        }

        public void Cancel()
        {
            CancelInternal();
            m_Callbacks = null;
        }

        private void CancelInternal()
        {
            if (m_Cts == null)
                return;

            m_Cts.Cancel();
            m_Cts.Dispose();
            m_Cts = null;
        }

        private async Awaitable ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (m_Events == null || m_Events.Count == 0)
                    return;

                // Sort by time offset (working copy to avoid mutating serialized data)
                var sortedEvents = new List<TimedEvent>(m_Events);
                sortedEvents.Sort((a, b) => a.TimeOffset.CompareTo(b.TimeOffset));

                float elapsed = 0f;

                for (int i = 0; i < sortedEvents.Count; i++)
                {
                    var timedEvent = sortedEvents[i];
                    float waitTime = timedEvent.TimeOffset - elapsed;

                    if (waitTime > 0f)
                    {
                        await Awaitable.WaitForSecondsAsync(waitTime, cancellationToken);
                        elapsed = timedEvent.TimeOffset;
                    }

                    m_Callbacks?.FireEvent(timedEvent.Event);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on Cancel or CancellationToken triggered.
            }
        }
    }
}
