using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NaughtyAttributes;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Game Mode/Game Mode State: Timed Actions")]
    public class GameModeState_DelayedAction : BaseState<GameModeStateDefinition>
    {
        [SerializeField]
        private DelayedAction[] m_delayedActions;

        public override void Enter()
        {
            base.Enter();

            foreach (var act in m_delayedActions)
            {
                act.Init();
            }
        }

        public override void Tick(float deltaTime)
        {
            foreach (var act in m_delayedActions)
            {
                act.Tick(deltaTime);
            }

            base.Tick(deltaTime);
        }

        [System.Serializable]
        private class DelayedAction
        {
            [SerializeField]
            private float m_actionDelayInSeconds = 1f;
            [SerializeField]
            private bool m_loopAction = false;
            [SerializeField]
            private UnityEvent m_onAction;

            private float m_timeBeforeAction = -1f;

            public void Init()
            {
                m_timeBeforeAction = m_actionDelayInSeconds;
            }

            public void Tick(float deltaTime)
            {
                if (m_timeBeforeAction != -1f)
                {
                    m_timeBeforeAction -= deltaTime;
                    if (m_timeBeforeAction <= 0)
                    {
                        m_onAction?.Invoke();

                        if (m_loopAction)
                        {
                            m_timeBeforeAction = m_actionDelayInSeconds;
                        }
                        else
                        {
                            m_timeBeforeAction = -1f;
                        }
                    }
                }
            }
        }

    }
}
