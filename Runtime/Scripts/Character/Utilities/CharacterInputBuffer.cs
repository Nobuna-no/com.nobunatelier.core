using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Utility class to handle input buffer timing for character actions
    /// </summary>
    public class CharacterInputBuffer
    {
        private float m_RequestTime = -1f;
        private float m_BufferDuration;

        /// <summary>
        /// Creates a new input buffer with specified duration
        /// </summary>
        /// <param name="bufferDuration">Duration in seconds the input request stays valid</param>
        public CharacterInputBuffer(float bufferDuration)
        {
            m_BufferDuration = bufferDuration;
        }

        /// <summary>
        /// Records an input request at the current time
        /// </summary>
        public void RequestAction()
        {
            m_RequestTime = Time.time;
        }

        /// <summary>
        /// Checks if an input request is active within the buffer time window
        /// </summary>
        /// <returns>True if a request is active within the buffer time window</returns>
        public bool HasActiveRequest()
        {
            return m_RequestTime >= 0 && (Time.time - m_RequestTime <= m_BufferDuration);
        }

        /// <summary>
        /// Consumes the active request, resetting the timer
        /// </summary>
        public void ConsumeRequest()
        {
            m_RequestTime = -1f;
        }

        /// <summary>
        /// Cancels the active request without consuming it
        /// </summary>
        public void CancelRequest()
        {
            m_RequestTime = -1f;
        }
    }
} 