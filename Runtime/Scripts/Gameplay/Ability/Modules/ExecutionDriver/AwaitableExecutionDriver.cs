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
        private float m_ChainOpportunityDuration;
        private CancellationTokenSource m_CancellationTokenSource;

        public void ConfigureFromActionModel(ModularAbilityDefinition.ActionModel actionModel)
        {
            if (actionModel == null)
            {
                m_ExecutionDelay = 0f;
                m_UpdateDuration = 0f;
                m_ChainOpportunityDuration = 0f;
                return;
            }

            m_ExecutionDelay = actionModel.ExecutionDelay;
            m_UpdateDuration = actionModel.UpdateDuration;
            m_ChainOpportunityDuration = actionModel.ChainOpportunityDuration;
        }

        public void Initialize(in AbilityExecutionDriverContext context)
        {
            m_Callbacks = context.Callbacks;
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
            m_CancellationTokenSource = new CancellationTokenSource();
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

                if (m_ChainOpportunityDuration > 0f)
                {
                    await Awaitable.WaitForSecondsAsync(m_ChainOpportunityDuration, cancellationToken);
                }

                m_Callbacks?.OnExecutionComplete();
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
