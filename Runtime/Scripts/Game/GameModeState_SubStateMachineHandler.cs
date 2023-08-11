using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    public class GameModeState_SubStateMachineHandler : BaseState<GameModeStateDefinition, GameModeStateCollection>
    {
        [SerializeField]
        private GameModeStateMachine m_subStateMachine;

        protected override void Awake()
        {
            base.Awake();
            if (m_subStateMachine == null)
            {
                CaptureSubStateMachine();
            }

            Debug.Assert(m_subStateMachine != null, $"{this.name}: Don't have valid child state machine.");
        }

        public override void Enter()
        {
            base.Enter();
            m_subStateMachine.StartFromScratch();
        }


        [Button(enabledMode: EButtonEnableMode.Editor)]
        private void CaptureSubStateMachine()
        {
            m_subStateMachine = GetComponentInChildren<GameModeStateMachine>();
        }
    }
}