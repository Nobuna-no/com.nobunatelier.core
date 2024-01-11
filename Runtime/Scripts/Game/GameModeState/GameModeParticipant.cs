using UnityEngine;

namespace NobunAtelier
{
    // Represents a participant in a game mode, which can either be a human player or an AI.
    // The class is used by GameModes to instantiate the character and the controller
    // affiliated to this participant.
    // Use cases: Players, AIs or Spectators.
    public abstract class GameModeParticipant : MonoBehaviour
    {
        [SerializeField]
        private LegacyCharacterControllerBase m_controllerPrefab;

        [SerializeField]
        private LegacyCharacterBase m_characterMovementPrefab;

        public LegacyCharacterControllerBase Controller { get; private set; }
        public LegacyCharacterBase CharacterMovement { get; private set; }
        public abstract bool IsAI { get; }

        protected virtual void Awake()
        {
            if (m_characterMovementPrefab != null)
            {
                InstantiateCharacter(m_characterMovementPrefab);
            }
            if (m_controllerPrefab != null)
            {
                InstantiateController(m_controllerPrefab);
            }
        }

        public virtual void InstantiateCharacter(LegacyCharacterBase characterPrefab)
        {
            this.CharacterMovement = null;
            if (!characterPrefab)
            {
                return;
            }

            this.CharacterMovement = Instantiate(characterPrefab, gameObject.transform);
        }

        public virtual void InstantiateController(LegacyCharacterControllerBase controllerPrefab)
        {
            this.Controller = null;
            if (!controllerPrefab)
            {
                return;
            }

            this.Controller = Instantiate(controllerPrefab, gameObject.transform);
            Controller.SetCharacterMovementReference(this.CharacterMovement);
        }
    }
}