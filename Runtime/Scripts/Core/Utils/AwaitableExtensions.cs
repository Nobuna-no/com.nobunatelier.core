using System;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Extension methods for Unity's Awaitable type to support fire-and-forget async operations.
    /// </summary>
    public static class AwaitableExtensions
    {
        /// <summary>
        /// Fires and forgets an Awaitable task with proper exception handling.
        /// Use this when you want to start an async operation without awaiting it.
        /// All exceptions will be logged to the Unity console.
        /// </summary>
        /// <param name="awaitable">The awaitable to execute</param>
        public static async void FireAndForget(this Awaitable awaitable)
        {
            try
            {
                await awaitable;
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected and should not be logged as an error
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        
        /// <summary>
        /// Fires and forgets an Awaitable task with proper exception handling and a context object for logging.
        /// Use this when you want to start an async operation without awaiting it.
        /// All exceptions will be logged to the Unity console with the provided context.
        /// </summary>
        /// <param name="awaitable">The awaitable to execute</param>
        /// <param name="context">The Unity Object context for exception logging</param>
        public static async void FireAndForget(this Awaitable awaitable, UnityEngine.Object context)
        {
            try
            {
                await awaitable;
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected and should not be logged as an error
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, context);
            }
        }
    }
}
