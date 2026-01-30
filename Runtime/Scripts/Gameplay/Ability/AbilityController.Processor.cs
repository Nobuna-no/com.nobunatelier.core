using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public partial class AbilityController
    {
        /// <summary>
        /// I've made the Instance processing in this subclass just for sake of splitting
        /// responsibility and have more mental bandwidth when designing surrounding logic.
        /// Can probably be merge back in AbilityController...
        /// </summary>
        private class Processor
        {
            private AbilityController m_controller;
            private IAbilityInstance m_activeAbilityInstance;

            ~Processor()
            {
                if (m_activeAbilityInstance != null)
                {
                    m_activeAbilityInstance.OnAbilityStartCharge -= OnAbilityStartCharge;
                    m_activeAbilityInstance.OnAbilityInitiated -= OnAbilitySetup;
                    m_activeAbilityInstance.OnAbilityChainOpportunity -= OnAbilityChainOpportunity;
                    m_activeAbilityInstance.OnAbilityCompleteExecution -= OnAbilityEnd;
                }
            }

            public void Initialize(AbilityController controller, AbilityDefinition activeAbility)
            {
                m_controller = controller;

                if (activeAbility == null || (m_activeAbilityInstance != null && m_activeAbilityInstance.Ability == activeAbility))
                {
                    return;
                }

                m_activeAbilityInstance = activeAbility.CreateAbilityInstance(controller);

                m_activeAbilityInstance.OnAbilityStartCharge += OnAbilityStartCharge;
                m_activeAbilityInstance.OnAbilityInitiated += OnAbilitySetup;
                m_activeAbilityInstance.OnAbilityChainOpportunity += OnAbilityChainOpportunity;
                m_activeAbilityInstance.OnAbilityCompleteExecution += OnAbilityEnd;
            }

            private void OnAbilityStartCharge()
            {
                m_controller.OnAbilitySetup();
                m_controller?.OnAbilityStartCharge?.Invoke();
            }

            private void OnAbilitySetup()
            {
                m_controller.OnAbilitySetup();
                m_controller.OnAbilityStartExecution?.Invoke();
            }

            private void OnAbilityChainOpportunity()
            {
                m_controller?.OnAbilityChainOpportunity?.Invoke();
            }

            private void OnAbilityEnd()
            {
                m_controller?.OnAbilityCompleteExecution?.Invoke();
            }

            public bool CanExecute()
            {
                return m_activeAbilityInstance.CanExecute();
            }


            public void Execute()
            {
                m_activeAbilityInstance.InitiateExecution();
            }

            public void PlayAbilityModules()
            {
                m_activeAbilityInstance.ExecuteEffect();
            }

            public void Update(float deltaTime)
            {
                m_activeAbilityInstance?.UpdateEffect(deltaTime);
            }

            public void StopAbilityModules()
            {
                m_activeAbilityInstance.StopEffect();
            }

            public void Terminate()
            {
                m_activeAbilityInstance.TerminateExecution();
            }

            public void StartCharge()
            {
                m_activeAbilityInstance.StartCharge();
            }

            public void ReleaseCharge()
            {
                m_activeAbilityInstance.ReleaseCharge();
            }

            public void CancelCharge()
            {
                m_activeAbilityInstance.CancelCharge();
            }
        }
    }
}
