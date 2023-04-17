using UnityEngine;

namespace NobunAtelier
{
    // Can either be a human or an IA - use by GameModes
    // Use cases: Players, AIs, Spectators, ...
    public class GameModeParticipant : MonoBehaviour
    {
        public LegacyCharacterControllerBase Controller { get; private set; }
        public LegacyCharacterBase CharacterMovement { get; private set; }

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