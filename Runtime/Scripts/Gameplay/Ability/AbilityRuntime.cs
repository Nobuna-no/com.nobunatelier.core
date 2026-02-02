using System;
using System.Collections.Generic;
using Physarida;

namespace NobunAtelier
{
    internal class AbilityRuntime
    {
        private AbilityController m_Controller;
        private AbilityDefinition m_DefaultAbility;
        private Queue<Action> m_ActionQueue = new Queue<Action>();
        private Processor m_AbilityProcessor;
        private Processor m_AbilityProcessorOverride;
        private bool m_CanExecuteNewAction = true;
        private IAbilityChainPolicy m_ChainPolicy; // Chain ability for combo.

        public bool CanQueueNewAction => m_CanExecuteNewAction;

        public void Initialize(AbilityController controller, AbilityDefinition defaultAbility)
        {
            m_Controller = controller;
            SetAbility(defaultAbility);
            EnsureChainPolicy();
        }

        public void SetAbility(AbilityDefinition ability)
        {
            m_DefaultAbility = ability;
            GetProcessorAndInitializeIfNeeded();
        }

        public void Update(float deltaTime)
        {
            var activeProcessor = GetProcessorAndInitializeIfNeeded();
            activeProcessor.Update(deltaTime);

            if (!m_CanExecuteNewAction || m_ActionQueue.Count == 0)
            {
                return;
            }

            m_Controller.Log.Record($"Dequeue next ability");
            m_ActionQueue.Dequeue().Invoke();
            m_CanExecuteNewAction = false;
        }

        public void QueueInitiateAbilityExecution()
        {
            m_Controller.Log.Record();

            if (!m_CanExecuteNewAction)
            {
                EnsureChainPolicy();
                m_ChainPolicy.BufferAbilityRequest(InitiateAbilityExecution);
                return;
            }

            m_ActionQueue.Enqueue(InitiateAbilityExecution);
        }

        public void StopAbility()
        {
            var activeProcessor = GetProcessorAndInitializeIfNeeded();

            if (activeProcessor != null)
            {
                activeProcessor.Terminate();
            }

            if (m_AbilityProcessorOverride != null)
            {
                m_AbilityProcessorOverride = null;
            }

            CloseChainOpportunity();
        }

        public void CancelAbility()
        {
            var activeProcessor = GetProcessorAndInitializeIfNeeded();

            if (activeProcessor != null)
            {
                activeProcessor.Cancel();
            }

            if (m_AbilityProcessorOverride != null)
            {
                m_AbilityProcessorOverride = null;
            }

            CloseChainOpportunity();
        }

        public void StartCharge()
        {
            var activeProcessor = GetProcessorAndInitializeIfNeeded();
            activeProcessor.StartCharge();
        }

        public void ReleaseCharge()
        {
            var activeProcessor = GetProcessorAndInitializeIfNeeded();
            activeProcessor.ReleaseCharge();
        }

        public void CancelCharge()
        {
            var activeProcessor = GetProcessorAndInitializeIfNeeded();
            activeProcessor.CancelCharge();
        }

        public void Dispose()
        {
            if (m_AbilityProcessor != null)
            {
                m_AbilityProcessor.Terminate();
                m_AbilityProcessor.Dispose();
                m_AbilityProcessor = null;
            }

            if (m_AbilityProcessorOverride != null)
            {
                m_AbilityProcessorOverride.Terminate();
                m_AbilityProcessorOverride.Dispose();
                m_AbilityProcessorOverride = null;
            }

            if (m_ActionQueue.Count > 0)
            {
                m_ActionQueue.Clear();
            }

            m_CanExecuteNewAction = true;
            CloseChainOpportunity();
            m_ChainPolicy?.Dispose();
            m_ChainPolicy = null;
            m_Controller = null;
            m_DefaultAbility = null;
        }

        private void InitiateAbilityExecution()
        {
            var activeProcessor = GetProcessorAndInitializeIfNeeded();

            if (!activeProcessor.CanExecute())
            {
                return;
            }

            activeProcessor.Execute();
            m_CanExecuteNewAction = false;
        }

        private Processor GetProcessorAndInitializeIfNeeded()
        {
            if (m_AbilityProcessor == null)
            {
                m_AbilityProcessor = new Processor(this);
            }

            m_AbilityProcessor.Initialize(m_Controller, m_DefaultAbility);
            return m_AbilityProcessorOverride != null ? m_AbilityProcessorOverride : m_AbilityProcessor;
        }

        private void OpenChainOpportunity()
        {
            EnsureChainPolicy();
            m_ChainPolicy.OpenChainOpportunity();
        }

        private void CloseChainOpportunity()
        {
            m_ChainPolicy?.CloseChainOpportunity();
        }

        private void EnsureChainPolicy()
        {
            if (m_ChainPolicy != null || m_Controller == null)
            {
                return;
            }

            m_ChainPolicy = new TimedRequestChainPolicy();
            m_ChainPolicy.Initialize(this, m_Controller);
        }

        private class Processor
        {
            private AbilityRuntime m_Runtime;
            private AbilityController m_Controller;
            private IAbilityInstance m_ActiveAbilityInstance;

            public Processor(AbilityRuntime runtime)
            {
                m_Runtime = runtime;
            }

            public void Initialize(AbilityController controller, AbilityDefinition activeAbility)
            {
                m_Controller = controller;

                if (activeAbility == null || (m_ActiveAbilityInstance != null && m_ActiveAbilityInstance.Ability == activeAbility))
                {
                    return;
                }

                m_ActiveAbilityInstance = activeAbility.CreateAbilityInstance(controller);

                // Bridge between data layer to scene-component layer.
                m_ActiveAbilityInstance.OnAbilityStartCharge += OnAbilityStartCharge;
                m_ActiveAbilityInstance.OnAbilityInitiated += OnAbilitySetup;
                m_ActiveAbilityInstance.OnAbilityChainOpportunity += OnAbilityChainOpportunity;
                m_ActiveAbilityInstance.OnAbilityCompleteExecution += OnAbilityEnd;
            }

            private void OnAbilityStartCharge()
            {
                m_Controller?.OnAbilityStartCharge?.Invoke();
            }

            private void OnAbilitySetup()
            {
                m_Controller.OnAbilityStartExecution?.Invoke();
            }

            private void OnAbilityChainOpportunity()
            {
                if (m_Runtime != null)
                {
                    m_Runtime.m_CanExecuteNewAction = true;
                }

                m_Controller?.OnAbilityChainOpportunity?.Invoke();
                m_Runtime?.OpenChainOpportunity();
            }

            private void OnAbilityEnd()
            {
                if (m_Runtime != null)
                {
                    m_Runtime.m_CanExecuteNewAction = true;
                }

                m_Controller?.OnAbilityCompleteExecution?.Invoke();
                m_Runtime?.CloseChainOpportunity();
            }

            public bool CanExecute()
            {
                return m_ActiveAbilityInstance != null && m_ActiveAbilityInstance.CanExecute();
            }

            public void Execute()
            {
                m_ActiveAbilityInstance?.InitiateExecution();
            }

            public void Update(float deltaTime)
            {
                m_ActiveAbilityInstance?.UpdateEffect(deltaTime);
            }

            public void Terminate()
            {
                m_ActiveAbilityInstance?.TerminateExecution();
            }

            public void Cancel()
            {
                m_ActiveAbilityInstance?.CancelExecution();
            }

            public void StartCharge()
            {
                m_ActiveAbilityInstance?.StartCharge();
            }

            public void ReleaseCharge()
            {
                m_ActiveAbilityInstance?.ReleaseCharge();
            }

            public void CancelCharge()
            {
                m_ActiveAbilityInstance?.CancelCharge();
            }

            public void Dispose()
            {
                if (m_ActiveAbilityInstance != null)
                {
                    m_ActiveAbilityInstance.OnAbilityStartCharge -= OnAbilityStartCharge;
                    m_ActiveAbilityInstance.OnAbilityInitiated -= OnAbilitySetup;
                    m_ActiveAbilityInstance.OnAbilityChainOpportunity -= OnAbilityChainOpportunity;
                    m_ActiveAbilityInstance.OnAbilityCompleteExecution -= OnAbilityEnd;

                    if (m_ActiveAbilityInstance is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    m_ActiveAbilityInstance = null;
                }

                m_Runtime = null;
                m_Controller = null;
            }
        }

        private interface IAbilityChainPolicy : IDisposable
        {
            void Initialize(AbilityRuntime runtime, AbilityController controller);
            void BufferAbilityRequest(Action action);
            void OpenChainOpportunity();
            void CloseChainOpportunity();
        }

        private class TimedRequestChainPolicy : IAbilityChainPolicy
        {
            private AbilityController m_Controller;
            private TimedRequestBuffer m_Buffer;
            private TimedRequestHandle m_RequestHandle;
            private Action m_OnConsumeAction;
            private bool m_IsChainOpportunityOpen;

            public void Initialize(AbilityRuntime runtime, AbilityController controller)
            {
                m_Controller = controller;
            }

            public void BufferAbilityRequest(Action action)
            {
                if (action == null)
                {
                    return;
                }

                m_OnConsumeAction = action;
                EnsureBuffer();
                m_RequestHandle?.Request();
            }

            public void OpenChainOpportunity()
            {
                m_IsChainOpportunityOpen = true;
                EnsureBuffer();
            }

            public void CloseChainOpportunity()
            {
                m_IsChainOpportunityOpen = false;
                m_RequestHandle?.Clear();
            }

            public void Dispose()
            {
                if (m_Buffer != null)
                {
                    TimedRequestBufferFactory.Release(m_Buffer);
                }

                m_Buffer = null;
                m_RequestHandle = null;
                m_OnConsumeAction = null;
                m_Controller = null;
            }

            private void EnsureBuffer()
            {
                if (m_Controller == null)
                {
                    return;
                }

                if (m_Buffer == null)
                {
                    m_Buffer = TimedRequestBufferFactory.Get(m_Controller, $"{m_Controller.name}-AbilityBuffer");
                    m_RequestHandle = m_Buffer.Register()
                        .WithBufferDuration(m_Controller.InputBufferDuration)
                        .UseUnscaledTime(m_Controller.InputBufferUseUnscaledTime)
                        .When(() => m_IsChainOpportunityOpen)
                        .OnConsume(HandleConsume)
                        .OwnedBy(m_Controller)
                        .WithDebugLabel("AbilityChain")
                        .Build();
                    return;
                }

                m_RequestHandle?.UpdateBufferDuration(m_Controller.InputBufferDuration);
            }

            private void HandleConsume()
            {
                m_OnConsumeAction?.Invoke();
            }
        }
    }
}
