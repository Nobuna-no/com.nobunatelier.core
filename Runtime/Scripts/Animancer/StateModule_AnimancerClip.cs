#if ANIMANCER
using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Plays an Animancer clip when the owning state enters.
    /// Optional clip-end callback drives Manual <see cref="StateWithTransition{T,TCollection}"/> exits.
    /// </summary>
    [AddComponentMenu("NobunAtelier/States/Modules/StateModule: Animancer Clip")]
    public class StateModule_AnimancerClip : StateComponentModule
    {
        private enum LayerMaskBehavior
        {
            Unchanged = 0,
            FullBody = 1,
        }

        [System.Serializable]
        public class ObjectEmittedEventBinding
        {
            [Tooltip("Must match the Object assigned on the transition event's Animancer ParameterObject.")]
            [SerializeField] private UnityEngine.Object m_ParameterObject;

            [SerializeField] private UnityEngine.Events.UnityEvent m_OnEmitted;

            public UnityEngine.Object ParameterObject => m_ParameterObject;

            public UnityEngine.Events.UnityEvent OnEmitted => m_OnEmitted;
        }

        private struct BoundAnimancerCallback
        {
            public AnimancerEvent.Sequence Sequence;
            public int EventIndex;
            public Action Handler;
            public bool IsEndEvent;
        }

        [SerializeField] private AnimancerComponent m_Animancer;

        [Tooltip("Animancer layer index. 0 = base body; use 1+ for masked upper-body actions.")]
        [SerializeField, Min(0)] private int m_LayerIndex;

        [Tooltip("Clip transition played on state enter.")]
        [SerializeField] private TransitionAsset m_Transition;

        [Tooltip("Override clip length. Use -1 for the transition default.")]
        [SerializeField] private float m_Duration = -1f;

        [Header("Layer overlay")]
        [Tooltip("Swap the layer AvatarMask on enter so a full-body clip can override locomotion on layer 0 underneath.")]
        [SerializeField] private LayerMaskBehavior m_LayerMaskBehavior;

        [Tooltip("Optional full-body mask for FullBody behavior. Falls back to a runtime humanoid mask when unset.")]
        [SerializeField] private AvatarMask m_FullBodyLayerMask;

        [Tooltip("When enabled, preserve the layer on early exit; release it only when the clip reaches its end event.")]
        [SerializeField] private bool m_HoldLayerOnEarlyExit;

        [Tooltip("Restore the layer AvatarMask saved on enter.")]
        [SerializeField] private bool m_RestoreLayerMaskOnExit = true;

        [Tooltip("Fade the action layer out on exit so base locomotion/posture shows through again.")]
        [SerializeField] private bool m_ReleaseLayerOnExit = true;

        [SerializeField, Min(0f)] private float m_FadeOutDuration = 0.15f;

        [Tooltip("When this clip plays on the locomotion layer, re-play locomotion on exit.")]
        [SerializeField] private StateModule_AnimancerLocomotion m_RestoreLocomotionOnExit;

        [Header("Transition object events")]
        [Tooltip("Invoked when a transition AnimancerEvent ParameterObject emits a matching Object.")]
        [SerializeField] private ObjectEmittedEventBinding[] m_ObjectEventBindings;

        [Header("Manual transition")]
        [Tooltip("When enabled, SetState is called on clip end using Next State On Clip End.")]
        [SerializeField] private bool m_CompleteStateOnClipEnd;

        [SerializeField] private StateDefinition m_NextStateOnClipEnd;

        private AnimancerState m_AnimancerState;
        private AnimancerLayer m_ActionLayer;
        private AvatarMask m_PreviousLayerMask;
        private bool m_HasPreviousLayerMask;
        private bool m_ExitFromClipCompletion;
        private static AvatarMask s_RuntimeFullBodyMask;
        private readonly List<BoundAnimancerCallback> m_BoundCallbacks = new List<BoundAnimancerCallback>();

        public override void Enter()
        {
            m_BoundCallbacks.Clear();
            m_ExitFromClipCompletion = false;

            if (m_Animancer == null)
            {
                Debug.LogError($"[{nameof(StateModule_AnimancerClip)}] No AnimancerComponent assigned on {gameObject.name}", this);
                return;
            }

            if (m_Transition == null || m_Transition.GetTransition() == null)
            {
                Debug.LogWarning($"[{nameof(StateModule_AnimancerClip)}] No transition assigned on {gameObject.name}", this);
                return;
            }

            m_ActionLayer = m_Animancer.Layers[m_LayerIndex];
            ApplyLayerMaskBehavior(m_ActionLayer);

            m_AnimancerState = m_ActionLayer.Play(m_Transition);
            if (m_Duration > 0f)
            {
                m_AnimancerState.Duration = m_Duration;
            }

            RestartAnimancerState(m_AnimancerState);

            m_AnimancerState.Events(this, out var sequence);
            BindParameterObjectEvents(sequence);

            if (ShouldTrackClipEnd())
            {
                sequence.OnEnd += OnClipEnd;
                m_BoundCallbacks.Add(new BoundAnimancerCallback
                {
                    Sequence = sequence,
                    Handler = OnClipEnd,
                    IsEndEvent = true,
                });
            }
        }

        private bool ShouldTrackClipEnd()
        {
            return m_HoldLayerOnEarlyExit
                || (m_CompleteStateOnClipEnd && m_NextStateOnClipEnd != null);
        }

        public override void Exit()
        {
            UnbindCallbacks();

            if (ShouldReleaseLayerOnExit())
            {
                ReleaseActionLayer();
                if (m_RestoreLayerMaskOnExit)
                {
                    RestoreLayerMask();
                }

                m_RestoreLocomotionOnExit?.Refresh();
            }
            else
            {
                m_HasPreviousLayerMask = false;
                m_PreviousLayerMask = null;
            }

            m_ExitFromClipCompletion = false;
            m_AnimancerState = null;
            m_ActionLayer = null;
        }

        private bool ShouldReleaseLayerOnExit()
        {
            if (!m_HoldLayerOnEarlyExit)
            {
                return true;
            }

            return m_ExitFromClipCompletion;
        }

        private void BindParameterObjectEvents(AnimancerEvent.Sequence sequence)
        {
            if (sequence == null || m_ObjectEventBindings == null || m_ObjectEventBindings.Length == 0)
            {
                return;
            }

            for (int i = 0; i < sequence.Count; i++)
            {
                if (!HasParameterObjectCallback(sequence[i].callback))
                {
                    continue;
                }

                var handler = sequence.AddCallback<UnityEngine.Object>(i, OnParameterObjectEmitted);
                if (handler == null)
                {
                    continue;
                }

                m_BoundCallbacks.Add(new BoundAnimancerCallback
                {
                    Sequence = sequence,
                    EventIndex = i,
                    Handler = handler,
                });
            }

            if (!HasParameterObjectCallback(sequence.OnEnd))
            {
                return;
            }

            AnimancerEvent.AssertContainsParameter<UnityEngine.Object>(sequence.OnEnd);
            var endHandler = AnimancerEvent.Parametize<UnityEngine.Object>(OnParameterObjectEmitted);
            sequence.OnEnd += endHandler;
            m_BoundCallbacks.Add(new BoundAnimancerCallback
            {
                Sequence = sequence,
                Handler = endHandler,
                IsEndEvent = true,
            });
        }

        private void OnParameterObjectEmitted(UnityEngine.Object emitted)
        {
            if (emitted == null || m_ObjectEventBindings == null)
            {
                return;
            }

            for (int i = 0; i < m_ObjectEventBindings.Length; i++)
            {
                var binding = m_ObjectEventBindings[i];
                if (binding == null || binding.ParameterObject != emitted)
                {
                    continue;
                }

                binding.OnEmitted?.Invoke();
            }
        }

        private void UnbindCallbacks()
        {
            for (int i = 0; i < m_BoundCallbacks.Count; i++)
            {
                var bound = m_BoundCallbacks[i];
                if (bound.Sequence == null || bound.Handler == null)
                {
                    continue;
                }

                if (bound.IsEndEvent)
                {
                    bound.Sequence.OnEnd -= bound.Handler;
                }
                else
                {
                    bound.Sequence.RemoveCallback(bound.EventIndex, bound.Handler);
                }
            }

            m_BoundCallbacks.Clear();
        }

        private static bool HasParameterObjectCallback(Action callback)
        {
            if (callback == null)
            {
                return false;
            }

            var invocations = callback.GetInvocationList();
            for (int i = 0; i < invocations.Length; i++)
            {
                if (invocations[i].Target is AnimancerEvent.IParameter)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyLayerMaskBehavior(AnimancerLayer layer)
        {
            if (layer == null || m_LayerMaskBehavior == LayerMaskBehavior.Unchanged)
            {
                return;
            }

            var fullBodyMask = ResolveFullBodyMask();
            if (fullBodyMask == null)
            {
                Debug.LogWarning(
                    $"[{nameof(StateModule_AnimancerClip)}] Could not resolve a full-body AvatarMask on {gameObject.name}",
                    this);
                return;
            }

            m_PreviousLayerMask = layer.Mask;
            m_HasPreviousLayerMask = true;
            layer.Mask = fullBodyMask;
        }

        private AvatarMask ResolveFullBodyMask()
        {
            if (m_FullBodyLayerMask != null)
            {
                return m_FullBodyLayerMask;
            }

            if (s_RuntimeFullBodyMask == null)
            {
                s_RuntimeFullBodyMask = CreateHumanoidFullBodyMask();
            }

            return s_RuntimeFullBodyMask;
        }

        private static AvatarMask CreateHumanoidFullBodyMask()
        {
            var mask = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, true);
            }

            return mask;
        }

        private void RestoreLayerMask()
        {
            if (!m_HasPreviousLayerMask || m_ActionLayer == null)
            {
                return;
            }

            if (m_PreviousLayerMask != null)
            {
                m_ActionLayer.Mask = m_PreviousLayerMask;
            }

            m_HasPreviousLayerMask = false;
            m_PreviousLayerMask = null;
        }

        private void ReleaseActionLayer()
        {
            if (!m_ReleaseLayerOnExit || m_ActionLayer == null || m_LayerIndex == 0)
            {
                return;
            }

            if (m_FadeOutDuration <= 0f)
            {
                m_ActionLayer.Stop();
                return;
            }

            m_ActionLayer.StartFade(0f, m_FadeOutDuration);
        }

        private void RestartAnimancerState(AnimancerState state)
        {
            if (state == null)
            {
                return;
            }

            var transition = m_Transition.GetTransition();
            if (transition != null && !float.IsNaN(transition.NormalizedStartTime))
            {
                state.NormalizedTime = transition.NormalizedStartTime;
                return;
            }

            // Animancer reuses cached states; Play() continues from the previous Time otherwise.
            state.TimeD = 0;
        }

        private void OnClipEnd()
        {
            m_ExitFromClipCompletion = true;

            if (!m_CompleteStateOnClipEnd || m_NextStateOnClipEnd == null || ModuleOwner == null)
            {
                return;
            }

            ModuleOwner.SetState(m_NextStateOnClipEnd.GetType(), m_NextStateOnClipEnd);
        }

        private void Reset()
        {
            m_Animancer = GetComponentInParent<AnimancerComponent>();
        }
    }
}
#endif
