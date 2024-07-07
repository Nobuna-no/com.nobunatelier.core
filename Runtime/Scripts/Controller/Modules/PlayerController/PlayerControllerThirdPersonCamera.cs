using NaughtyAttributes;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using static NobunAtelier.CharacterThirdPersonAim;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Controller/Player/Player Controller Module: Third Person Camera")]
    public class PlayerControllerThirdPersonCamera : PlayerControllerModuleBase
    {
        private const string GamepadControlSchemeName = "Gamepad";
        private const string KeyboardAndMouseControlSchemeName = "Keyboard&Mouse";


        [Header("Third Person Camera")]
        [SerializeField, Required]
        private Transform m_cameraTarget;

        [SerializeField, MinMaxSlider(-85f, 85f)]
        private Vector2 m_TiltAngleDegreeRange = new Vector2(-20, 60);


        [Tooltip("How the player's rotation is coupled to the camera's rotation.  Three modes are available:\n"
            + "<b>Coupled</b>: The player rotates with the camera.  Sideways movement will result in strafing.\n"
            + "<b>Coupled When Moving</b>: Camera can rotate freely around the player when the player is stationary, "
                + "but the player will rotate to face camera forward when it starts moving.\n"
            + "<b>Decoupled</b>: The player's rotation is independent of the camera's rotation.")]
        public CouplingMode PlayerRotation;
        [Tooltip("How fast the player rotates to face the camera direction when the player starts moving.  "
            + "Only used when Player Rotation is Coupled When Moving.")]
        public float RotationDamping = 0.2f;
        // [SerializeField]
        // private float m_maxTiltAngleDegree = 60f;

        // Maybe we can later move that into some kind of Scriptable object profile and save per user...
        [Header("Mouse Settings")]
        [SerializeField]
        private float m_mouseCameraHorizontalSpeed = 10f;

        [SerializeField]
        private float m_mouseCameraVerticalSpeed = 5f;

        [Header("Gamepad Settings")]
        [SerializeField]
        private AnimationCurve m_gamepadInputResponseCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [SerializeField]
        private float m_gamepadCameraHorizontalSpeed = 10f;

        [SerializeField]
        private float m_gamepadCameraVerticalSpeed = 3f;

        private InputAction m_lookAction;

        private Vector2 m_lastLookInputValue;
        private Quaternion m_DesiredWorldRotation;
        private Vector2 m_currentLookInputValue;
        private Vector2 m_lookInputDelta;

        public override void UpdateModule(float deltaTime)
        {
            CameraRotationUpdate();
            UpdatePlayerRotation(deltaTime);
        }

        public override void EnableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            m_lookAction = activeActionMap.FindAction("Look");
            Debug.Assert(m_lookAction != null, $"[{Time.frameCount}] {this}: Look action was not found in activeActionMap `{activeActionMap}`");
            m_lookAction.performed += OnLookActionPerformed;
            m_lookAction.canceled += OnLookActionCancelled;

            if (ControlledCharacter)
            {
                ControlledCharacter.OnPostUpdate += PostUpdate;
            }
        }

        public override void DisableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            if (m_lookAction != null)
            {
                m_lookAction.performed -= OnLookActionPerformed;
                m_lookAction.canceled -= OnLookActionCancelled;
            }

            if (ControlledCharacter)
            {
                ControlledCharacter.OnPostUpdate -= PostUpdate;
            }

            m_lookAction = null;
        }

        private void CameraRotationUpdate()
        {
            if (PlayerInput.currentControlScheme == KeyboardAndMouseControlSchemeName)
            {
                Vector2 inputDir = Mouse.current.delta.ReadValue();
                m_lastLookInputValue.x = inputDir.y * m_mouseCameraVerticalSpeed;
                m_lastLookInputValue.y = inputDir.x * m_mouseCameraHorizontalSpeed;
                m_cameraTarget.rotation = UnityQuaternionExtensions.ApplyCameraRotation(m_cameraTarget.rotation, m_lastLookInputValue * Time.smoothDeltaTime, Vector3.up);
            }
            else
            {
                m_cameraTarget.rotation = UnityQuaternionExtensions.ApplyCameraRotation(m_cameraTarget.rotation, m_lastLookInputValue, Vector3.up);
            }

            var eulerRot = m_cameraTarget.rotation.eulerAngles;
            if (eulerRot.x > 180)
            {
                eulerRot.x -= 360;
            }
            eulerRot.z = 0;

            eulerRot.x = Mathf.Clamp(eulerRot.x, m_TiltAngleDegreeRange.x, m_TiltAngleDegreeRange.y);
            m_cameraTarget.rotation = Quaternion.Euler(eulerRot);
            m_DesiredWorldRotation = m_cameraTarget.rotation;
        }

        private void OnLookActionPerformed(InputAction.CallbackContext obj)
        {
            var val = obj.ReadValue<Vector2>();

            if (PlayerInput.currentControlScheme == GamepadControlSchemeName)
            {
                m_lastLookInputValue.x = m_gamepadInputResponseCurve.Evaluate(Mathf.Abs(val.y)) * Mathf.Sign(val.y) * m_gamepadCameraVerticalSpeed;
                m_lastLookInputValue.y = m_gamepadInputResponseCurve.Evaluate(Mathf.Abs(val.x)) * Mathf.Sign(val.x) * m_gamepadCameraHorizontalSpeed;
            }
        }

        private void OnLookActionCancelled(InputAction.CallbackContext obj)
        {
            m_lastLookInputValue = Vector2.zero;
        }

        private void UpdatePlayerRotation(float deltaTime)
        {
            // var t = m_cameraTarget;
            // t.localRotation = Quaternion.Euler(m_currentLookInputValue.y, m_currentLookInputValue.x, 0);
            // m_DesiredWorldRotation = t.rotation;
            switch (PlayerRotation)
            {
                case CouplingMode.Coupled:
                    RecenterPlayer(deltaTime);
                    break;
                case CouplingMode.CoupledWhenMoving:
                    if (ControlledCharacter.IsMoving)
                        RecenterPlayer(deltaTime, RotationDamping);
                    break;
                case CouplingMode.Decoupled:
                    break;
            }
        }

        private void PostUpdate()
        {
            if (PlayerRotation == CouplingMode.Decoupled)
            {
                ControlledCharacter.Transform.rotation = m_DesiredWorldRotation;
                // var delta = (Quaternion.Inverse(m_cameraTarget.rotation) * m_DesiredWorldRotation).eulerAngles;
                // m_currentLookInputValue.y = NormalizeAngle(delta.x);
                // m_currentLookInputValue.x = NormalizeAngle(delta.y);
            }
        }

        private void RecenterPlayer(float deltaTime, float damping = 0)
        {
            if (m_cameraTarget == null)
                return;

            // Get my rotation relative to parent
            var rot = m_cameraTarget.localRotation.eulerAngles;
            rot.y = NormalizeAngle(rot.y);
            var delta = rot.y;
            delta = Damper.Damp(delta, damping, deltaTime);

            // Rotate the parent towards me
            var t = ControlledCharacter.Transform;
            t.rotation = Quaternion.AngleAxis(delta, t.up) * t.rotation;

            // m_cameraTarget.rotation = Quaternion.AngleAxis(delta, m_cameraTarget.up) * m_cameraTarget.rotation;

            // Rotate me in the opposite direction
            // m_currentLookInputValue.y -= delta;
            rot.y -= delta;
            m_cameraTarget.localRotation = Quaternion.Euler(rot);
        }

        private float NormalizeAngle(float angle)
        {
            while (angle > 180)
                angle -= 360;
            while (angle < -180)
                angle += 360;
            return angle;
        }

    }
}