using NaughtyAttributes;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

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

        public override void UpdateModule(float deltaTime)
        {
            CameraRotationUpdate();
        }

        public override void EnableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            m_lookAction = activeActionMap.FindAction("Look");
            Debug.Assert(m_lookAction != null, $"[{Time.frameCount}] {this}: Look action was not found in activeActionMap `{activeActionMap}`");
            m_lookAction.performed += OnLookActionPerformed;
            m_lookAction.canceled += OnLookActionCancelled;
        }

        public override void DisableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            if (m_lookAction != null)
            {
                m_lookAction.performed -= OnLookActionPerformed;
                m_lookAction.canceled -= OnLookActionCancelled;
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