using UnityEngine;

// Can be improved by splitting the logic:
// - ModuleOwner -> handle mounting, animation and any character behaviour such as ModuleOwner...
// - CharacterMovement -> only handle movement logic
namespace NobunAtelier
{
    public abstract class Character : MonoBehaviour, ITargetable
    {
        public virtual CharacterController Controller { get; private set; }
        public Animator Animator { get; private set; }
        public virtual bool IsTargetable => true;
        public virtual Transform Transform => transform;
        public virtual Vector3 Position => transform.position;

        protected Vector3 m_initialPosition;

        public abstract Vector3 GetMoveVector();

        public abstract float GetMoveSpeed();

        public abstract float GetNormalizedMoveSpeed();

        public virtual void Mount(CharacterController controller)
        {
            Controller = controller;
        }

        // moveInput doesn't need to be normalized
        public virtual void Move(Vector3 moveInput, float deltaTime)
        {
        }

        public virtual void ProceduralMove(Vector3 deltaMovement)
        {
        }

        public virtual void Rotate(Vector3 normalizedDirection)
        {
        }

        //public virtual void SetForward(Vector3 dir, float stepSpeed)
        //{
        //}

        public virtual void AttachAnimator(Animator animator, bool destroyCurrent = true)
        {
            if (destroyCurrent && Animator)
            {
                Destroy(Animator.gameObject);
            }

            Animator = Instantiate(animator.gameObject, this.transform).GetComponent<Animator>();
        }

        public virtual void ResetLocalPosition()
        {
            transform.localPosition = m_initialPosition;
        }

        public virtual void ResetCharacter(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            Physics.SyncTransforms();
        }

        public virtual void ResetCharacter(Transform transform)
        {
            this.transform.SetPositionAndRotation(transform.position, transform.rotation);
            Physics.SyncTransforms();
        }

        protected virtual void Awake()
        {
            m_initialPosition = transform.localPosition;
            Animator = GetComponentInChildren<Animator>();
        }
    }
}