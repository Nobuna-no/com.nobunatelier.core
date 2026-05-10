namespace NobunAtelier
{
    /// <summary>
    /// Runtime instance of an <see cref="AbilityEffect"/>.
    /// Created by <see cref="AbilityEffect.CreateInstance"/>, managed by ActionExecution (Phase 3).
    /// </summary>
    public interface IAbilityEffectInstance
    {
        /// <summary>
        /// Whether this instance needs per-frame Update calls while active.
        /// </summary>
        bool NeedsUpdate { get; }

        /// <summary>
        /// Execute the effect with the given context.
        /// For fire-and-forget effects, this is the only call.
        /// For duration-bound effects, this starts the effect.
        /// </summary>
        void Execute(AbilityEffectContext context);

        /// <summary>
        /// Per-frame update for active effects (e.g., hitbox tracking, trail following).
        /// Only called when <see cref="NeedsUpdate"/> returns true.
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// Stop the effect. Called when the bound stop-event fires,
        /// or when the action is torn down.
        /// </summary>
        void Stop();
    }
}
