using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Controller/Player/Player Controller Module: Third Person Movement")]
    public class PlayerControllerThirdPersonMovement : PlayerControllerModuleBase
    {
        [Header("Third Person Movement")]
        [SerializeField, Required]
        private Transform m_cameraTarget;

        private InputAction m_moveAction;
        private Vector2 m_lastMoveInputValue;
        public Vector2 LastMoveInput => m_lastMoveInputValue;

#if UNITY_EDITOR

        [Header("Debug")]
        [SerializeField]
        private bool m_debugEnabled = false;

        [SerializeField, ShowIf("m_debugEnabled")]
        private float m_debugDrawDistance = 5;

#endif

        public override void UpdateModule(float deltaTime)
        {
            CharacterMovementUpdate();
        }

        public override void EnableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            m_moveAction = activeActionMap.FindAction("Move");
            Debug.Assert(m_moveAction != null, $"[{Time.frameCount}] {this}: Move action was not found in activeActionMap `{activeActionMap}`");
            m_moveAction.performed += OnMoveActionPerformed;
            m_moveAction.canceled += OnMoveActionCanceled;
        }

        public override void DisableModuleInput(PlayerInput playerInput, InputActionMap activeActionMap)
        {
            m_moveAction.performed -= OnMoveActionPerformed;
            m_moveAction.canceled -= OnMoveActionCanceled;
        }

        public void SetCameraTarget(ITargetable target)
        {
            m_cameraTarget = target.TargetTransform;
        }

        public void SetCameraTarget(Transform cameraTarget)
        {
            m_cameraTarget = cameraTarget;
        }

        private void CharacterMovementUpdate()
        {
            if (m_lastMoveInputValue == Vector2.zero)
            {
                return;
            }

            Vector3 inputDir = new Vector3(m_lastMoveInputValue.x, 0, m_lastMoveInputValue.y);

            Vector3 forward = m_cameraTarget.forward;
            Vector3 right = m_cameraTarget.right;
            forward.y = 0;
            right.y = 0;

            // Move inputDirection to camera space: https://www.youtube.com/watch?v=7j5yW5QDC2U
            Vector3 dest = right * inputDir.x + forward * inputDir.z;
            ControlledCharacter.Move(dest);

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

        private void OnMoveActionPerformed(InputAction.CallbackContext obj)
        {
            m_lastMoveInputValue = obj.ReadValue<Vector2>();
        }

        private void OnMoveActionCanceled(InputAction.CallbackContext obj)
        {
            m_lastMoveInputValue = Vector2.zero;
        }
    }
}