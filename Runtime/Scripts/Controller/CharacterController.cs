using UnityEngine;

namespace NobunAtelier
{
    public abstract class CharacterController : MonoBehaviour
    {
        public Character CharacterMovement => m_characterMovement;
        public abstract bool IsAI { get; }

        [Header("ModuleOwner PlayerController")]
        [SerializeField]
        protected Character m_characterMovement;

        public virtual void ResetCharacter(Vector3 position, Quaternion rotation)
        {
            m_characterMovement?.ResetCharacter(position, rotation);
        }

        protected virtual void Awake()
        {
            Debug.Assert(m_characterMovement != null, $"[{Time.frameCount}] {this}: ModuleOwner is required");
        }

        protected virtual void Start()
        {
            m_characterMovement?.Mount(this);
        }

        public virtual void SetCharacterMovementReference(Character movement)
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