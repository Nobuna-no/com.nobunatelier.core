using Codice.Client.Common.GameUI;
using NobunAtelier;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class CharacterGaitVelocity : CharacterVelocityModuleBase, ContextualLogManager.IStateProvider
{
    private const float kDelayBeforeInferringJump = 0.3f;

    [Header("Gait")]
    [Tooltip("Ground speed when walking")]
    [SerializeField] private float Speed = 1f;
    [Tooltip("Ground speed when sprinting")]
    [SerializeField] private float SprintSpeed = 4;
    [Tooltip("Ground speed when crouching")]
    [SerializeField] private float CrouchSpeed = .7f;
    [SerializeField] private UnityEvent StartCrouch;
    [SerializeField] private UnityEvent EndCrouch;
    [Tooltip("Initial vertical speed when jumping")]
    [SerializeField] private float JumpSpeed = 4;
    [Tooltip("Initial vertical speed when sprint-jumping")]
    [SerializeField] private float SprintJumpSpeed = 6;
    [SerializeField] private UnityEvent StartJump;
    [SerializeField] private float DelayBetweenJumpPressedAndExecution = 0.1f;

    // [Header("Gravity")]
    // [Tooltip("Force of gravity in the down direction (m/s/s)")]
    // [SerializeField] private float Gravity = 10;
    [Tooltip("This event is sent when the player lands after a jump.")]
    [SerializeField] private UnityEvent Landed = new();

    [Header("Camera")]
    [Tooltip("If true, player will strafe when moving sideways, otherwise will turn to face direction of motion")]
    public bool m_Strafe = false;
    [Tooltip("Override the main camera. Useful for split screen games.")]
    public Camera CameraOverride;

    [Header("Polish")]
    [Tooltip("How long it takes for the player to change velocity")]
    [SerializeField] private float m_Damping = 0.5f;

    public Camera Camera => CameraOverride == null ? Camera.main : CameraOverride;

    [SerializeField]
    private ContextualLogManager.LogSettings m_LogSettings;
    public ContextualLogManager.LogPartition Log { get; private set; }

    public string LogPartitionName => null;

    public string GetStateMessage()
    {
        return $"Action requested: {m_gaitState}; IsGrounded: {IsGrounded};" +
            $"IsJumping: {m_IsJumping}";
    }

    public bool IsGrounded { get; private set; } = true;
    public bool Strafe
    {
        get => m_Strafe;
        set => m_Strafe = value;
    }

    private Vector3 m_CurrentVelocityXZ;
    private Vector3 m_LastInput;
    private float m_CurrentVelocityY;
    private bool m_IsSprinting;
    private bool m_IsCrouching;
    private bool m_IsJumping;

    private ActionRequestType m_gaitState = 0;

    bool m_InTopHemisphere = true;
    float m_TimeInHemisphere = 100;
    Vector3 m_LastRawInput;
    Quaternion m_Upsidedown = Quaternion.AngleAxis(180, Vector3.left);

    [System.Flags]
    private enum ActionRequestType
    {
        Walk = 1 << 0,
        Jump = 1 << 1,
        Sprint = 1 << 2,
        Crouch = 1 << 3,
    }

    public override bool CanBeExecuted()
    {
        return base.CanBeExecuted();
    }

    public override void ModuleInit(Character character)
    {
        base.ModuleInit(character);

        m_CurrentVelocityY = 0;
        m_IsSprinting = false;
        m_IsCrouching = false;
        m_IsJumping = false;
    }

    public override void StateUpdate(bool grounded)
    {
        IsGrounded = grounded;
        Log.Record(ContextualLogManager.LogTypeFilter.Update);
    }

    public override Vector3 VelocityUpdate(Vector3 currentVel, float deltaTime)
    {
        // Process Jump
        bool justLanded =  ProcessJump();

        Vector3 rawInput = new Vector3(LastMoveDirection.x, 0, LastMoveDirection.z);
        var inputFrame = GetInputFrame(Vector3.Dot(rawInput, m_LastRawInput) < 0.8f);
        m_LastRawInput = rawInput;
        LastMoveDirection = Vector3.zero;

        // Read the input from the user and put it in the input frame
        m_LastInput = inputFrame * rawInput;
        if (m_LastInput.sqrMagnitude > 1)
            m_LastInput.Normalize();

        // Compute the new velocity and move the player, but only if not mid-jump
        if (!m_IsJumping && !m_jumpPending)
        {
            m_IsSprinting = (m_gaitState & ActionRequestType.Sprint) != 0;
            m_IsCrouching = (m_gaitState & ActionRequestType.Crouch) != 0;
            float activeSpeed = m_IsCrouching ? CrouchSpeed : (m_IsSprinting ? SprintSpeed : Speed);
            var desiredVelocity = m_LastInput * activeSpeed;
            var damping = justLanded ? 0 : m_Damping;
            if (Vector3.Angle(m_CurrentVelocityXZ, desiredVelocity) < 100)
            {
                m_CurrentVelocityXZ = Vector3.Slerp(
                    m_CurrentVelocityXZ, desiredVelocity,
                    Damper.Damp(1, damping, Time.deltaTime));
            }
            else
            {
                m_CurrentVelocityXZ += Damper.Damp(
                    desiredVelocity - m_CurrentVelocityXZ, damping, Time.deltaTime);
            }
        }

        currentVel = m_CurrentVelocityXZ;
        currentVel.y = m_CurrentVelocityY;
        m_CurrentVelocityY = 0;

        return currentVel;
    }

    public void StartSprint()
    {
        m_gaitState |= ActionRequestType.Sprint;
    }

    public void StopSprint()
    {
        m_gaitState &= ~ActionRequestType.Sprint;
    }

    public void ToggleCrouch()
    {
        if (m_IsJumping || m_jumpPending)
        {
            return;
        }

        if ((m_gaitState & ActionRequestType.Crouch) != 0)
        {
            m_gaitState &= ~ActionRequestType.Crouch;
            EndCrouch?.Invoke();
        }
        else
        {
            m_gaitState |= ActionRequestType.Crouch;
            StartCrouch?.Invoke();
        }
    }

    public void DoJump()
    {
        if (m_IsJumping || m_jumpPending)
        {
            return;
        }

        if ((m_gaitState & ActionRequestType.Crouch) != 0)
        {
            ToggleCrouch();
        }

        m_gaitState |= ActionRequestType.Jump;
    }

    private bool m_jumpPending = false;
    private float m_jumpRemainingTime = 0f;
    private bool ProcessJump()
    {
        bool justLanded = false;
        bool grounded = IsGrounded;

        if (m_jumpPending)
        {
            m_jumpRemainingTime -= Time.deltaTime;
            if (m_jumpRemainingTime <= 0)
            {
                grounded = false;
                // Use default jumpSpeed if crouched.
                m_CurrentVelocityY = m_IsSprinting ? SprintJumpSpeed : JumpSpeed;
                m_gaitState &= ~ActionRequestType.Jump;
                m_IsJumping = true;
                m_jumpPending = false;
            }
        }
        else if (!m_IsJumping)
        {
            // Process jump command
            if (grounded && (m_gaitState & ActionRequestType.Jump) != 0)
            {
                StartJump?.Invoke();
                m_gaitState &= ~ActionRequestType.Jump;
                m_jumpPending = true;
                m_jumpRemainingTime = DelayBetweenJumpPressedAndExecution;
                // StartCoroutine(StartJump_Routine());
            }
        }

        if (grounded)
        {
            m_CurrentVelocityY = 0;

            // If we were jumping, complete the jump
            if (m_IsJumping)
            {
                Log.Record("Land");

                m_IsJumping = false;
                justLanded = true;
                Landed?.Invoke();
            }
        }
        return justLanded;
    }

    // Get the reference frame for the input.  The idea is to map camera fwd/right
    // to the player's XZ plane.  There is some complexity here to avoid
    // gimbal lock when the player is tilted 180 degrees relative to the camera.
    private Quaternion GetInputFrame(bool inputDirectionChanged)
    {
        // Get the raw input frame, depending of forward mode setting
        var frame = Camera.transform.rotation;

        // Map the raw input frame to something that makes sense as a direction for the player
        var playerUp = transform.up;
        var up = frame * Vector3.up;

        // Is the player in the top or bottom hemisphere?  This is needed to avoid gimbal lock
        const float BlendTime = 2f;
        m_TimeInHemisphere += Time.deltaTime;
        bool inTopHemisphere = Vector3.Dot(up, playerUp) >= 0;
        if (inTopHemisphere != m_InTopHemisphere)
        {
            m_InTopHemisphere = inTopHemisphere;
            m_TimeInHemisphere = Mathf.Max(0, BlendTime - m_TimeInHemisphere);
        }

        // If the player is untilted relative to the input frame, then early-out with a simple LookRotation
        var axis = Vector3.Cross(up, playerUp);
        if (axis.sqrMagnitude < 0.001f && inTopHemisphere)
            return frame;

        // Player is tilted relative to input frame: tilt the input frame to match
        var angle = UnityVectorExtensions.SignedAngle(up, playerUp, axis);
        var frameA = Quaternion.AngleAxis(angle, axis) * frame;

        // If the player is tilted, then we need to get tricky to avoid gimbal-lock
        // when player is tilted 180 degrees.  There is no perfect solution for this,
        // we need to cheat it :/
        Quaternion frameB = frameA;
        if (!inTopHemisphere || m_TimeInHemisphere < BlendTime)
        {
            // Compute an alternative reference frame for the bottom hemisphere.
            // The two reference frames are incompatible where they meet, especially
            // when player up is pointing along the X axis of camera frame.
            // There is no one reference frame that works for all player directions.
            frameB = frame * m_Upsidedown;
            var axisB = Vector3.Cross(frameB * Vector3.up, playerUp);
            if (axisB.sqrMagnitude > 0.001f)
                frameB = Quaternion.AngleAxis(180f - angle, axisB) * frameB;
        }
        // Blend timer force-expires when user changes input direction
        if (inputDirectionChanged)
            m_TimeInHemisphere = BlendTime;

        // If we have been long enough in one hemisphere, then we can just use its reference frame
        if (m_TimeInHemisphere >= BlendTime)
            return inTopHemisphere ? frameA : frameB;

        // Because frameA and frameB do not join seamlessly when player Up is along X axis,
        // we blend them over a time in order to avoid degenerate spinning.
        // This will produce weird movements occasionally, but it's the lesser of the evils.
        if (inTopHemisphere)
            return Quaternion.Slerp(frameB, frameA, m_TimeInHemisphere / BlendTime);
        return Quaternion.Slerp(frameA, frameB, m_TimeInHemisphere / BlendTime);
    }

    private IEnumerator StartJump_Routine()
    {
        yield return new WaitForSeconds(DelayBetweenJumpPressedAndExecution);
        // Use default jumpSpeed if crouched.
        m_CurrentVelocityY = m_IsSprinting ? SprintJumpSpeed : JumpSpeed;
        m_IsJumping = true;
    }

    private void OnEnable()
    {
        Log = ContextualLogManager.Register(this, m_LogSettings);
    }

    private void OnDisable()
    {
        ContextualLogManager.Unregister(Log);
    }
}
