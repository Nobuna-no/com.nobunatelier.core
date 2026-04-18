using System;
using System.Threading;
using UnityEngine;

namespace NobunAtelier
{
    public sealed class AwaitableExecutionDriver : IAbilityExecutionDriver
    {
        private IAbilityExecutionDriverCallbacks m_Callbacks;
        private float m_ExecutionDelay;
        private float m_UpdateDuration;
        private float m_RecoveryDuration;
        private CancellationTokenSource m_CancellationTokenSource;
        private CancellationToken m_ExternalToken;

        public void Configure(float executionDelay, float updateDuration, float recoveryDuration)
        {
            m_ExecutionDelay = executionDelay;
            m_UpdateDuration = updateDuration;
            m_RecoveryDuration = recoveryDuration;
        }

        public void Initialize(in AbilityExecutionDriverContext context)
        {
            m_Callbacks = context.Callbacks;
            m_ExternalToken = context.Token;
        }

        public void RequestExecution()
        {
            RestartExecution();
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

        private void RestartExecution()
        {
            CancelInternal();
            m_CancellationTokenSource = m_ExternalToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(m_ExternalToken)
                : new CancellationTokenSource();
            ExecuteAsync(m_CancellationTokenSource.Token).FireAndForget();
        }

        private void CancelInternal()
        {
            if (m_CancellationTokenSource == null)
            {
                return;
            }

            m_CancellationTokenSource.Cancel();
            m_CancellationTokenSource.Dispose();
            m_CancellationTokenSource = null;
        }

        private async Awaitable ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (m_ExecutionDelay > 0f)
                {
                    await Awaitable.WaitForSecondsAsync(m_ExecutionDelay, cancellationToken);
                }

                m_Callbacks?.OnEffectStart();

                if (m_UpdateDuration > 0f)
                {
                    await Awaitable.WaitForSecondsAsync(m_UpdateDuration, cancellationToken);
                }

                m_Callbacks?.OnEffectStop();

                if (m_RecoveryDuration > 0f)
                {
                    await Awaitable.WaitForSecondsAsync(m_RecoveryDuration, cancellationToken);
                }

                m_Callbacks?.OnExecutionComplete();
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
