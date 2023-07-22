using Unity.Cinemachine;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    public class ThirdPersonCharacterController : LegacyPlayerControllerBase
    {
        private const string GamepadControlSchemeName = "Gamepad";
        private const string KeyboardAndMouseControlSchemeName = "Keyboard&Mouse";

        [Header("Third Person")]
        [SerializeField, Required]
        private Transform m_cameraTarget;

        [Header("Camera")]
        [SerializeField]
        private float m_minTiltAngleDegree = -20f;

        [SerializeField]
        private float m_maxTiltAngleDegree = 60f;

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

#if UNITY_EDITOR

        [Header("Debug")]
        [SerializeField]
        private bool m_debugEnabled = false;

        [SerializeField, ShowIf("m_debugEnabled")]
        private float m_debugDrawDistance = 5;

#endif

        private InputAction m_moveAction;
        private InputAction m_lookAction;

        private Vector2 m_lastMoveInputValue;
        private Vector2 m_lastLookInputValue;

        protected override void Awake()
        {
            base.Awake();

            Debug.Assert(m_cameraTarget != null, $"{this} has no camera assigned.");
        }

        public override void EnableInput()
        {
            base.EnableInput();

            m_moveAction = ActiveActionMap.FindAction("Move");
            m_lookAction = ActiveActionMap.FindAction("Look");
        }

        protected override void ControllerUpdate()
        {
            CameraRotationUpdate();

            CharacterMovementUpdate();
        }

        public void CursorLockAndHide(bool enable)
        {
            if (enable)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        protected virtual void OnEnable()
        {
            if (m_moveAction != null)
            {
                m_moveAction.performed += OnMoveActionPerformed;
                m_moveAction.canceled += OnMoveActionCanceled;
            }

            if (m_lookAction != null)
            {
                m_lookAction.performed += OnLookActionPerformed;
                m_lookAction.canceled += OnLookActionCancelled;
            }
            CursorLockAndHide(true);
        }

        protected virtual void OnDisable()
        {
            if (m_moveAction != null)
            {
                m_moveAction.performed -= OnMoveActionPerformed;
                m_moveAction.canceled -= OnMoveActionCanceled;
            }

            if (m_lookAction != null)
            {
                m_lookAction.performed -= OnLookActionPerformed;
                m_lookAction.canceled -= OnLookActionCancelled;
            }

            CursorLockAndHide(false);
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

            eulerRot.x = Mathf.Clamp(eulerRot.x, m_minTiltAngleDegree, m_maxTiltAngleDegree);
            m_cameraTarget.rotation = Quaternion.Euler(eulerRot);
        }

        private void CharacterMovementUpdate()
        {
            if (m_lastMoveInputValue != Vector2.zero)
            {
                Vector3 inputDir = new Vector3(m_lastMoveInputValue.x, 0, m_lastMoveInputValue.y);

                Vector3 forward = m_cameraTarget.forward;
                Vector3 right = m_cameraTarget.right;
                forward.y = 0;
                right.y = 0;

                // Move inputDirection to camera space: https://www.youtube.com/watch?v=7j5yW5QDC2U
                Vector3 dest = right * inputDir.x + forward * inputDir.z;
                m_controlledCharacter.Move(dest);

#if UNITY_EDITOR
                if (m_debugEnabled)
                {
                    Debug.DrawRay(transform.position, forward * m_debugDrawDistance, Color.blue);
                    Debug.DrawRay(transform.position, right * m_debugDrawDistance, Color.red);
                    Debug.DrawRay(transform.position, inputDir * m_debugDrawDistance, Color.grey);
                    Debug.DrawRay(transform.position, dest * m_debugDrawDistance, Color.yellow);
                }
#endif
            }
        }

        private void OnMoveActionPerformed(InputAction.CallbackContext obj)
        {
            m_lastMoveInputValue = obj.ReadValue<Vector2>();
        }

        private void OnMoveActionCanceled(InputAction.CallbackContext obj)
        {
            m_lastMoveInputValue = Vector2.zero;
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
    }
}