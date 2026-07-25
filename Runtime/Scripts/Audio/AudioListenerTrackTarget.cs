using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Audio/Audio Listener Track Target")]
    public class AudioListenerTrackTarget : MonoBehaviour, IAudioListenerPoseDriver
    {
        [Header("Targets")]
        [SerializeField]
        private Transform m_CameraTransform;

        [SerializeField]
        private Transform m_CharacterTransform;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("0 = camera position, 1 = character position.")]
        private float m_PositionBlend = 0.5f;

        [SerializeField]
        [Tooltip("When enabled, listener rotation matches the camera.")]
        private bool m_UseCameraRotation = true;

        public void ApplyListenerPose(Transform listenerTransform)
        {
            if (!m_CameraTransform || !m_CharacterTransform)
            {
                return;
            }

            Vector3 position = Vector3.Lerp(m_CameraTransform.position, m_CharacterTransform.position, m_PositionBlend);
            Quaternion rotation = m_UseCameraRotation ? m_CameraTransform.rotation : listenerTransform.rotation;
            listenerTransform.SetPositionAndRotation(position, rotation);
        }

        private void OnEnable()
        {
            if (!AudioManager.IsSingletonValid)
            {
                return;
            }

            AudioManager.Instance.ListenerTrackTarget(this);
        }

        private void OnDisable()
        {
            if (!AudioManager.IsSingletonValid)
            {
                return;
            }

            AudioManager.Instance.ReleaseListenerTrackTarget(this);
        }

#if UNITY_EDITOR
        [Button("Assign From Main Camera And Player Tag")]
        private void AssignFromMainCameraAndPlayerTag()
        {
            if (Camera.main)
            {
                m_CameraTransform = Camera.main.transform;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                m_CharacterTransform = player.transform;
            }
        }
#endif
    }
}
