using UnityEngine;
using static NobunAtelier.AbilityModuleDefinition;

namespace NobunAtelier
{
    /// <summary>
    /// Context passed to <see cref="IAbilityEffectInstance.Execute"/>.
    /// Carries pre-computed value and target info.
    /// </summary>
    public readonly struct AbilityEffectContext
    {
        /// <summary>
        /// Pre-computed value: SkillDefinition.Value * EffectEntry.ValueMultiplier.
        /// </summary>
        public float Value { get; }

        /// <summary>
        /// Whether the effect targets self or the ability's target.
        /// </summary>
        public EffectTarget Target { get; }

        /// <summary>
        /// Reference to the AbilityController executing this effect.
        /// </summary>
        public AbilityController Controller { get; }

        public AbilityEffectContext(float value, EffectTarget target, AbilityController controller)
        {
            Value = value;
            Target = target;
            Controller = controller;
        }
    }
}
