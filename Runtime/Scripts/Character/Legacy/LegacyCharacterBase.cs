using UnityEngine;
using UnityEngine.AI;

// Can be improved by splitting the logic:
// - ModuleOwner -> handle mounting, animation and any character behaviour such as ModuleOwner...
// - ControlledCharacter -> only handle movement logic
namespace NobunAtelier
{
    public abstract class LegacyCharacterBase : MonoBehaviour, ITargetable
    {
        public virtual LegacyCharacterControllerBase Controller { get; private set; }
        public Animator Animator { get; private set; }
        public virtual bool IsTargetable => true;
        public virtual Transform Transform => transform;
        public virtual Vector3 Position => transform.position;
        public virtual Quaternion Rotation => transform.rotation;

        protected Vector3 m_initialPosition;
        protected Vector3 m_initialWorldPosition;
        protected Quaternion m_initialWorldRotation;

        private NavMeshAgent m_navMeshAgent = null;

        public abstract Vector3 GetMoveVector();

        public abstract float GetMoveSpeed();

        public abstract float GetNormalizedMoveSpeed();

        public virtual void Mount(LegacyCharacterControllerBase controller)
        {
            Controller = controller;
        }

        public virtual void Move(Vector3 direction)
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
            if (m_navMeshAgent)
            {
                m_navMeshAgent.enabled = false;
            }
            transform.localPosition = m_initialPosition;
            Physics.SyncTransforms();
            if (m_navMeshAgent)
            {
                m_navMeshAgent.enabled = true;
            }
        }

        public virtual void ResetInitialWorldTransform()
        {
            if (m_navMeshAgent)
            {
                m_navMeshAgent.enabled = false;
            }
            transform.position = m_initialWorldPosition;
            transform.rotation = m_initialWorldRotation;
            Physics.SyncTransforms();
            if (m_navMeshAgent)
            {
                m_navMeshAgent.enabled = true;
            }
        }

        public virtual void ResetCharacter(Vector3 position, Quaternion rotation)
        {
            if (m_navMeshAgent)
            {
                m_navMeshAgent.enabled = false;
            }
            transform.SetPositionAndRotation(position, rotation);
            Physics.SyncTransforms();
            if (m_navMeshAgent)
            {
                m_navMeshAgent.enabled = true;
            }
        }

        public virtual void ResetCharacter(Transform transform)
        {
            if (m_navMeshAgent)
            {
                m_navMeshAgent.enabled = false;
            }
            this.transform.SetPositionAndRotation(transform.position, transform.rotation);
            Physics.SyncTransforms();
            if (m_navMeshAgent)
            {
                m_navMeshAgent.enabled = true;
            }
        }

        protected virtual void Awake()
        {
            m_initialWorldPosition = transform.position;
            m_initialWorldRotation = transform.rotation;

            m_initialPosition = transform.localPosition;
            Animator = GetComponentInChildren<Animator>();
            m_navMeshAgent = GetComponent<NavMeshAgent>();
        }
    }
}