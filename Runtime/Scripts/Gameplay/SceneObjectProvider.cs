using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Registers this GameObject with the <see cref="SceneObjectRegistry"/> under a <see cref="SceneObjectID"/>.
    /// Place on scene-level objects that need to be looked up by data-driven systems
    /// (e.g., cameras, lights, post-processing volumes).
    /// </summary>
    public class SceneObjectProvider : MonoBehaviour
    {
        [SerializeField] private SceneObjectID m_ID;

        private void OnEnable()
        {
            if (m_ID != null)
            {
                SceneObjectRegistry.Register(m_ID, gameObject);
            }
        }

        private void OnDisable()
        {
            if (m_ID != null)
            {
                SceneObjectRegistry.Unregister(m_ID);
            }
        }
    }
}
