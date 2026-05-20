using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Time-driven ability action driver with explicit timing phases.
    /// Signals <see cref="AbilityPhase"/> transitions at phase boundaries.
    /// Content events fire at specified offsets during the full execution.
    /// </summary>
    [Serializable]
    public class AwaitableDrivenAbilityAction : IAbilityActionDriver
    {
        [Header("Timing")]
        [Tooltip("Duration of startup phase before Active (seconds).")]
        [Min(0f)]
        [SerializeField] private float m_StartupDuration;

        [Tooltip("Duration of active phase (seconds).")]
        [Min(0f)]
        [SerializeField] private float m_ActiveDuration = 0.5f;

        [Tooltip("Duration of recovery phase before Complete (seconds).")]
        [Min(0f)]
        [SerializeField] private float m_RecoveryDuration = 0.3f;

        [Header("Gameplay")]
        [Tooltip("Gameplay events fired during execution. Time offsets are absolute from execution start.")]
        [SerializeField] private List<TimedGameplayEvent> m_GameplayEvents;

        [Serializable]
        public class TimedGameplayEvent
        {
            [Tooltip("The GameplayEvent to fire.")]
            [SerializeField] private GameplayEventDefinition m_Event;

            [Tooltip("Time offset in seconds from execution start.")]
            [Min(0f)]
            [SerializeField] private float m_TimeOffset;

            public GameplayEventDefinition Event => m_Event;
            public float TimeOffset => m_TimeOffset;
        }

        private IAbilityActionDriverCallbacks m_Callbacks;
        private CancellationTokenSource m_Cts;
        private CancellationToken m_ExternalToken;

        public GameplayEventDefinition[] GetAvailableEvents()
        {
            if (m_GameplayEvents == null || m_GameplayEvents.Count == 0)
                return Array.Empty<GameplayEventDefinition>();

            var events = new GameplayEventDefinition[m_GameplayEvents.Count];
            for (int i = 0; i < m_GameplayEvents.Count; i++)
            {
                events[i] = m_GameplayEvents[i].Event;
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
                float elapsed = 0f;

                // Sort content events by time offset
                List<TimedGameplayEvent> sortedContent = null;
                if (m_GameplayEvents != null && m_GameplayEvents.Count > 0)
                {
                    sortedContent = new List<TimedGameplayEvent>(m_GameplayEvents);
                    sortedContent.Sort((a, b) => a.TimeOffset.CompareTo(b.TimeOffset));
                }
                int nextContentIndex = 0;

                // Startup phase
                if (m_StartupDuration > 0f)
                {
                    (elapsed, nextContentIndex) = await FireContentEventsUntil(
                        sortedContent, nextContentIndex, elapsed, m_StartupDuration, cancellationToken);
                }

                // -> Active
                m_Callbacks?.OnPhaseTransition(AbilityPhase.Active);

                // Active phase
                float activeEnd = m_StartupDuration + m_ActiveDuration;
                if (m_ActiveDuration > 0f)
                {
                    (elapsed, nextContentIndex) = await FireContentEventsUntil(
                        sortedContent, nextContentIndex, elapsed, activeEnd, cancellationToken);
                }

                // -> Recovery
                m_Callbacks?.OnPhaseTransition(AbilityPhase.Recovery);

                // Recovery phase
                float recoveryEnd = activeEnd + m_RecoveryDuration;
                if (m_RecoveryDuration > 0f)
                {
                    (elapsed, nextContentIndex) = await FireContentEventsUntil(
                        sortedContent, nextContentIndex, elapsed, recoveryEnd, cancellationToken);
                }

                // Fire remaining content events
                if (sortedContent != null)
                {
                    while (nextContentIndex < sortedContent.Count)
                    {
                        m_Callbacks?.FireEvent(sortedContent[nextContentIndex].Event);
                        nextContentIndex++;
                    }
                }

                // -> Complete
                m_Callbacks?.OnPhaseTransition(AbilityPhase.Complete);
            }
            catch (OperationCanceledException)
            {
                // Expected on Cancel or CancellationToken triggered.
            }
        }

        private async Awaitable<(float elapsed, int nextIndex)> FireContentEventsUntil(
            List<TimedGameplayEvent> sortedContent, int nextIndex,
            float elapsed, float targetTime, CancellationToken cancellationToken)
        {
            if (sortedContent != null)
            {
                while (nextIndex < sortedContent.Count && sortedContent[nextIndex].TimeOffset <= targetTime)
                {
                    float waitTime = sortedContent[nextIndex].TimeOffset - elapsed;
                    if (waitTime > 0f)
                    {
                        await Awaitable.WaitForSecondsAsync(waitTime, cancellationToken);
                        elapsed = sortedContent[nextIndex].TimeOffset;
                    }
                    m_Callbacks?.FireEvent(sortedContent[nextIndex].Event);
                    nextIndex++;
                }
            }

            float remainingWait = targetTime - elapsed;
            if (remainingWait > 0f)
            {
                await Awaitable.WaitForSecondsAsync(remainingWait, cancellationToken);
            }
            return (targetTime, nextIndex);
        }
    }
}
