using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Lightweight identity token for gameplay events.
    /// Replaces Animancer's StringAsset as event identity within the ability system,
    /// decoupling event dispatch from any specific animation framework.
    /// </summary>
    [CreateAssetMenu(menuName = "NobunAtelier/Ability/Gameplay Event")]
    public class GameplayEvent : DataDefinition { }
}
