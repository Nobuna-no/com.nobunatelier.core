using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Accessibility;

namespace NobunAtelier
{

    // Unity pawn
    // Use Unity CharacterController to handle move.
    public class AtelierCharacter : Character
    {
        protected UnityEngine.CharacterController m_movement;
        protected UnityEngine.Rigidbody m_body;

        // [SerializeField]
        private CharacterMovementModule[] m_modules;

        // Executes all of them in priority order
        [Header("Velocity")]
        [SerializeField]
        private List<CharacterVelocityModule> m_velocityModules;

        [Header("Rotation")]
        // Evaluates and only execute the best module
        [SerializeField]
        private List<CharacterRotationModule> m_rotationModules;

        [SerializeField]
        private CharacterInputDrivenRotation m_inputDrivenRotationModule;
        [SerializeField]
        private CharacterVelocityDrivenRotation m_velocityDrivenRotationModule;
        [SerializeField]
        private CharacterRotationToTarget m_rotateTowardTargetModule;


        private Vector3 m_lastMoveDir;

        private CharacterRotationModule GetBestRotationModule()
        {
            if (m_rotationModules == null)
            {
                return null;
            }

            int bestPriority = 0;
            CharacterRotationModule bestModule = null;
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

        public T GetModule<T>() where T : CharacterMovementModule
        {
            foreach (var module in m_modules)
            {
                if (module.GetType() == typeof(T))
                {
                    return module as T;
                }
            }

            return null;
        }

        public T GetModule_Concept<T>() where T : CharacterModuleBase
        {
            foreach (var module in m_velocityModules)
            {
                if (module.GetType() == typeof(T))
                {
                    return module as T;
                }
            }

            return null;
        }

        public override Vector3 GetMoveVector()
        {
            return m_lastMoveDir;
        }

        public override float GetMoveSpeed()
        {
            return 0;
        }

        public override float GetNormalizedMoveSpeed()
        {
            return 0;
        }

        public override void Move(Vector3 direction)
        {
            Debug.Log($"Move: {direction}");
            for (int i = 0, c = m_velocityModules.Count; i < c; ++i)
            {
                m_velocityModules[i].MoveInput(direction);
            }
        }

        public override void ProceduralMove(Vector3 deltaMovement)
        {
        }

        public override void Rotate(Vector3 normalizedDirection)
        {
            var rotationModule = GetBestRotationModule();
            if (rotationModule == null)
            {
                return;
            }

            rotationModule.RotateInput(normalizedDirection);
            //if (m_inputDrivenRotation)
            //{
            //    m_inputDrivenRotationModule.RotateInput(normalizedDirection);
            //}
            //else
            //{
            //    m_velocityDrivenRotationModule.RotateInput(normalizedDirection);
            //}
        }

        protected override void Awake()
        {
            base.Awake();
            m_movement = GetComponent<UnityEngine.CharacterController>();

            //m_velocityModules = new List<CharacterVelocityModule>
            //{
            //    //m_inputDrivenRotationModule,
            //    //m_velocityDrivenRotationModule,
            //    m_pawnMovementModuleJump,
            //    m_pawnMovementModuleGravity,
            //    m_pawnMovementModule2D
            //};

            //m_rotationModules = new List<CharacterRotationModule>
            //{
            //    m_inputDrivenRotationModule,
            //    m_velocityDrivenRotationModule,
            //    m_rotateTowardTargetModule
            //};
        }

        private void ModulesInit()
        {
            for (int i = 0, c = m_velocityModules.Count;  i < c; ++i)
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

            // m_inputDrivenRotationModule.ModuleInit(this);
            // m_rotateTowardTargetModule.ModuleInit(this);
            // m_velocityDrivenRotationModule.ModuleInit(this);
            // m_pawnMovementModuleJump.ModuleInit(this);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            var rotationModule = GetBestRotationModule();
            if (rotationModule != null)
            {
                rotationModule.RotationUpdate(deltaTime);
            }

            //if (m_inputDrivenRotation)
            //{
            //    m_inputDrivenRotationModule.RotationUpdate(deltaTime);
            //}
            //else
            //{
            //    m_velocityDrivenRotationModule.RotationUpdate(deltaTime);
            //}

            m_velocityModules.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            bool isGrounded = m_movement.isGrounded;
            Vector3 currentVel = Vector3.zero;

            for (int i = 0, c = m_velocityModules.Count; i < c; ++i)
            {
                m_velocityModules[i].StateUpdate(isGrounded);
                if (m_velocityModules[i].CanBeExecuted())
                {
                    currentVel = m_velocityModules[i].VelocityUpdate(currentVel, deltaTime);
                }
            }

            //currentVel = m_pawnMovementModule2D.VelocityUpdate(currentVel, deltaTime);
            //if (m_useGravity)
            //{
            //    m_pawnMovementModuleGravity.StateUpdate(m_movement.isGrounded);
            //    currentVel = m_pawnMovementModuleGravity.VelocityUpdate(currentVel, deltaTime);
            //}
            //if (m_canJump)
            //{
            //    m_pawnMovementModuleJump.StateUpdate(m_movement.isGrounded);
            //    currentVel = m_pawnMovementModuleJump.VelocityUpdate(currentVel, deltaTime);
            //}

            m_lastMoveDir = currentVel.normalized;
            // CharacterController.Move uses deltaPosition.
            m_movement.Move(currentVel * deltaTime);

            // CharacterController.SimpleMove uses units/s.
            // m_movement.SimpleMove(currentVel);

            // Rigidbody gave more control but might not be useful for more simpler movement...
        }
    }
}