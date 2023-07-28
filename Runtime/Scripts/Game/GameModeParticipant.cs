using UnityEngine;

namespace NobunAtelier
{
    // Can either be a human or an IA - use by GameModes
    // Use cases: Players, AIs, Spectators, ...
    public class GameModeParticipant : MonoBehaviour
    {
        [SerializeField]
        private LegacyCharacterControllerBase m_controllerPrefab;
        [SerializeField]
        private LegacyCharacterBase m_characterMovementPrefab;
        [SerializeField]
        private bool m_isAI = false;

        public LegacyCharacterControllerBase Controller { get; private set; }
        public LegacyCharacterBase CharacterMovement { get; private set; }
        public bool IsAI => m_isAI;

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