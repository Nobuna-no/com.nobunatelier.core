using System;

namespace NobunAtelier
{
    /// <summary>
    /// Abstract base for inline serializable effects. Used via [SerializeReference] on <see cref="EventBinding"/>.
    /// Concrete implementations (FeedbackEffect, DamageAreaEffect) come in Phase 4.
    /// </summary>
    [Serializable]
    public abstract class AbilityEffect
    {
        /// <summary>
        /// Create a runtime instance of this effect for the given controller.
        /// Instances are cached and reused by ActionExecution (Phase 3).
        /// </summary>
        public abstract IAbilityEffectInstance CreateInstance(AbilityController controller);
    }
}
