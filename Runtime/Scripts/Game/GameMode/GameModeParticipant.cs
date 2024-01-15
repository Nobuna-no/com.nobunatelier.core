using UnityEditor;
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
        private CharacterControllerBase m_controllerReference;

        [SerializeField]
        private Character m_characterReference;

        [SerializeField]
        private bool m_useReferenceAsPrefabToInstantiate = false;

        public CharacterControllerBase Controller { get; private set; }
        public Character Character { get; private set; }
        public abstract bool IsAI { get; }

        protected virtual void Awake()
        {
            if (!m_useReferenceAsPrefabToInstantiate)
            {
                return;
            }

            if (m_characterReference != null)
            {
                InstantiateCharacter(m_characterReference);
            }
            if (m_controllerReference != null)
            {
                InstantiateController(m_controllerReference);
            }
        }

        public virtual void InstantiateCharacter(Character characterPrefab)
        {
            this.Character = null;
            if (!characterPrefab)
            {
                return;
            }

            this.Character = Instantiate(characterPrefab, gameObject.transform);
        }

        public virtual void InstantiateController(CharacterControllerBase controllerPrefab)
        {
            this.Controller = null;
            if (!controllerPrefab)
            {
                return;
            }

            this.Controller = Instantiate(controllerPrefab, gameObject.transform);
            Controller.SetCharacterReference(this.Character);
        }

        public virtual void EnableInput()
        {
            Controller.EnableInput();
        }

        public virtual void DisableInput()
        {
            Controller.DisableInput();
        }
    }
}