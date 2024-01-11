using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    // The class is used to handle player input and interactions within the game mode.
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputParticipant : GameModeParticipant
    {
        public override bool IsAI => false;

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