using NobunAtelier;
using System;
using System.Collections.Generic;
using UnityEngine;

// Disable "variable not used" warning.
#pragma warning disable 67

/// <summary>
/// Very much work in progress atm...
/// </summary>
[CreateAssetMenu]
public class WIP_AbilityChainDefinition : AbilityDefinition
{
    [Header("Ability Chain")]
    // let's ignore that for now and consider all as Sequential Manual
    [SerializeField] private ChainMode m_chainMode = ChainMode.SequentialAuto;
    [SerializeField] private AbilityDefinition[] m_chain;

    [Header("Debug")]
    [SerializeField] private bool m_debugLog;

    public ChainMode ChainsMode => m_chainMode;
    public IReadOnlyList<AbilityDefinition> Chain => m_chain;

    public override IAbilityInstance CreateAbilityInstance(AbilityController controller)
    {
        return new AbilityChainInstance(this, controller);
    }

    public enum ChainMode
    {
        // Play all the ability at the same time. (Not sure what that mean for the animation part...)
        Simultaneous,
        // Play ability one after another in a combo fashion.
        SequentialAuto,
        // Play one ability after another but based on player input.
        SequentialManual
    }

    public class AbilityChainInstance : DataInstance<WIP_AbilityChainDefinition>, IAbilityInstance
    {
        public event Action OnExecutionRequest;

        public event Action OnAbilityStartCharge;

        public event Action OnAbilityInitiated;

        public event Action OnAbilityChainOpportunity;

        public event Action OnAbilityCompleteExecution;

        public AbilityDefinition Ability => Data;
        public bool IsCharging { get; private set; }

        public IAbilityInstance.ExecutionState State
        {
            get => m_currentState;
            private set
            {
                m_previousState = m_currentState;
                m_currentState = value;
            }
        }
        private IAbilityInstance.ExecutionState m_previousState = IAbilityInstance.ExecutionState.Ready;
        private IAbilityInstance.ExecutionState m_currentState = IAbilityInstance.ExecutionState.Ready;
        private Dictionary<AbilityDefinition, IAbilityInstance> m_abilityMap;
        private int m_chainIndex = -1;

        public AbilityChainInstance(WIP_AbilityChainDefinition data, AbilityController controller)
            : base(data)
        {
            m_abilityMap = new Dictionary<AbilityDefinition, IAbilityInstance>();

            foreach (var d in data.m_chain)
            {
                m_abilityMap.Add(d, d.CreateAbilityInstance(controller));
            }
        }
        private int m_temporaryIndex = -1;
        public bool CanExecute()
        {
            bool result;
            switch (State)
            {
                // First ability of the sequence
                case IAbilityInstance.ExecutionState.Ready:
                    {
                        Log("UpdateExecutionState[Ready].");
                        m_temporaryIndex = 0;
                        result = m_abilityMap[Data.Chain[m_temporaryIndex]].CanExecute();
                        break;
                    }
                case IAbilityInstance.ExecutionState.ChainOpportunity:
                    {
                        Log("UpdateExecutionState[ChainOpportunity].");
                        // Finish state of the previous.
                        m_abilityMap[Data.Chain[m_chainIndex]].TerminateExecution();
                        m_temporaryIndex = (int)Mathf.Repeat(m_chainIndex + 1, Data.Chain.Count);
                        result = m_abilityMap[Data.Chain[m_temporaryIndex]].CanExecute();
                        break;
                    }
                // case IAbilityInstance.ExecutionState.Charging:
                //     {
                //         Log($"UpdateExecutionState[Charging] -> Release Charge for <b>{Data.Chain[m_chainIndex]}</b>.");
                //     }
                //     return false;

                default:
                    return false;
            }

            return result;
        }

        public void InitiateExecution()
        {
            State = IAbilityInstance.ExecutionState.InProgress;
            m_chainIndex = m_temporaryIndex;
            m_temporaryIndex = -1;

            m_abilityMap[Data.Chain[m_chainIndex]].InitiateExecution();
        }

        public void ExecuteEffect()
        {
            switch (Data.m_chainMode)
            {
                case ChainMode.SequentialAuto:
                    foreach (var d in m_abilityMap.Values)
                    {
                        OnExecutionRequest?.Invoke();
                        d.ExecuteEffect();
                    }
                    break;
                case ChainMode.SequentialManual:
                    break;
                case ChainMode.Simultaneous:
                    break;
            }
        }

        public void UpdateEffect(float deltaTime)
        {

        }

        public void StopEffect()
        {
            if (State == IAbilityInstance.ExecutionState.ChainOpportunity)
            {
                m_chainIndex = (int)Mathf.Repeat(m_chainIndex + 1, Data.Chain.Count);
            }
        }

        public void TerminateExecution()
        {
            foreach (var ability in m_abilityMap.Values)
            {
                // /!\
                // What does that mean in case we have a nested AbilityChain???
                // /!\
                ability.TerminateExecution();
            }

            Log("Stop");

            m_chainIndex = -1;
            State = IAbilityInstance.ExecutionState.Ready;
        }

        public void CancelExecution()
        {
            TerminateExecution();
        }

        public void StartCharge()
        {

        }

        public void ReleaseCharge()
        {

        }

        public void CancelCharge()
        {

        }

        private void Log(string message, bool isWarning = false)
        {
            if (!Data.m_debugLog)
            {
                return;
            }

            message = $"[{Time.frameCount}] <b>{Data}</b>: {message}" +
                $"\nState: {State};";// m_chainIndex: {m_chainIndex}" +
                                     // $"\nActive Ability: {m_activeAbility}; m_rootOfChargeAbility: {m_rootOfChargeAbility}" +
                                     // $"\nHasAlreadyBeenChainFromStartCharge: {m_hasAlreadyBeenChainFromStartCharge}";
            if (isWarning)
            {
                Debug.LogWarning(message, Data);
            }
            else
            {
                Debug.Log(message, Data);
            }
        }

        public void StopEffect(bool backgroundExecution = false)
        {
            StopEffect();
        }
    }
}

#pragma warning restore 67
