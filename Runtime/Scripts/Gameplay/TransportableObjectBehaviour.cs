using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier.Gameplay
{
    public class TransportableObjectBehaviour : FactoryProduct
    {
        protected const float k_ExplosiveForce = 100;

        [Header("Transportable Object")]
        [SerializeField] private bool m_isPickable = true;
        [SerializeField] private Rigidbody m_targetRigidbody = null;
        [SerializeField] private Collider m_targetInteractionCollider = null;
        [SerializeField] private bool m_usePhysics = true;

        private RigidbodyInterpolation m_savedInterpolation = RigidbodyInterpolation.None;
        private Transform m_parentBeforeSocketAttach;
        private Transform m_attachedSocket;

        public bool IsAttachedToSocket => m_attachedSocket != null;

        [Header("Carry On Socket")]
        [SerializeField] private Transform m_carryPoseAnchor;
        [SerializeField] private bool m_useAuthoringCarryLocalTransform;
        [ShowIf(nameof(ShowAuthoringCarryFields)), SerializeField]
        private Vector3 m_authoringCarryLocalPosition = Vector3.zero;
        [ShowIf(nameof(ShowAuthoringCarryFields)), SerializeField]
        private Vector3 m_authoringCarryLocalEulerAngles = Vector3.zero;

        private bool ShowAuthoringCarryFields => m_useAuthoringCarryLocalTransform && m_carryPoseAnchor == null;

        [Header("Throw Effect")]
        [SerializeField] private bool m_scaleThrowWithRigidbodyMass = false;
        [SerializeField] private bool m_resetLocalTransformOnThrow = true;
        [SerializeField, ShowIf(nameof(m_resetLocalTransformOnThrow))]
        private bool m_resetLocalPositionOnThrow = false;
        [SerializeField] private bool m_useAuthoringRestLocalTransform = false;
        [ShowIf(nameof(m_useAuthoringRestLocalTransform)), SerializeField]
        private Vector3 m_authoringRestLocalPosition = Vector3.zero;
        [ShowIf(nameof(m_useAuthoringRestLocalTransform)), SerializeField]
        private Vector3 m_authoringRestLocalEulerAngles = Vector3.zero;
        [ShowIf(nameof(m_useAuthoringRestLocalTransform)), SerializeField]
        private Vector3 m_authoringRestLocalScale = Vector3.one;

        private Vector3 m_capturedRestLocalPosition;
        private Quaternion m_capturedRestLocalRotation;
        private Vector3 m_capturedRestLocalScale;
        private bool m_hasCapturedRestLocalTransform;

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

        public void AttachToSocket(Transform socket)
        {
            if (socket == null)
            {
                return;
            }

            m_parentBeforeSocketAttach = transform.parent;
            m_attachedSocket = socket;
            transform.SetParent(socket, false);
            TryGetCarryLocalTransform(out Vector3 localPosition, out Quaternion localRotation);
            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            SyncTargetRigidbodyTransform();
        }

        public void GetCarryWorldPose(Transform socket, out Vector3 worldPosition, out Quaternion worldRotation)
        {
            TryGetCarryLocalTransform(out Vector3 localPosition, out Quaternion localRotation);
            if (socket == null)
            {
                worldPosition = transform.position;
                worldRotation = transform.rotation;
                return;
            }

            worldPosition = socket.TransformPoint(localPosition);
            worldRotation = socket.rotation * localRotation;
        }

        public bool TryGetCarryLocalTransform(out Vector3 localPosition, out Quaternion localRotation)
        {
            if (m_carryPoseAnchor != null)
            {
                localPosition = -m_carryPoseAnchor.localPosition;
                localRotation = Quaternion.Inverse(m_carryPoseAnchor.localRotation);
                return true;
            }

            if (m_useAuthoringCarryLocalTransform)
            {
                localPosition = m_authoringCarryLocalPosition;
                localRotation = Quaternion.Euler(m_authoringCarryLocalEulerAngles);
                return true;
            }

            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;
            return false;
        }

        public void DetachFromSocket()
        {
            if (m_attachedSocket == null)
            {
                return;
            }

            transform.SetParent(m_parentBeforeSocketAttach, true);
            m_attachedSocket = null;
            m_parentBeforeSocketAttach = null;
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
            DetachFromSocket();
            ApplyRestLocalTransformOnThrow();
            EnablePhysics(true);
            OnThrownEvent?.Invoke();
            if (m_scaleThrowWithRigidbodyMass)
            {
                TargetRigidbody.AddForce(dir * force * TargetRigidbody.mass, ForceMode.Impulse);
            }
            else
            {
                TargetRigidbody.AddForce(dir * force, ForceMode.Impulse);
            }
        }

        protected override void OnProductReset()
        {
            DetachFromSocket();

            if (m_targetInteractionCollider == null)
            {
                m_targetInteractionCollider = GetComponent<Collider>();
            }

            if (m_targetRigidbody == null)
            {
                m_targetRigidbody = GetComponent<Rigidbody>();
            }

            CaptureRestLocalTransform();
        }

        protected override void OnProductActivation()
        {
            Drop();
        }

        protected override void OnProductDeactivation()
        {
            EnablePhysics(false);
        }

        protected void EnablePhysics(bool enable)
        {
            enable &= m_usePhysics;

            if (enable)
            {
                DetachFromSocket();
            }

            m_isPickable = enable;
            Collider.enabled = enable;

            if (!enable)
            {
                m_savedInterpolation = TargetRigidbody.interpolation;
                TargetRigidbody.interpolation = RigidbodyInterpolation.None;
            }
            else
            {
                TargetRigidbody.interpolation = m_savedInterpolation;
            }

            TargetRigidbody.isKinematic = !enable;
            TargetRigidbody.useGravity = enable;
            TargetRigidbody.detectCollisions = enable;
        }

        protected Vector3 GetLocalSpawnPointInSphere()
        {
            Vector3 vec = Random.insideUnitSphere * k_ExplosiveForce;
            vec.x *= m_dropEffectOrigin.x;
            vec.z *= m_dropEffectOrigin.z;
            vec.y = -m_dropEffectOrigin.y;
            return vec;
        }

        private void CaptureRestLocalTransform()
        {
            if (m_useAuthoringRestLocalTransform)
            {
                return;
            }

            m_capturedRestLocalPosition = transform.localPosition;
            m_capturedRestLocalRotation = transform.localRotation;
            m_capturedRestLocalScale = transform.localScale;
            m_hasCapturedRestLocalTransform = true;
        }

        private void ApplyRestLocalTransformOnThrow()
        {
            if (!m_resetLocalTransformOnThrow)
            {
                return;
            }

            Vector3 worldPosition = transform.position;

            Vector3 restLocalPosition;
            Quaternion restLocalRotation;
            Vector3 restLocalScale;
            if (!TryGetRestLocalTransform(out restLocalPosition, out restLocalRotation, out restLocalScale))
            {
                return;
            }

            if (m_resetLocalPositionOnThrow)
            {
                transform.localPosition = restLocalPosition;
            }

            transform.localRotation = restLocalRotation;
            transform.localScale = restLocalScale;

            if (!m_resetLocalPositionOnThrow)
            {
                transform.position = worldPosition;
            }

            SyncTargetRigidbodyTransform();
        }

        private void SyncTargetRigidbodyTransform()
        {
            if (TargetRigidbody != null && TargetRigidbody.transform == transform)
            {
                TargetRigidbody.position = transform.position;
                TargetRigidbody.rotation = transform.rotation;
            }
        }

        private bool TryGetRestLocalTransform(out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale)
        {
            if (m_useAuthoringRestLocalTransform)
            {
                localPosition = m_authoringRestLocalPosition;
                localRotation = Quaternion.Euler(m_authoringRestLocalEulerAngles);
                localScale = m_authoringRestLocalScale;
                return true;
            }

            if (m_hasCapturedRestLocalTransform)
            {
                localPosition = m_capturedRestLocalPosition;
                localRotation = m_capturedRestLocalRotation;
                localScale = m_capturedRestLocalScale;
                return true;
            }

            localPosition = default;
            localRotation = default;
            localScale = default;
            return false;
        }
    }
}