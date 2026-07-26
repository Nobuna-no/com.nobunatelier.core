using System;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Lightweight identity token for gameplay events.
    /// </summary>
    public class GameplayEventDefinition : DataDefinition
    {
#if UNITY_EDITOR
        [SerializeField] private string m_Description;
#endif

        /// <summary>
        /// Fired when any <see cref="GameplayEventDefinition"/> is raised via <see cref="Raise"/>.
        /// Scene components such as <see cref="GameplayEventListener"/> subscribe here.
        /// </summary>
        public static event Action<GameplayEventDefinition> Raised;

        /// <summary>
        /// Broadcast this event to global subscribers and scene <see cref="GameplayEventListener"/> bindings.
        /// </summary>
        public void Raise()
        {
            Debug.Log("Raised", this);
            Raised?.Invoke(this);
        }
    }
}
