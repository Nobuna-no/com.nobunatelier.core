using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Accessibility;

namespace NobunAtelier
{
    // Need a way to say for which physics controller the movement will be use... (CharacterController.Move vs SimpleMove vs Rigidbody velocity)
    //public abstract class CharacterModule
    //{
    //    public AtelierCharacter ModuleOwner { get; private set; }

    //    public virtual void ModuleInit(AtelierCharacter character)
    //    {
    //        ModuleOwner = character;
    //    }
    //}

    public abstract class AtelierCharacterRotation : AtelierCharacterModule
    {
        public virtual void RotateInput(Vector3 normalizedDirection) { }

        public abstract void RotationUpdate(float deltaTime);
    }

    public class AtelierCharacterMovementState : ScriptableObject
    {
        //Unknown,
        //Grounded,
        //Crouched,
        //InAir,
        //Swimming,
        //Flying
    }

    public abstract class AtelierCharacterMovement : AtelierCharacterModule
    {
        public Vector2 LastMoveInput { get; protected set; }

        public virtual void MoveInput(Vector2 input)
        {
            LastMoveInput = input;
        }

        public virtual void StateUpdate(bool grounded)
        { }

        public abstract Vector3 VelocityUpdate(Vector3 currentVelocity, float deltaTime);
    }

    [Serializable]
    public class AtelierCharacter2DMovement : AtelierCharacterMovement
    {
        public enum MovementAxes
        {
            XZ,
            XY,
            YZ,
            Custom
        }

        public enum VelocityProcessing
        {
            // No acceleration, velocity calculated from raw inputDirection
            FromRawInput,
            // Prioritize acceleration control over velocity
            FromAcceleration,
            // Use the acceleration but prioritize velocity control
            DesiredVelocityFromAcceleration,
        }

        [SerializeField]
        private MovementAxes m_movementAxes = MovementAxes.XZ;
        [SerializeField]
        private VelocityProcessing m_accelerationApplication = VelocityProcessing.FromRawInput;
        [SerializeField, Range(0, 100f)]
        private float m_maxAcceleration = 10.0f;
        [SerializeField, Range(0, 100f)]
        private float m_maxSpeed = 10.0f;

        private Vector3 m_movementVector;
        private Vector3 m_acceleration;

        public Vector3 CustomMovementAxesForward = Vector3.forward;
        public Vector3 CustomMovementAxesRight = Vector3.right;

        // 1. Evaluate current active state
        // 2. Feed inputDirection
        // 3. Update velocity?

        // issue with this:
        // - Module_Movement
        // - Module_Rotation
        // - Module_Animation

        public bool EvaluateState()
        {
            return true;
        }

        public override void MoveInput(Vector2 inputDirection)
        {
            switch (m_movementAxes)
            {
                case MovementAxes.XZ:
                    m_movementVector = inputDirection;
                    m_movementVector.y = 0;
                    break;
                case MovementAxes.XY:
                    m_movementVector = new Vector3(inputDirection.x, inputDirection.y, 0);
                    break;
                case MovementAxes.YZ:
                    m_movementVector = new Vector3(0, inputDirection.y, inputDirection.x);
                    break;
                case MovementAxes.Custom:
                    m_movementVector = CustomMovementAxesRight * inputDirection.x + CustomMovementAxesForward * inputDirection.y;
                    break;
            }

            m_movementVector.Normalize();
        }

        public override Vector3 VelocityUpdate(Vector3 currentVelocity, float deltaTime)
        {
            switch (m_accelerationApplication)
            {
                case VelocityProcessing.FromRawInput:
                    currentVelocity += m_movementVector * m_maxSpeed;
                    break;

                case VelocityProcessing.FromAcceleration:
                    Vector3 acceleration = m_movementVector * m_maxAcceleration;
                    currentVelocity += m_acceleration * m_maxSpeed * deltaTime;
                    break;

                case VelocityProcessing.DesiredVelocityFromAcceleration:
                    Vector3 desiredVelocity = m_movementVector * m_maxSpeed;
                    float maxSpeedChange = m_maxAcceleration * deltaTime;

                    currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, desiredVelocity.x, maxSpeedChange);
                    currentVelocity.z = Mathf.MoveTowards(currentVelocity.z, desiredVelocity.z, maxSpeedChange);
                    break;
            }

            m_movementVector = Vector3.zero;

            return currentVelocity;
        }
    }

    [Serializable]
    public class AtelierCharacterGravity : AtelierCharacterMovement
    {
        [SerializeField]
        private float m_gravityMultiplier = 1;
        private float m_verticalVelocity = 0;

        private bool m_wasGroundedLastFrame = true;

        public override void StateUpdate(bool grounded)
        {
            if (grounded)
            {
                m_verticalVelocity = 0;
            }

            grounded = m_wasGroundedLastFrame;
        }

        public bool EvaluateState()
        {
            return true;
        }

        public override Vector3 VelocityUpdate(Vector3 currentVelocity, float deltaTime)
        {
            m_verticalVelocity += Physics.gravity.y * m_gravityMultiplier * Time.deltaTime;
            currentVelocity.y += m_verticalVelocity;
            return currentVelocity;
        }
    }

    [Serializable]
    public class AtelierCharacterJump : AtelierCharacterMovement
    {
        [SerializeField]
        private float m_jumpHeight = 20;
        [SerializeField]
        private int m_jumpCount = 1;

        private int m_currentJumpCount = 0;
        private bool m_wasGroundedLastFrame = true;
        private bool m_canJump = true;
        private bool m_wantToJump = false;

        public void DoJump()
        {
            if (m_canJump)
            {
                m_wantToJump = true;
            }
        }

        private Vector3 Jump()
        {
            if (!m_wantToJump)
            {
                return Vector3.zero;
            }

            m_canJump = ++m_currentJumpCount < m_jumpCount;
            m_wantToJump = false;

            return ModuleOwner.transform.up * (m_jumpHeight * -Physics.gravity.y);
        }

        public override void StateUpdate(bool grounded)
        {
            if (grounded)
            {
                m_canJump = true;
                m_currentJumpCount = 0;
            }

            grounded = m_wasGroundedLastFrame;
        }

        public bool EvaluateState()
        {
            return true;
        }

        public override Vector3 VelocityUpdate(Vector3 currentVelocity, float deltaTime)
        {
            return currentVelocity + Jump();
        }
    }


    // Process RotateInput and StickAim inputDirection to rotate the character
    [Serializable]
    public class AtelierCharacterInputDrivenRotation : AtelierCharacterRotation
    {
        [Serializable]
        public enum RotationAxis
        {
            X,
            Y,
            Z
        }

        [SerializeField]
        private RotationAxis m_rotationAxis = RotationAxis.Y;

        private Vector3 m_lastDirection;

        public override void RotateInput(Vector3 normalizedDirection)
        {
            m_lastDirection = normalizedDirection;
        }

        public override void RotationUpdate(float deltaTime)
        {
            ModuleOwner.transform.rotation = TowDownDirectionToQuaternion(m_lastDirection);
        }

        private Quaternion TowDownDirectionToQuaternion(Vector3 normalizedDirection)
        {
            switch (m_rotationAxis)
            {
                case RotationAxis.X:
                    return Quaternion.Euler(new Vector3(Mathf.Atan2(normalizedDirection.x, normalizedDirection.y) * Mathf.Rad2Deg, 0, 0));
                case RotationAxis.Y:
                    return Quaternion.Euler(new Vector3(0, Mathf.Atan2(normalizedDirection.x, normalizedDirection.y) * Mathf.Rad2Deg, 0));
                case RotationAxis.Z:
                    return Quaternion.Euler(new Vector3(0, 0, Mathf.Atan2(normalizedDirection.x, normalizedDirection.y) * Mathf.Rad2Deg));
            }

            return Quaternion.identity;
        }
    }

    [Serializable]
    public class AtelierCharacterVelocityDrivenRotation : AtelierCharacterRotation
    {
        [SerializeField]
        protected float m_rotationSpeed;

        protected void SetForward(Vector3 dir, float stepSpeed)
        {
            ModuleOwner.transform.forward = Vector3.Slerp(ModuleOwner.transform.forward, dir, stepSpeed);
        }

        public override void RotationUpdate(float deltaTime)
        {
            SetForward(ModuleOwner.GetMoveVector(), m_rotationSpeed * deltaTime);
        }

    }

    [Serializable]
    public class AtelierCharacterRotateTowardTarget : AtelierCharacterVelocityDrivenRotation
    {
        [SerializeField]
        private Transform m_target;

        public override void RotationUpdate(float deltaTime)
        {
            var dir = (m_target.position - ModuleOwner.Position).normalized;

            SetForward(dir, m_rotationSpeed);
        }

        public override bool CanBeExecuted()
        {
            return m_target != null;
        }
    }

    // Unity pawn
    // Use Unity CharacterController to handle move.
    public class AtelierCharacter : Character
    {
        protected UnityEngine.CharacterController m_movement;
        protected UnityEngine.Rigidbody m_body;

        // [SerializeField]
        private CharacterMovementModule[] m_modules;

        // Execute all of them in priority order
        private List<AtelierCharacterMovement> m_modules_concept;

        [Header("Movement")]
        [SerializeField]
        AtelierCharacter2DMovement m_pawnMovementModule2D;
        [SerializeField]
        private bool m_useGravity = false;
        [SerializeField]
        private AtelierCharacterGravity m_pawnMovementModuleGravity;
        [SerializeField]
        private bool m_canJump = false;
        [SerializeField]
        private AtelierCharacterJump m_pawnMovementModuleJump;


        [Header("Rotation")]
        [SerializeField]
        private bool m_inputDrivenRotation = true;

        [SerializeField]
        private AtelierCharacterInputDrivenRotation m_inputDrivenRotationModule;
        [SerializeField]
        private AtelierCharacterVelocityDrivenRotation m_velocityDrivenRotationModule;
        [SerializeField]
        private AtelierCharacterRotateTowardTarget m_rotateTowardTargetModule;

        // Evaluate and only execute the best module
        private List<AtelierCharacterRotation> m_rotationModule_Concepts;

        private Vector3 m_lastMoveDir;

        private AtelierCharacterRotation GetBestRotationModule()
        {
            if (m_rotationModule_Concepts == null)
            {
                return null;
            }

            int bestPriority = 0;
            AtelierCharacterRotation bestModule = null;
            for (int i = 0, c = m_rotationModule_Concepts.Count; i < c; i++)
            {
                if (m_rotationModule_Concepts[i].CanBeExecuted() && m_rotationModule_Concepts[i].Priority > bestPriority)
                {
                    bestModule = m_rotationModule_Concepts[i];
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

        public T GetModule_Concept<T>() where T : AtelierCharacterModule
        {
            foreach (var module in m_modules_concept)
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

        public override void Move(Vector3 direction, float deltaTime)
        {
            m_pawnMovementModule2D.MoveInput(direction);
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

            m_modules_concept = new List<AtelierCharacterMovement>
            {
                //m_inputDrivenRotationModule,
                //m_velocityDrivenRotationModule,
                m_pawnMovementModuleJump,
                m_pawnMovementModuleGravity,
                m_pawnMovementModule2D
            };

            m_rotationModule_Concepts = new List<AtelierCharacterRotation>
            {
                m_inputDrivenRotationModule,
                m_velocityDrivenRotationModule,
                m_rotateTowardTargetModule
            };
        }

        private void Start()
        {
            m_inputDrivenRotationModule.ModuleInit(this);
            m_rotateTowardTargetModule.ModuleInit(this);
            m_velocityDrivenRotationModule.ModuleInit(this);
            m_pawnMovementModuleJump.ModuleInit(this);
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

            Vector3 currentVel = Vector3.zero;
            currentVel = m_pawnMovementModule2D.VelocityUpdate(currentVel, deltaTime);
            if (m_useGravity)
            {
                m_pawnMovementModuleGravity.StateUpdate(m_movement.isGrounded);
                currentVel = m_pawnMovementModuleGravity.VelocityUpdate(currentVel, deltaTime);
            }
            if (m_canJump)
            {
                m_pawnMovementModuleJump.StateUpdate(m_movement.isGrounded);
                currentVel = m_pawnMovementModuleJump.VelocityUpdate(currentVel, deltaTime);
            }

            m_lastMoveDir = currentVel.normalized;
            // CharacterController.Move uses deltaPosition.
            m_movement.Move(currentVel * deltaTime);

            // CharacterController.SimpleMove uses units/s.
            // m_movement.SimpleMove(currentVel);

            // Rigidbody gave more control but might not be useful for more simpler movement...
        }
    }
}