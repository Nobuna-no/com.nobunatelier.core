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

        protected virtual void Awake()
        {
            Debug.Assert(m_controlledCharacter != null, $"[{Time.frameCount}] {this}: ModuleOwner is required");
        }

        protected virtual void Start()
        {
            m_controlledCharacter?.Mount(this);
        }

        public virtual void SetCharacterMovementReference(LegacyCharacterBase movement)
        {
            m_controlledCharacter = movement;
            m_controlledCharacter?.Mount(this);
        }

        // Update is called once per frame
        //private void Update()
        //{
        //    UpdateController();
        //}

        //private void FixedUpdate()
        //{
        //    ControllerFixedUpdate();
        //}

        protected virtual void ControllerUpdate()
        { }

        protected virtual void ControllerFixedUpdate()
        { }
    }
}