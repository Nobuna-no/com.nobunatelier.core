using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    public class AIPlayer : GameModeParticipant
    {
        private LegacyAIControllerBase m_aiController;
        public LegacyAIControllerBase AIController
        {
            get
            {
                if (m_aiController == null)
                {
                    m_aiController = Controller as LegacyAIControllerBase;
                }

                return m_aiController;
            }
        }
    }
}