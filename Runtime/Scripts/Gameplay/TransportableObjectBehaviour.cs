using UnityEngine;

namespace NobunAtelier.Gameplay
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class TransportableObjectBehaviour : PoolableBehaviour
    {
        [Header("Transportable Object")]
        [SerializeField]
        private bool m_usePhysics = true;

        public Rigidbody RigidBody { get; private set; }
        public Collider Collider { get; private set; }

        public bool IsPickable { get; set; } = true;

        public virtual bool Pick()
        {
            if (!IsPickable)
            {
                return false;
            }

            EnablePhysics(false);
            return true;
        }

        public virtual void Drop()
        {
            EnablePhysics(true);
        }

        public virtual void Throw(Vector3 dir, float force)
        {
            Drop();
            RigidBody.AddForce(dir * force, ForceMode.Impulse);
        }

        protected override void OnActivation()
        {
            Drop();
        }

        protected override void OnDeactivation()
        {
            EnablePhysics(false);
        }

        private void EnablePhysics(bool enable)
        {
            enable &= m_usePhysics;

            IsPickable = enable;
            Collider.enabled = enable;
            RigidBody.isKinematic = !enable;
            RigidBody.useGravity = enable;
            RigidBody.detectCollisions = enable;
        }

        protected override void Awake()
        {
            base.Awake();

            Collider = GetComponent<Collider>();
            RigidBody = GetComponent<Rigidbody>();
        }
    }
}