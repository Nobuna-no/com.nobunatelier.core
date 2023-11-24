using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Modules/StateModule: Scheduled Actions")]
    public class StateModule_ScheduledActions : StateComponentModule
    {
        [SerializeField]
        private TimedAction[] m_actions;

        public override void Enter()
        {
            base.Enter();

            foreach (var act in m_actions)
            {
                act.Init();
            }
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            foreach (var act in m_actions)
            {
                act.Tick(deltaTime);
            }
        }

        [System.Serializable]
        private class TimedAction
        {
            [SerializeField, MinMaxSlider(0, 10)]
            private Vector2 m_actionDelayInSeconds = new Vector2(0, 1);

            [SerializeField]
            private bool m_loopAction = false;

            [SerializeField]
            private UnityEvent m_onAction;

            private float m_timeBeforeAction = -1f;

            public void Init()
            {
                m_timeBeforeAction = Random.Range(m_actionDelayInSeconds.x, m_actionDelayInSeconds.y);
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
                            m_timeBeforeAction = Random.Range(m_actionDelayInSeconds.x, m_actionDelayInSeconds.y);
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