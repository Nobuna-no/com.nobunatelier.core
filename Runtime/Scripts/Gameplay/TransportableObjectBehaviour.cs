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

        [Header("Drop Effect")]
        [SerializeField] private float m_dropEffectForce = 5;
        [SerializeField] private Vector3 m_dropEffectOrigin = Vector3.one;

        private const float k_ExplosiveForce = 100;

        public virtual bool Pick()
        {
            if (!IsPickable)
            {
                return false;
            }

            EnablePhysics(false);
            return true;
        }

        protected Vector3 GetLocalSpawnPointInSphere()
        {
            Vector3 vec = Random.insideUnitSphere * k_ExplosiveForce;
            vec.x *= m_dropEffectOrigin.x;
            vec.z *= m_dropEffectOrigin.z;
            vec.y = -m_dropEffectOrigin.y;
            return vec;
        }

        public virtual void Drop(bool withExplosiveForce = false)
        {
            EnablePhysics(true);

            if (withExplosiveForce)
            {
                RigidBody.AddExplosionForce(RigidBody.mass * m_dropEffectForce * k_ExplosiveForce, RigidBody.position + GetLocalSpawnPointInSphere(), k_ExplosiveForce * m_dropEffectForce);
            }
        }

        public virtual void Throw(Vector3 dir, float force)
        {
            Drop();
            RigidBody.AddForce(dir * force * RigidBody.mass, ForceMode.Impulse);
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