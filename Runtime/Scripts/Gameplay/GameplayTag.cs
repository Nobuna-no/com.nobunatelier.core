using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Lightweight identity token for runtime state flags (e.g., SuperArmor, IFrame, Stagger).
    /// Used by SkillDefinition to declare tags granted/revoked at state transitions.
    /// Tag management system is future work; this establishes the data slot.
    /// </summary>
    public class GameplayTagDefinition : DataDefinition
    {
#if UNITY_EDITOR
        [SerializeField] private string m_Description;
#endif
    }

    [CreateAssetMenu(fileName = "[GameplayTag]", menuName = "NobunAtelier/Ability/Gameplay Tags")]
    public class GameplayTagCollection : DataCollection<GameplayTagDefinition> { }

}
