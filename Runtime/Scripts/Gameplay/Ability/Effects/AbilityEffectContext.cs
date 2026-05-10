using UnityEngine;
using static NobunAtelier.AbilityModuleDefinition;

namespace NobunAtelier
{
    /// <summary>
    /// Context passed to <see cref="IAbilityEffectInstance.Execute"/>, carrying per-invocation data
    /// computed from SkillDefinition.Value * EventBinding.ValueMultiplier plus spatial offsets.
    /// </summary>
    public readonly struct AbilityEffectContext
    {
        /// <summary>
        /// Pre-computed value: SkillDefinition.Value * EventBinding.ValueMultiplier.
        /// Semantics depend on the effect (damage amount, heal amount, etc.).
        /// </summary>
        public float Value { get; }

        /// <summary>
        /// Whether the effect targets self or the ability's target.
        /// </summary>
        public EffectTarget Target { get; }

        /// <summary>
        /// Position offset relative to the target transform.
        /// </summary>
        public Vector3 PositionOffset { get; }

        /// <summary>
        /// Rotation offset relative to the target transform.
        /// </summary>
        public Quaternion RotationOffset { get; }

        /// <summary>
        /// Reference to the AbilityController executing this effect.
        /// </summary>
        public AbilityController Controller { get; }

        public AbilityEffectContext(
            float value,
            EffectTarget target,
            Vector3 positionOffset,
            Quaternion rotationOffset,
            AbilityController controller)
        {
            Value = value;
            Target = target;
            PositionOffset = positionOffset;
            RotationOffset = rotationOffset;
            Controller = controller;
        }
    }
}
