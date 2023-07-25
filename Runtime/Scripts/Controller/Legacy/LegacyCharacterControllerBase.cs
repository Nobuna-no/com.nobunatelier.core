using UnityEngine;

namespace NobunAtelier
{
    public abstract class LegacyCharacterControllerBase : MonoBehaviour
    {
        public LegacyCharacterBase ControlledCharacter => m_controlledCharacter;
        public abstract bool IsAI { get; }

        [Header("ModuleOwner LegacyPlayerControllerBase")]
        [SerializeField]
        protected LegacyCharacterBase m_controlledCharacter;

        public virtual void ResetCharacter(Vector3 position, Quaternion rotation)
        {
            m_controlledCharacter?.ResetCharacter(position, rotation);
        }

        protected virtual void Start()
        {
            m_controlledCharacter?.Mount(this);
        }

        public virtual void SetCharacterMovementReference(LegacyCharacterBase character)
        {
            m_controlledCharacter = character;
            m_controlledCharacter?.Mount(this);
        }

        // Update is called once per frame
        private void Update()
        {
            ControllerUpdate();
        }

        private void FixedUpdate()
        {
            ControllerFixedUpdate();
        }

        protected virtual void ControllerUpdate()
        { }

        protected virtual void ControllerFixedUpdate()
        { }
    }
}