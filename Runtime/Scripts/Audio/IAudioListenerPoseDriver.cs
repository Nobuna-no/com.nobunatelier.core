using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Drives the persistent <see cref="AudioManager"/> listener transform each frame.
    /// </summary>
    public interface IAudioListenerPoseDriver
    {
        void ApplyListenerPose(Transform listenerTransform);
    }
}
