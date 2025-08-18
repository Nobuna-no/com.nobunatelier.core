namespace NobunAtelier.Gameplay
{
    public interface IStatModifier
    {
        /// <summary>
        /// The value of the modifier.
        /// </summary>
        float Value { get; }

        /// <summary>
        /// The source of the modifier.
        /// </summary>
        object Source { get; }

        /// <summary>
        /// The order in which the modifier is applied.
        /// </summary>
        int ExecutionOrder { get; }

        /// <summary>
        /// Apply the modifier to the current value.
        /// </summary>
        /// <param name="baseValue">The base value of the stat.</param>
        /// <param name="currentValue">The current value of the stat.</param>
        /// <returns>The new value of the stat.</returns>
        float ApplyModifier(float baseValue, float currentValue);

        /// <summary>
        /// Set the value of the modifier. Needed when modifier is pooled.
        /// </summary>
        /// <param name="value">The value of the modifier.</param>
        void SetValue(float value);

        public static string GetDescription(IStatModifier modifier)
        {
            return $"[{modifier.ExecutionOrder}] {modifier.GetType().Name}({modifier.Value}) source: {modifier.Source}";
        }
    }
}
