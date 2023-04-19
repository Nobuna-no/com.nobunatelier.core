using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public class Character : MonoBehaviour, ITargetable
    {
        public virtual CharacterControllerBase Controller { get; private set; }
        public Animator Animator { get; private set; }
        public virtual bool IsTargetable => true;
        public virtual Transform Transform => Body.transform;
        public CharacterPhysicsModule Body => m_physicsModule;
        public Vector3 Position => Body.Position;
        public Quaternion Rotation => Body.Rotation;

        [SerializeField]
        private CharacterPhysicsModule m_physicsModule;

        [SerializeField, Tooltip("Each frame, the modules are sorted per priority and availability and then executed.")]
        private List<CharacterVelocityModuleBase> m_velocityModules;

        [SerializeField, Tooltip("Only one rotation module executed per frame. The best module is evaluated based on availability and priority.")]
        private List<CharacterRotationModuleBase> m_rotationModules;

        [SerializeField, ReadOnly]
        private Vector3 currentVel = Vector3.zero;

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

        public virtual void Mount(CharacterControllerBase controller)
        {
            Controller = controller;
        }

        public Vector3 GetMoveVector()
        {
            return m_physicsModule.Velocity;
        }

        public float GetMoveSpeed()
        {
            return m_physicsModule.Velocity.magnitude;
        }

        public float GetNormalizedMoveSpeed()
        {
            return 0;
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
            var rotationModule = GetBestRotationModule();
            if (rotationModule == null)
            {
                return;
            }

            rotationModule.RotateInput(direction);
        }

        public void ResetCharacter(Vector3 position, Quaternion rotation)
        {
            Body.Position = position;
            Body.Rotation = rotation;
        }

        public void ResetCharacter(Transform transform)
        {
            Body.Position = transform.position;
            Body.Rotation = transform.rotation;
        }

        protected void Awake()
        {
            Animator = GetComponentInChildren<Animator>();

            CaptureModules();

#if UNITY_EDITOR
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
#else
            Debug.Assert(m_physicsModule, $"{this} doesn't have a Physics module!");
            Debug.Assert(m_velocityModules && m_velocityModules.Count > 0, $"{this} doesn't have any Velocity module!");
            Debug.Assert(m_rotationModules && m_rotationModules.Count > 0, $"{this} doesn't have any Rotation module!");
#endif

            m_physicsModule.ModuleInit(this);
        }

        private void Start()
        {
            ModulesInit();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            var rotationModule = GetBestRotationModule();
            rotationModule?.RotationUpdate(deltaTime);

            if (m_physicsModule.VelocityUpdate != CharacterPhysicsModule.VelocityApplicationUpdate.Update)
            {
                return;
            }

            MovementProcessing(deltaTime);
        }

        private void FixedUpdate()
        {
            if (m_physicsModule.VelocityUpdate != CharacterPhysicsModule.VelocityApplicationUpdate.FixedUpdate)
            {
                return;
            }

            MovementProcessing(Time.fixedDeltaTime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            m_physicsModule.OnModuleCollisionEnter(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            m_physicsModule.OnModuleCollisionStay(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            m_physicsModule.OnModuleCollisionExit(collision);
        }

        private void OnValidate()
        {
            CaptureModules();
        }

        [Button("Refresh modules")]
        private void CaptureModules()
        {
            m_physicsModule = GetComponent<CharacterPhysicsModule>();
            
            m_velocityModules.Clear();
            m_velocityModules.AddRange(GetComponents<CharacterVelocityModuleBase>());

            m_rotationModules.Clear();
            m_rotationModules.AddRange(GetComponents<CharacterRotationModuleBase>());
        }

        private void ModulesInit()
        {
            for (int i = 0, c = m_velocityModules.Count; i < c; ++i)
            {
                m_velocityModules[i].ModuleInit(this);
            }

            for (int i = 0, c = m_rotationModules.Count; i < c; ++i)
            {
                m_rotationModules[i].ModuleInit(this);
            }
        }

        private CharacterRotationModuleBase GetBestRotationModule()
        {
            if (m_rotationModules == null || m_rotationModules.Count == 0)
            {
                return null;
            }

            int bestPriority = -1;
            CharacterRotationModuleBase bestModule = null;
            for (int i = 0, c = m_rotationModules.Count; i < c; i++)
            {
                if (m_rotationModules[i].CanBeExecuted() && m_rotationModules[i].Priority > bestPriority)
                {
                    bestModule = m_rotationModules[i];
                    bestPriority = bestModule.Priority;
                }
            }

            return bestModule;
        }

        private void MovementProcessing(float deltaTime)
        {
            m_velocityModules.Sort((x, y) => x.Priority.CompareTo(y.Priority));
            m_velocityModules.Reverse();

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
    }
}