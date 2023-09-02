using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public abstract class StateDefinition : DataDefinition
    {
        public StateDefinition RequiredPriorState => m_requiredPriorState;

        public string Description => m_description;

        [SerializeField, TextArea]
        private string m_description;

        [SerializeField]
        private StateDefinition m_requiredPriorState;

#if UNITY_EDITOR
        public void Editor_SetDescription(string description)
        {
            m_description = description;
        }
#endif
    }
}

