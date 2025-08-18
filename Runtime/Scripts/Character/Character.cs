using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    public class Character : MonoBehaviour
    {
        private const float kMinimumMovementTreshold = 0.1f;
        public virtual CharacterControllerBase Controller { get; private set; }
        public Animator Animator { get; private set; }

        // public virtual bool IsTargetable => true;
        public Transform Transform => Body ? Body.transform : transform;

        public CharacterPhysicsModule Body => m_PhysicsModule;
        public Vector3 Position => Body ? Body.Position : transform.position;
        public Quaternion Rotation => Body ? Body.Rotation : transform.rotation;

        public event Action OnPreUpdate;
        public event Action OnPostUpdate;

        [SerializeField]
        [FormerlySerializedAs("m_physicsModule")]
        private CharacterPhysicsModule m_PhysicsModule;

        [SerializeField, Tooltip("Each frame, the modules are sorted per priority and availability and then executed.")]
        [FormerlySerializedAs("m_velocityModules")]
        private List<CharacterVelocityModuleBase> m_VelocityModules = new List<CharacterVelocityModuleBase>();

        [SerializeField, Tooltip("Only one rotation module executed per frame. The best module is evaluated based on availability and priority.")]
        [FormerlySerializedAs("m_rotationModules")]
        private List<CharacterRotationModuleBase> m_RotationModules = new List<CharacterRotationModuleBase>();

        [SerializeField, Tooltip("Each frame, the modules are sorted per priority and availability and then executed.")]
        [FormerlySerializedAs("m_abilityModules")]
        private List<CharacterAbilityModuleBase> m_AbilityModules = new List<CharacterAbilityModuleBase>();

        [SerializeField, ReadOnly]
        [FormerlySerializedAs("currentVel")]
        private Vector3 m_CurrentVel = Vector3.zero;

        [SerializeField]
        [FormerlySerializedAs("m_ignoreMissingModule")]
        private bool m_IgnoreMissingModule = false;
        [FormerlySerializedAs("m_autoCaptureModules")]
        [SerializeField] private bool m_AutoCaptureModules = true;

        public bool TryGetAbilityModule<T>(out T outModule) where T : CharacterAbilityModuleBase
        {
            outModule = null;
            for (int i = 0, c = m_AbilityModules.Count; i < c; ++i)
            {
                var module = m_AbilityModules[i];
                if (module.GetType() == typeof(T))
                {
                    outModule = module as T;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetVelocityModule<T>(out T outModule) where T : CharacterVelocityModuleBase
        {
            outModule = null;
            for (int i = 0, c = m_VelocityModules.Count; i < c; ++i)
            {
                var module = m_VelocityModules[i];
                if (module.GetType() == typeof(T))
                {
                    outModule = module as T;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetRotationModule<T>(out T outModule) where T : CharacterRotationModuleBase
        {
            outModule = null;
            for (int i = 0, c = m_RotationModules.Count; i < c; ++i)
            {
                var module = m_RotationModules[i];
                if (module.GetType() == typeof(T))
                {
                    outModule = module as T;
                    return true;
                }
            }

            return false;
        }

        public virtual void SetController(CharacterControllerBase controller)
        {
            Controller = controller;
        }

        public Vector3 GetMoveVector()
        {
            return m_CurrentVel;
        }

        public float GetMoveSpeed()
        {
            return m_CurrentVel.magnitude;
        }

        public bool IsMoving => m_CurrentVel.sqrMagnitude > kMinimumMovementTreshold;

        public Vector3 GetNormalizedMoveSpeed()
        {
            return Vector3.Normalize(m_PhysicsModule.Velocity);
        }

        public void Move(Vector3 direction)
        {
            for (int i = 0, c = m_VelocityModules.Count; i < c; ++i)
            {
                m_VelocityModules[i].MoveInput(direction);
            }
        }

        public void Rotate(Vector3 direction)
        {
            var rotationModules = GetBestRotationModules();
            if (rotationModules == null)
            {
                return;
            }

            foreach (var module in rotationModules)
            {
                module.RotateInput(direction);
            }
        }

        public void ResetCharacter(Vector3 position, Quaternion rotation)
        {
            Body.Position = position;
            Body.Rotation = rotation;
            Physics.SyncTransforms();
        }

        public void ResetCharacter(Transform transform)
        {
            Body.Position = transform.position;
            Body.Rotation = transform.rotation;
            Physics.SyncTransforms();
        }

        protected void Awake()
        {
            Animator = GetComponentInChildren<Animator>();

            CaptureModules();

            if (m_IgnoreMissingModule)
            {
                return;
            }

            if (m_PhysicsModule == null)
            {
                m_PhysicsModule = gameObject.AddComponent<CharacterUnityCharacterController>();
                Debug.LogWarning($"No physics module found on {this}, instancing a default CharacterUnityCharacterController.");
            }
            if (m_VelocityModules == null || m_VelocityModules.Count == 0)
            {
                Debug.LogWarning($"No velocity module found on {this}.");
            }
            if (m_RotationModules == null || m_RotationModules.Count == 0)
            {
                Debug.LogWarning($"No rotation module found on {this}.");
            }
        }

        private void Start()
        {
            ModulesInit();
        }

        private void Update()
        {
            OnPreUpdate?.Invoke();

            float deltaTime = Time.deltaTime;

            var rotationModules = GetBestRotationModules();
            if (rotationModules != null)
            {
                foreach (var module in rotationModules)
                {
                    module.RotationUpdate(deltaTime);
                }
            }

            AbilityProcessing(deltaTime);

            if (m_PhysicsModule?.VelocityUpdate != CharacterPhysicsModule.VelocityApplicationUpdate.Update)
            {
                return;
            }

            MovementProcessing(deltaTime);

            OnPostUpdate?.Invoke();
        }

        private void FixedUpdate()
        {
            if (m_PhysicsModule?.VelocityUpdate != CharacterPhysicsModule.VelocityApplicationUpdate.FixedUpdate)
            {
                return;
            }

            MovementProcessing(Time.fixedDeltaTime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            m_PhysicsModule?.OnModuleCollisionEnter(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            m_PhysicsModule?.OnModuleCollisionStay(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            m_PhysicsModule?.OnModuleCollisionExit(collision);
        }

        private void OnValidate()
        {
            CaptureModules();
        }

        [Button("Refresh modules")]
        private void CaptureModules()
        {
            if (!m_AutoCaptureModules)
            {
                return;
            }

            m_PhysicsModule = GetComponent<CharacterPhysicsModule>();

            m_AbilityModules.Clear();
            m_AbilityModules.AddRange(GetComponentsInChildren<CharacterAbilityModuleBase>());

            m_VelocityModules.Clear();
            m_VelocityModules.AddRange(GetComponents<CharacterVelocityModuleBase>());

            m_RotationModules.Clear();
            m_RotationModules.AddRange(GetComponents<CharacterRotationModuleBase>());
        }

        private void ModulesInit()
        {
            m_PhysicsModule?.ModuleInit(this);

            for (int i = 0, c = m_AbilityModules.Count; i < c; ++i)
            {
                m_AbilityModules[i].ModuleInit(this);
            }

            for (int i = 0, c = m_VelocityModules.Count; i < c; ++i)
            {
                m_VelocityModules[i].ModuleInit(this);
            }

            for (int i = 0, c = m_RotationModules.Count; i < c; ++i)
            {
                m_RotationModules[i].ModuleInit(this);
            }
        }

        // For rotation, we only want to get the most suitable module as in general using multiple at the same
        // time would end up in wanted behavior.
        // However, if all modules with the same highest priority will be computed.
        private CharacterRotationModuleBase[] GetBestRotationModules()
        {
            if (m_RotationModules == null || m_RotationModules.Count == 0)
            {
                return null;
            }


            int bestPriority = -1;
            List<CharacterRotationModuleBase> bestModules = new List<CharacterRotationModuleBase>();
            // Sort ascending order (lower first).
            m_RotationModules.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            for (int i = 0, c = m_RotationModules.Count; i < c; i++)
            {
                if (!m_RotationModules[i].CanBeExecuted())
                {
                    continue;
                }

                if (bestModules.Count > 0 && m_RotationModules[i].Priority > bestPriority)
                {
                    break;
                }

                bestModules.Add(m_RotationModules[i]);
                bestPriority = m_RotationModules[i].Priority;
            }

            return bestModules.ToArray();
        }

        private void MovementProcessing(float deltaTime)
        {
            if (!m_PhysicsModule)
            {
                return;
            }

            m_VelocityModules.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            m_CurrentVel = m_PhysicsModule.Velocity;
            bool isGrounded = m_PhysicsModule.IsGrounded;

            for (int i = 0, c = m_VelocityModules.Count; i < c; ++i)
            {
                var vModule = m_VelocityModules[i];
                vModule.StateUpdate(isGrounded);
                if (vModule.CanBeExecuted())
                {
                    m_CurrentVel = vModule.VelocityUpdate(m_CurrentVel, deltaTime);
                }
            }

            m_PhysicsModule.ApplyVelocity(m_CurrentVel, deltaTime);
        }

        private void AbilityProcessing(float deltaTime)
        {
            m_AbilityModules.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            for (int i = 0, c = m_AbilityModules.Count; i < c; ++i)
            {
                var vModule = m_AbilityModules[i];
                if (vModule.CanBeExecuted())
                {
                    vModule.AbilityUpdate(deltaTime);
                }
            }
        }
    }
}