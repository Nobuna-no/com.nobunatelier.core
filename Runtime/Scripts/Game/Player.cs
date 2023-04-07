using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    [RequireComponent(typeof(PlayerInput))]
    public class Player : GameModeParticipant
    {
        public PlayerInput PlayerInput
        {
            get
            {
                if (m_playerInput == null)
                {
                    m_playerInput = GetComponent<PlayerInput>();
                }

                return m_playerInput;
            }
        }

        private PlayerInput m_playerInput = null;
    }
}