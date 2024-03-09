using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public class PoolableWithEvent : PoolableBehaviour
    {
        [SerializeField] private UnityEvent m_onCreation;
        [SerializeField] private UnityEvent m_onActivation;
        [SerializeField] private UnityEvent m_onDeactivation;

        protected override void OnCreation()
        {
            m_onCreation?.Invoke();
        }

        protected override void OnActivation()
        {
            m_onActivation?.Invoke();
        }

        protected override void OnDeactivation()
        {
            m_onDeactivation?.Invoke();
        }
    }
}
