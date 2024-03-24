using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public class FactoryProductWithEvent : FactoryProduct
    {
        [SerializeField] private UnityEvent m_onCreation;
        [SerializeField] private UnityEvent m_onActivation;
        [SerializeField] private UnityEvent m_onDeactivation;

        protected override void OnProductReset()
        {
            m_onCreation?.Invoke();
        }

        protected override void OnProductActivation()
        {
            m_onActivation?.Invoke();
        }

        protected override void OnProductDeactivation()
        {
            m_onDeactivation?.Invoke();
        }
    }
}
