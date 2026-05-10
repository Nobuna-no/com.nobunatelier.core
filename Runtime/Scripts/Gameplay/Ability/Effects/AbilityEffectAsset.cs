using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// ScriptableObject wrapper for a shared <see cref="AbilityEffect"/>.
    /// Use when the same effect definition is referenced by multiple <see cref="EventBinding"/>s
    /// (e.g., a standard DamageArea shared across slash attacks, or Start/Stop pairing for duration-bound effects).
    /// </summary>
    [CreateAssetMenu(menuName = "NobunAtelier/Ability/Effect Asset")]
    public class AbilityEffectAsset : ScriptableObject
    {
        [SerializeReference]
        private AbilityEffect m_Definition;

        public AbilityEffect Definition => m_Definition;
    }
}
