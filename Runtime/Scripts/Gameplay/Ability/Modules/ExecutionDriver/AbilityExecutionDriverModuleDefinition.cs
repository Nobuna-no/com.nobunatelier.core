using NobunAtelier;
using UnityEngine;

/// <summary>
/// Base class for ability modules that control execution timing via IAbilityExecutionDriver.
/// Provides helper methods to fire timing events and handles boilerplate initialization.
/// </summary>
/// <typeparam name="T">The derived timing driver definition type</typeparam>
public abstract class AbilityExecutionDriverModuleDefinition<T> : AbilityModuleDefinition, IAbilityExecutionDriverModuleDefinition
    where T : AbilityExecutionDriverModuleDefinition<T>
{
    /// <summary>
    /// Base instance class for timing driver modules.
    /// Handles callback registration and provides protected helper methods for firing timing events.
    /// </summary>
    public abstract class ExecutionDriverInstance : AbilityModuleInstance<T>, IAbilityExecutionDriver
    {
        private IAbilityExecutionDriverCallbacks m_TimingCallbacks;

        protected ExecutionDriverInstance(AbilityController controller, T data)
            : base(controller, data)
        {
        }

        void IAbilityExecutionDriver.Initialize(in AbilityExecutionDriverContext context)
        {
            m_TimingCallbacks = context.Callbacks;
            OnTimingDriverInitialized();
        }

        void IAbilityExecutionDriver.RequestExecution()
        {
            OnExecutionRequested();
        }

        void IAbilityExecutionDriver.Reset()
        {
            OnTimingDriverReset();
            m_TimingCallbacks = null;
        }

        void IAbilityExecutionDriver.Cancel()
        {
            OnTimingDriverCanceled();
            m_TimingCallbacks = null;
        }

        /// <summary>
        /// Called when timing driver is initialized. Override to perform setup.
        /// </summary>
        protected virtual void OnTimingDriverInitialized() { }

        /// <summary>
        /// Called when execution is requested. Override to start timing-driven execution.
        /// </summary>
        protected virtual void OnExecutionRequested() { }

        /// <summary>
        /// Called when timing driver is reset. Override to perform cleanup.
        /// </summary>
        protected virtual void OnTimingDriverReset() { }

        /// <summary>
        /// Called when timing driver is canceled. Override to perform cleanup.
        /// </summary>
        protected virtual void OnTimingDriverCanceled()
        {
            OnTimingDriverReset();
        }

        /// <summary>
        /// Fires the effect start event, triggering ExecuteEffect on all ability modules.
        /// </summary>
        protected void FireEffectStart()
        {
            m_TimingCallbacks?.OnEffectStart();
        }

        /// <summary>
        /// Fires the effect stop event, triggering StopEffect on the ability.
        /// </summary>
        protected void FireEffectStop()
        {
            m_TimingCallbacks?.OnEffectStop();
        }

        /// <summary>
        /// Fires the execution complete event, triggering ability termination.
        /// </summary>
        protected void FireExecutionComplete()
        {
            m_TimingCallbacks?.OnExecutionComplete();
        }

        /// <summary>
        /// Returns true if timing driver is currently initialized and ready to fire events.
        /// </summary>
        protected bool IsTimingDriverActive => m_TimingCallbacks != null;
    }
}
