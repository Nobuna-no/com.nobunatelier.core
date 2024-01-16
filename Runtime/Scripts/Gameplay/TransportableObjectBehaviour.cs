using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier.Gameplay
{
    public class TransportableObjectBehaviour : PoolableBehaviour
    {
        protected const float k_ExplosiveForce = 100;

        [Header("Transportable Object")]
        [SerializeField] private bool m_isPickable = true;
        [SerializeField] private Rigidbody m_targetRigidbody = null;
        [SerializeField] private Collider m_targetInteractionCollider = null;
        [SerializeField] private bool m_usePhysics = true;

        [Header("Drop Effect")]
        [SerializeField] private bool m_dropEffect = true;
        [ShowIf("m_dropEffect"), SerializeField] private float m_dropEffectForce = 5;
        [ShowIf("m_dropEffect"), SerializeField] private Vector3 m_dropEffectOrigin = Vector3.one;

        [Header("Events")]
        public UnityEvent OnPickedEvent;
        public UnityEvent OnDroppedEvent;
        public UnityEvent OnThrownEvent;

        public Rigidbody TargetRigidbody => m_targetRigidbody;
        public Collider Collider => m_targetInteractionCollider;
        public bool IsPickable
        {
            get => m_isPickable;
            set => m_isPickable = value;
        }
        public bool HasDropEffect => m_dropEffect;
        public float DropEffectForce => m_dropEffectForce;

        public virtual bool Pick()
        {
            if (!m_isPickable)
            {
                return false;
            }

            EnablePhysics(false);
            OnPickedEvent?.Invoke();
            return true;
        }

        public virtual void Drop(bool withExplosiveForce = false)
        {
            EnablePhysics(true);
            OnDroppedEvent?.Invoke();

            if (m_dropEffect && withExplosiveForce)
            {
                TargetRigidbody.AddExplosionForce(TargetRigidbody.mass * m_dropEffectForce * k_ExplosiveForce, TargetRigidbody.position + GetLocalSpawnPointInSphere(), k_ExplosiveForce * m_dropEffectForce);
            }
        }

        public virtual void Throw(Vector3 dir, float force)
        {
            EnablePhysics(true);
            OnThrownEvent?.Invoke();
            TargetRigidbody.AddForce(dir * force * TargetRigidbody.mass, ForceMode.Impulse);
        }

        protected override void OnActivation()
        {
            Drop();
        }

        protected override void OnDeactivation()
        {
            EnablePhysics(false);
        }

        protected void EnablePhysics(bool enable)
        {
            enable &= m_usePhysics;

            m_isPickable = enable;
            Collider.enabled = enable;
            TargetRigidbody.isKinematic = !enable;
            TargetRigidbody.useGravity = enable;
            TargetRigidbody.detectCollisions = enable;
        }

        protected override void Awake()
        {
            base.Awake();

            if (m_targetInteractionCollider == null)
            {
                m_targetInteractionCollider = GetComponent<Collider>();
            }

            if (m_targetRigidbody == null)
            {
                m_targetRigidbody = GetComponent<Rigidbody>();
            }
        }

        protected Vector3 GetLocalSpawnPointInSphere()
        {
            Vector3 vec = Random.insideUnitSphere * k_ExplosiveForce;
            vec.x *= m_dropEffectOrigin.x;
            vec.z *= m_dropEffectOrigin.z;
            vec.y = -m_dropEffectOrigin.y;
            return vec;
        }
    }
}