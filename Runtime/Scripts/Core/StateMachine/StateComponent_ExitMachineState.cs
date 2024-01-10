using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    public class StateComponent_ExitMachineState<T, TCollection> : StateComponent<T, TCollection>
        where T : NobunAtelier.StateDefinition
        where TCollection : DataCollection
    {
        private enum ExitStateMachineTrigger
        {
            Manual,
            AfterDelay,
            OnStateEnter,
        }

        [Header("State Machine Exit")]
        [SerializeField]
        private ExitStateMachineTrigger m_trigger = ExitStateMachineTrigger.OnStateEnter;
        
        [SerializeField, ShowIf("IsDelayMode"), Min(0)]
        private float m_delayInSeconds = 1f;

        private float m_currentDelay = 0f;

        private bool IsDelayMode => m_trigger == ExitStateMachineTrigger.AfterDelay;

        protected override void Awake()
        {
            base.Awake();
            enabled = false;
        }

        public override void Enter()
        {
            base.Enter();
            if (m_trigger == ExitStateMachineTrigger.OnStateEnter)
            {
                ParentStateMachine.ExitStateMachine();
            }

            if (m_trigger == ExitStateMachineTrigger.AfterDelay)
            {
                m_currentDelay = m_delayInSeconds;
                enabled = true;
            }
        }

        public void ExitStateMachine()
        {
            ParentStateMachine.ExitStateMachine();
        }

        private void Update()
        {
            m_currentDelay -= Time.deltaTime;
            if (m_currentDelay <= 0)
            {
                ParentStateMachine.ExitStateMachine();
                enabled = false;
            }
        }
    }
}