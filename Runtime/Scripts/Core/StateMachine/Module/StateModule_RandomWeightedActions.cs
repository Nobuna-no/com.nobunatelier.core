using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public enum RandomWeightedActionTriggerMode
    {
        Manual = 0,
        Delay = 1,
        RangeDelay = 2,
        OnStateEnter = 3,
    }

    /// <summary>
    /// Picks one weighted <see cref="UnityEvent"/> from a pool when triggered.
    /// Supports manual fire, fixed delay, random delay range (like <see cref="StateModule_ScheduledActions"/>), or fire on state enter.
    /// </summary>
    [AddComponentMenu("NobunAtelier/States/Modules/StateModule: Random Weighted Actions")]
    public class StateModule_RandomWeightedActions : StateComponentModule
    {
        [SerializeField]
        private RandomWeightedActionTriggerMode m_TriggerMode = RandomWeightedActionTriggerMode.OnStateEnter;

        [SerializeField, ShowIf("UsesFixedDelay"), Min(0f)]
        private float m_DelaySeconds = 1f;

        [SerializeField, ShowIf("UsesRangeDelay"), MinMaxSlider(0f, 60f)]
        private Vector2 m_DelayRangeSeconds = new Vector2(0f, 1f);

        [SerializeField, ShowIf("UsesTimedTrigger")]
        private bool m_LoopTimedTrigger;

        [SerializeField]
        private WeightedAction[] m_Actions;

        private float m_TimeUntilTrigger = -1f;

#if UNITY_EDITOR
        private bool UsesFixedDelay => m_TriggerMode == RandomWeightedActionTriggerMode.Delay;

        private bool UsesRangeDelay => m_TriggerMode == RandomWeightedActionTriggerMode.RangeDelay;

        private bool UsesTimedTrigger =>
            m_TriggerMode == RandomWeightedActionTriggerMode.Delay
            || m_TriggerMode == RandomWeightedActionTriggerMode.RangeDelay;
#endif

        public override void Enter()
        {
            base.Enter();

            switch (m_TriggerMode)
            {
                case RandomWeightedActionTriggerMode.OnStateEnter:
                    TriggerRandomAction();
                    break;
                case RandomWeightedActionTriggerMode.Delay:
                case RandomWeightedActionTriggerMode.RangeDelay:
                    ScheduleNextTimedTrigger();
                    break;
            }
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            if (m_TimeUntilTrigger < 0f)
            {
                return;
            }

            m_TimeUntilTrigger -= deltaTime;
            if (m_TimeUntilTrigger > 0f)
            {
                return;
            }

            TriggerRandomAction();

            if (m_LoopTimedTrigger && UsesTimedTriggerRuntime())
            {
                ScheduleNextTimedTrigger();
            }
            else
            {
                m_TimeUntilTrigger = -1f;
            }
        }

        public override void Exit()
        {
            m_TimeUntilTrigger = -1f;
            base.Exit();
        }

        /// <summary>
        /// Picks and invokes one weighted action. Use for <see cref="RandomWeightedActionTriggerMode.Manual"/> or UnityEvent wiring.
        /// </summary>
        public void TriggerRandomAction()
        {
            if (m_Actions == null || m_Actions.Length == 0)
            {
                return;
            }

            float totalWeight = 0f;
            for (int i = 0; i < m_Actions.Length; i++)
            {
                var entry = m_Actions[i];
                if (entry != null && entry.Weight > 0f)
                {
                    totalWeight += entry.Weight;
                }
            }

            if (totalWeight <= 0f)
            {
                Debug.LogWarning(
                    $"[{nameof(StateModule_RandomWeightedActions)}] No positive weights on {gameObject.name}",
                    this);
                return;
            }

            float pick = Random.value * totalWeight;
            float cumulative = 0f;
            for (int i = 0; i < m_Actions.Length; i++)
            {
                var entry = m_Actions[i];
                if (entry == null || entry.Weight <= 0f)
                {
                    continue;
                }

                cumulative += entry.Weight;
                if (pick <= cumulative)
                {
                    entry.OnSelected?.Invoke();
                    return;
                }
            }
        }

        private void ScheduleNextTimedTrigger()
        {
            m_TimeUntilTrigger = m_TriggerMode switch
            {
                RandomWeightedActionTriggerMode.Delay => m_DelaySeconds,
                RandomWeightedActionTriggerMode.RangeDelay => Random.Range(
                    m_DelayRangeSeconds.x,
                    m_DelayRangeSeconds.y),
                _ => -1f,
            };
        }

        private bool UsesTimedTriggerRuntime()
        {
            return m_TriggerMode == RandomWeightedActionTriggerMode.Delay
                || m_TriggerMode == RandomWeightedActionTriggerMode.RangeDelay;
        }

        [System.Serializable]
        private class WeightedAction
        {
            [SerializeField, Min(0f)]
            private float m_Weight = 1f;

            [SerializeField]
            private UnityEvent m_OnSelected;

            public float Weight => m_Weight;

            public UnityEvent OnSelected => m_OnSelected;
        }
    }
}
