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
        public CharacterPhysicsModule Body => m_bodyModule;
        public Vector3 Position => Body.Position;
        public Quaternion Rotation => Body.Rotation;

        [SerializeField]
        private CharacterPhysicsModule m_bodyModule;

        [SerializeField, Tooltip("Each frame, the modules are sorted per priority and availability and then executed.")]
        private List<CharacterVelocityModule> m_velocityModules;

        [SerializeField, Tooltip("Only one rotation module executed per frame. The best module is evaluated based on availability and priority.")]
        private List<CharacterRotationModuleBase> m_rotationModules;

        [SerializeField, ReadOnly]
        private Vector3 currentVel = Vector3.zero;

        private CharacterRotationModuleBase GetBestRotationModule()
        {
            if (m_rotationModules == null)
            {
                return null;
            }

            int bestPriority = 0;
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

        public bool TryGetVelocityModule<T>(out T outModule) where T : CharacterVelocityModule
        {
            outModule = null;
            for (int i = 0; i < m_velocityModules.Count; ++i)
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
            for (int i = 0; i < m_rotationModules.Count; ++i)
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
            return m_bodyModule.Velocity;
        }

        public float GetMoveSpeed()
        {
            return m_bodyModule.Velocity.magnitude;
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

            if (!m_bodyModule)
            {
                m_bodyModule = GetComponent<CharacterPhysicsModule>();
                if (m_bodyModule == null)
                {
                    m_bodyModule = gameObject.AddComponent<CharacterUnityCharacterController>();
                    Debug.LogWarning($"No body module found on {this}, instancing a default CharacterUnityCharacterController.");
                }
            }

            m_bodyModule.ModuleInit(this);
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

        private void Start()
        {
            ModulesInit();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            var rotationModule = GetBestRotationModule();
            if (rotationModule != null)
            {
                rotationModule.RotationUpdate(deltaTime);
            }

            if (m_bodyModule.VelocityUpdate != CharacterPhysicsModule.VelocityApplicationUpdate.Update)
            {
                return;
            }

            MovementProcessing(deltaTime);
        }

        private void FixedUpdate()
        {
            if (m_bodyModule.VelocityUpdate != CharacterPhysicsModule.VelocityApplicationUpdate.FixedUpdate)
            {
                return;
            }

            MovementProcessing(Time.fixedDeltaTime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            m_bodyModule.OnModuleCollisionEnter(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            m_bodyModule.OnModuleCollisionStay(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            m_bodyModule.OnModuleCollisionExit(collision);
        }

        private void MovementProcessing(float deltaTime)
        {
            m_velocityModules.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            currentVel = m_bodyModule.Velocity;
            bool isGrounded = m_bodyModule.IsGrounded;

            for (int i = 0, c = m_velocityModules.Count; i < c; ++i)
            {
                var vModule = m_velocityModules[i];
                vModule.StateUpdate(isGrounded);
                if (vModule.CanBeExecuted())
                {
                    currentVel = vModule.VelocityUpdate(currentVel, deltaTime);
                }
            }

            m_bodyModule.ApplyVelocity(currentVel, deltaTime);
        }
    }
}