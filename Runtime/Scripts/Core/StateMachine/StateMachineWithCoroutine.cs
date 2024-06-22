using System.Collections;
using UnityEngine;

namespace NobunAtelier
{
    public class StateMachineWithCoroutine<T, TCollection> : StateMachineComponent<T, TCollection>
        where T : StateDefinition
        where TCollection : DataCollection
    {
        private float m_updatePerSeconds = 60;
        private bool m_isRoutineRunning = false;

        public override void Enter()
        {
            base.Enter();

            if (m_isRoutineRunning)
            {
                return;
            }

            StartCoroutine(UpdateRoutine());
            m_isRoutineRunning = true;
        }

        public override void Exit()
        {
            StopCoroutine(UpdateRoutine());
            m_isRoutineRunning = false;
            base.Exit();
        }

        private void OnDestroy()
        {
            StopCoroutine(UpdateRoutine());
        }

        private IEnumerator UpdateRoutine()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1f / m_updatePerSeconds);

                if (!IsPaused)
                {
                    Tick(1f / m_updatePerSeconds);
                }
            }
        }
    }
}