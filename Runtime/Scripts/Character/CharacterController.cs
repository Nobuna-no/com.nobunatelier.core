using UnityEngine;

namespace NobunAtelier
{
    public abstract class CharacterController : MonoBehaviour
    {
        public CharacterMovement CharacterMovement => m_characterMovement;
        public abstract bool IsAI { get; }

        [Header("Character Controller")]
        [SerializeField]
        protected CharacterMovement m_characterMovement;

        public virtual void ResetCharacter(Vector3 position, Quaternion rotation)
        {
            m_characterMovement?.ResetCharacter(position, rotation);
        }

        protected virtual void Awake()
        { }

        protected virtual void Start()
        {
            m_characterMovement?.Mount(this);
        }

        public virtual void SetCharacterMovementReference(CharacterMovement movement)
        {
            m_characterMovement = movement;
            m_characterMovement?.Mount(this);
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