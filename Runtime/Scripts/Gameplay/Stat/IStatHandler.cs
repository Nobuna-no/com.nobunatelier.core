using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier.Gameplay
{
    /// <summary>
    /// Interface for systems that manage stats through a <see cref="StatModule"/>.
    /// Provides a standardized way to expose stat processing capabilities
    /// and allows systems to subscribe to stat changes without tight coupling.
    /// </summary>
    public interface IStatHandler
    {
        delegate void OnStatChangedDelegate(IStatDefinition stat);

        /// <summary>
        /// Event raised when the stat modifiers are changed.
        /// Implementors should use the <see cref="StatManager.OnStatInvalidated"/> event to notify the handler.
        /// This way systems can listen to the IStatHandler.OnStatModifiersChanged event to be notified when the stat modifiers are changed.
        /// Without this, the systems would need to wait for StatProcessor to be initialized before being able to listen to the OnStatChanged event.
        /// </summary>
        event OnStatChangedDelegate OnStatChanged;

        StatManager StatModule { get; }
    }

    public static class StatHandlerExtensions
    {
        /// <summary>
        /// Compute the stat value for a given stat and base value by applying all the relevant modifiers.
        /// The value is also clamped base on the stat settings.
        /// </summary>
        /// <param name="handler">The handler to compute the stat value for.</param>
        /// <param name="stat">The stat to compute the value for.</param>
        /// <param name="baseValue"></param>
        /// <returns></returns>
        public static float ComputeStat(this IStatHandler handler, IStatDefinition stat, float baseValue) 
        {
            var currentValue = baseValue;
            if (!handler.StatModule.TryGetStatModifiers(stat, out var modifiers))
            {
                return Mathf.Clamp(currentValue, stat.MinValue, stat.MaxValue);
            }

            foreach (var modifier in modifiers)
            {
                currentValue = modifier.ApplyModifier(baseValue, currentValue);
            }

            return Mathf.Clamp(currentValue, stat.MinValue, stat.MaxValue);
        }
    }
}