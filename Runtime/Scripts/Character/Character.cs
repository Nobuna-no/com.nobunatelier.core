using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public class Character : MonoBehaviour
    {
        private const float kMinimumMovementTreshold = 0.1f;
        public virtual CharacterControllerBase Controller { get; private set; }
        public Animator Animator { get; private set; }

        // public virtual bool IsTargetable => true;
        public Transform Transform => Body ? Body.transform : transform;

        public CharacterPhysicsModule Body => m_physicsModule;
        public Vector3 Position => Body ? Body.Position : transform.position;
        public Quaternion Rotation => Body ? Body.Rotation : transform.rotation;

        public event Action OnPreUpdate;
        public event Action OnPostUpdate;

        [SerializeField]
        private CharacterPhysicsModule m_physicsModule;

        [SerializeField, Tooltip("Each frame, the modules are sorted per priority and availability and then executed.")]
        private List<CharacterVelocityModuleBase> m_velocityModules = new List<CharacterVelocityModuleBase>();

        [SerializeField, Tooltip("Only one rotation module executed per frame. The best module is evaluated based on availability and priority.")]
        private List<CharacterRotationModuleBase> m_rotationModules = new List<CharacterRotationModuleBase>();

        [SerializeField, Tooltip("Each frame, the modules are sorted per priority and availability and then executed.")]
        private List<CharacterAbilityModuleBase> m_abilityModules = new List<CharacterAbilityModuleBase>();

        [SerializeField, ReadOnly]
        private Vector3 currentVel = Vector3.zero;

        [SerializeField]
        private bool m_ignoreMissingModule = false;
        [SerializeField] private bool m_autoCaptureModules = true;

        public bool TryGetAbilityModule<T>(out T outModule) where T : CharacterAbilityModuleBase
        {
            outModule = null;
            for (int i = 0, c = m_abilityModules.Count; i < c; ++i)
            {
                var module = m_abilityModules[i];
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
            for (int i = 0, c = m_velocityModules.Count; i < c; ++i)
            {
                var module = m_velocityModules[i];
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
            for (int i = 0, c = m_rotationModules.Count; i < c; ++i)
            {
                var module = m_rotationModules[i];
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
            return currentVel;
        }

        public float GetMoveSpeed()
        {
            return currentVel.magnitude;
        }

        public bool IsMoving => currentVel.sqrMagnitude > kMinimumMovementTreshold;

        public Vector3 GetNormalizedMoveSpeed()
        {
            return Vector3.Normalize(m_physicsModule.Velocity);
        }

        public void Move(Vector3 direction)
        {
            for (int i = 0, c = m_velocityModules.Count; i < c; ++i)
            {
                m_velocityModules[i].MoveInput(direction);
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

            if (m_ignoreMissingModule)
            {
                return;
            }
            
            if (m_physicsModule == null)
            {
                m_physicsModule = gameObject.AddComponent<CharacterUnityCharacterController>();
                Debug.LogWarning($"No physics module found on {this}, instancing a default CharacterUnityCharacterController.");
            }
            if (m_velocityModules == null || m_velocityModules.Count == 0)
            {
                Debug.LogWarning($"No velocity module found on {this}.");
            }
            if (m_rotationModules == null || m_rotationModules.Count == 0)
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

            if (m_physicsModule?.VelocityUpdate != CharacterPhysicsModule.VelocityApplicationUpdate.Update)
            {
                return;
            }

            MovementProcessing(deltaTime);

            OnPostUpdate?.Invoke();
        }

        private void FixedUpdate()
        {
            if (m_physicsModule?.VelocityUpdate != CharacterPhysicsModule.VelocityApplicationUpdate.FixedUpdate)
            {
                return;
            }

            MovementProcessing(Time.fixedDeltaTime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            m_physicsModule?.OnModuleCollisionEnter(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            m_physicsModule?.OnModuleCollisionStay(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            m_physicsModule?.OnModuleCollisionExit(collision);
        }

        private void OnValidate()
        {
            CaptureModules();
        }

        [Button("Refresh modules")]
        private void CaptureModules()
        {
            if (!m_autoCaptureModules)
            {
                return;
            }

            m_physicsModule = GetComponent<CharacterPhysicsModule>();

            m_abilityModules.Clear();
            m_abilityModules.AddRange(GetComponentsInChildren<CharacterAbilityModuleBase>());

            m_velocityModules.Clear();
            m_velocityModules.AddRange(GetComponents<CharacterVelocityModuleBase>());

            m_rotationModules.Clear();
            m_rotationModules.AddRange(GetComponents<CharacterRotationModuleBase>());
        }

        private void ModulesInit()
        {
            m_physicsModule?.ModuleInit(this);

            for (int i = 0, c = m_abilityModules.Count; i < c; ++i)
            {
                m_abilityModules[i].ModuleInit(this);
            }

            for (int i = 0, c = m_velocityModules.Count; i < c; ++i)
            {
                m_velocityModules[i].ModuleInit(this);
            }

            for (int i = 0, c = m_rotationModules.Count; i < c; ++i)
            {
                m_rotationModules[i].ModuleInit(this);
            }
        }

        // For rotation, we only want to get the most suitable module as in general using multiple at the same
        // time would end up in wanted behavior.
        // However, if all modules with the same highest priority will be computed.
        private CharacterRotationModuleBase[] GetBestRotationModules()
        {
            if (m_rotationModules == null || m_rotationModules.Count == 0)
            {
                return null;
            }


            int bestPriority = -1;
            List<CharacterRotationModuleBase> bestModules = new List<CharacterRotationModuleBase>();
            // Sort ascending order (lower first).
            m_rotationModules.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            for (int i = 0, c = m_rotationModules.Count; i < c; i++)
            {
                if (!m_rotationModules[i].CanBeExecuted())
                {
                    continue;
                }

                if (bestModules.Count > 0 && m_rotationModules[i].Priority > bestPriority)
                {
                    break;
                }

                bestModules.Add(m_rotationModules[i]);
                bestPriority = m_rotationModules[i].Priority;
            }

            return bestModules.ToArray();
        }

        private void MovementProcessing(float deltaTime)
        {
            if (!m_physicsModule)
            {
                return;
            }

            m_velocityModules.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            currentVel = m_physicsModule.Velocity;
            bool isGrounded = m_physicsModule.IsGrounded;

            for (int i = 0, c = m_velocityModules.Count; i < c; ++i)
            {
                var vModule = m_velocityModules[i];
                vModule.StateUpdate(isGrounded);
                if (vModule.CanBeExecuted())
                {
                    currentVel = vModule.VelocityUpdate(currentVel, deltaTime);
                }
            }

            m_physicsModule.ApplyVelocity(currentVel, deltaTime);
        }

        private void AbilityProcessing(float deltaTime)
        {
            m_abilityModules.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            for (int i = 0, c = m_abilityModules.Count; i < c; ++i)
            {
                var vModule = m_abilityModules[i];
                if (vModule.CanBeExecuted())
                {
                    vModule.AbilityUpdate(deltaTime);
                }
            }
        }
    }
}