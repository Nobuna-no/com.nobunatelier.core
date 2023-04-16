using UnityEngine;

namespace NobunAtelier
{
    public abstract class CharacterController : MonoBehaviour
    {
        public Character ControlledCharacter => m_controlledCharacter;
        public abstract bool IsAI { get; }

        [Header("ModuleOwner PlayerController")]
        [SerializeField]
        protected Character m_controlledCharacter;

        public virtual void ResetCharacter(Vector3 position, Quaternion rotation)
        {
            m_controlledCharacter?.ResetCharacter(position, rotation);
        }

        protected virtual void Awake()
        {
            Debug.Assert(m_controlledCharacter != null, $"[{Time.frameCount}] {this}: ModuleOwner is required");
        }

        protected virtual void Start()
        {
            m_controlledCharacter?.Mount(this);
        }

        public virtual void SetCharacterMovementReference(Character movement)
        {
            m_controlledCharacter = movement;
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