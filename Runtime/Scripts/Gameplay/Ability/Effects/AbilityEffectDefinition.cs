using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace NobunAtelier
{
    /// <summary>
    /// DataDefinition wrapper for a shared <see cref="AbilityEffect"/>.
    /// Use when the same effect definition is referenced by multiple <see cref="EffectEntry"/>s
    /// (e.g., a standard DamageArea shared across slash attacks, or Start/Stop pairing for duration-bound effects).
    /// </summary>
    [MovedFrom(true, sourceClassName: "AbilityEffectAsset")]
    [CreateAssetMenu(menuName = "NobunAtelier/Ability/Effect Asset")]
    public class AbilityEffectDefinition : DataDefinition
    {
        [SerializeReference, SubclassSelector]
        private AbilityEffect m_Definition;

        public AbilityEffect Definition => m_Definition;
    }
}
