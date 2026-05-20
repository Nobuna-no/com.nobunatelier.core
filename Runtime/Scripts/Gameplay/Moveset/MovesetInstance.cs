using System.Collections.Generic;

namespace NobunAtelier
{
    internal struct ResolvedInput
    {
        public int PathIndex;
        public int StepIndex;
        public SkillDefinition Ability;
        public bool IsChargeInput;
    }

    /// <summary>
    /// Runtime state for combo routing, step tracking, and input buffering.
    /// Owned by MovesetController.
    /// </summary>
    internal class MovesetInstance
    {
        private struct CachedPath
        {
            public MovesetPath Definition;
            public int CurrentStep;
            public float ResetTimer;
            public bool IsTimerActive;
        }

        private CachedPath[] m_CachedPaths;
        private Dictionary<InputSlot, List<int>> m_InputToCandidates;
        private int m_ActivePathIndex = -1;

        private ResolvedInput? m_BufferedInput;
        private float m_BufferAge;
        private float m_BufferDuration;

        public int ActivePathIndex => m_ActivePathIndex;

        public void Initialize(MovesetDefinition definition, float bufferDuration)
        {
            m_BufferDuration = bufferDuration;

            var paths = definition.Paths;
            m_CachedPaths = new CachedPath[paths.Length];
            m_InputToCandidates = new Dictionary<InputSlot, List<int>>();

            for (int i = 0; i < paths.Length; i++)
            {
                m_CachedPaths[i] = new CachedPath
                {
                    Definition = paths[i],
                    CurrentStep = 0,
                    ResetTimer = 0f,
                    IsTimerActive = false,
                };

                if (paths[i].Steps != null && paths[i].Steps.Length > 0)
                {
                    var slot = paths[i].Steps[0].InputSlot;
                    if (slot != null)
                    {
                        if (!m_InputToCandidates.TryGetValue(slot, out var list))
                        {
                            list = new List<int>();
                            m_InputToCandidates[slot] = list;
                        }

                        list.Add(i);
                    }
                }
            }

            m_ActivePathIndex = -1;
            m_BufferedInput = null;
        }

        public void Update(float deltaTime)
        {
            if (m_BufferedInput.HasValue)
            {
                m_BufferAge += deltaTime;
                if (m_BufferAge >= m_BufferDuration)
                {
                    m_BufferedInput = null;
                }
            }

            for (int i = 0; i < m_CachedPaths.Length; i++)
            {
                if (!m_CachedPaths[i].IsTimerActive)
                {
                    continue;
                }

                m_CachedPaths[i].ResetTimer += deltaTime;
                if (m_CachedPaths[i].ResetTimer >= m_CachedPaths[i].Definition.ResetTimeout)
                {
                    m_CachedPaths[i].CurrentStep = 0;
                    m_CachedPaths[i].IsTimerActive = false;

                    if (m_ActivePathIndex == i)
                    {
                        m_ActivePathIndex = -1;
                    }
                }
            }
        }

        public ResolvedInput? ResolvePress(InputSlot inputSlot)
        {
            return ResolveInput(inputSlot);
        }

        public ResolvedInput? ResolveHold(InputSlot inputSlot)
        {
            return ResolveInput(inputSlot);
        }

        public ResolvedInput? FlushBuffer()
        {
            if (!m_BufferedInput.HasValue)
            {
                return null;
            }

            var resolved = m_BufferedInput.Value;
            m_BufferedInput = null;
            return resolved;
        }

        public void BufferInput(ResolvedInput resolved)
        {
            m_BufferedInput = resolved;
            m_BufferAge = 0f;
        }

        public void AdvanceStep(ResolvedInput resolved)
        {
            if (resolved.PathIndex != m_ActivePathIndex)
            {
                ResetCrossPath(resolved.PathIndex);
            }

            m_ActivePathIndex = resolved.PathIndex;
            m_CachedPaths[resolved.PathIndex].CurrentStep = resolved.StepIndex + 1;
            m_CachedPaths[resolved.PathIndex].IsTimerActive = false;
        }

        public void ApplyResetRule()
        {
            if (m_ActivePathIndex < 0 || m_ActivePathIndex >= m_CachedPaths.Length)
            {
                return;
            }

            ref var path = ref m_CachedPaths[m_ActivePathIndex];
            bool isLastStep = path.CurrentStep >= path.Definition.Steps.Length;

            switch (path.Definition.ResetMode)
            {
                case ResetMode.OnCompletion:
                    path.CurrentStep = 0;
                    path.IsTimerActive = false;
                    m_ActivePathIndex = -1;
                    break;

                case ResetMode.OnTimeout:
                    if (isLastStep)
                    {
                        path.CurrentStep = 0;
                        path.IsTimerActive = false;
                        m_ActivePathIndex = -1;
                    }
                    else
                    {
                        path.ResetTimer = 0f;
                        path.IsTimerActive = true;
                    }
                    break;

                case ResetMode.Loop:
                    path.CurrentStep = 0;
                    path.ResetTimer = 0f;
                    path.IsTimerActive = true;
                    break;
            }
        }

        public void ResetAllPaths()
        {
            for (int i = 0; i < m_CachedPaths.Length; i++)
            {
                m_CachedPaths[i].CurrentStep = 0;
                m_CachedPaths[i].IsTimerActive = false;
            }

            m_ActivePathIndex = -1;
            m_BufferedInput = null;
        }

        public void ResetOnCancel()
        {
            if (m_ActivePathIndex >= 0 && m_ActivePathIndex < m_CachedPaths.Length)
            {
                m_CachedPaths[m_ActivePathIndex].CurrentStep = 0;
                m_CachedPaths[m_ActivePathIndex].IsTimerActive = false;
            }

            m_ActivePathIndex = -1;
            m_BufferedInput = null;
        }

        public int GetComboStep(int pathIndex)
        {
            if (pathIndex < 0 || pathIndex >= m_CachedPaths.Length)
            {
                return 0;
            }

            return m_CachedPaths[pathIndex].CurrentStep;
        }

        private ResolvedInput? ResolveInput(InputSlot inputSlot)
        {
            if (m_ActivePathIndex >= 0)
            {
                ref var activePath = ref m_CachedPaths[m_ActivePathIndex];
                int nextStep = activePath.CurrentStep;

                if (nextStep < activePath.Definition.Steps.Length)
                {
                    var step = activePath.Definition.Steps[nextStep];
                    if (step.InputSlot == inputSlot)
                    {
                        return new ResolvedInput
                        {
                            PathIndex = m_ActivePathIndex,
                            StepIndex = nextStep,
                            Ability = step.Ability,
                            IsChargeInput = step.IsChargeInput,
                        };
                    }
                }
                else if (activePath.Definition.ResetMode == ResetMode.Loop)
                {
                    var step = activePath.Definition.Steps[0];
                    if (step.InputSlot == inputSlot)
                    {
                        return new ResolvedInput
                        {
                            PathIndex = m_ActivePathIndex,
                            StepIndex = 0,
                            Ability = step.Ability,
                            IsChargeInput = step.IsChargeInput,
                        };
                    }
                }

                return ResolveCrossPath(inputSlot, activePath.Definition.Priority);
            }

            return ResolveFromIdle(inputSlot);
        }

        private ResolvedInput? ResolveCrossPath(InputSlot inputSlot, int activePathPriority)
        {
            if (!m_InputToCandidates.TryGetValue(inputSlot, out var candidates))
            {
                return null;
            }

            int bestPathIndex = -1;
            int bestPriority = activePathPriority;

            foreach (int pathIndex in candidates)
            {
                int priority = m_CachedPaths[pathIndex].Definition.Priority;
                if (priority > bestPriority ||
                    (priority == bestPriority && bestPathIndex >= 0 && pathIndex < bestPathIndex))
                {
                    bestPriority = priority;
                    bestPathIndex = pathIndex;
                }
            }

            if (bestPathIndex < 0)
            {
                return null;
            }

            var step = m_CachedPaths[bestPathIndex].Definition.Steps[0];
            return new ResolvedInput
            {
                PathIndex = bestPathIndex,
                StepIndex = 0,
                Ability = step.Ability,
                IsChargeInput = step.IsChargeInput,
            };
        }

        private ResolvedInput? ResolveFromIdle(InputSlot inputSlot)
        {
            if (!m_InputToCandidates.TryGetValue(inputSlot, out var candidates))
            {
                return null;
            }

            int bestPathIndex = -1;
            int bestPriority = int.MinValue;

            foreach (int pathIndex in candidates)
            {
                int priority = m_CachedPaths[pathIndex].Definition.Priority;
                if (priority > bestPriority ||
                    (priority == bestPriority && (bestPathIndex < 0 || pathIndex < bestPathIndex)))
                {
                    bestPriority = priority;
                    bestPathIndex = pathIndex;
                }
            }

            if (bestPathIndex < 0)
            {
                return null;
            }

            var step = m_CachedPaths[bestPathIndex].Definition.Steps[0];
            return new ResolvedInput
            {
                PathIndex = bestPathIndex,
                StepIndex = 0,
                Ability = step.Ability,
                IsChargeInput = step.IsChargeInput,
            };
        }

        private void ResetCrossPath(int newActivePathIndex)
        {
            for (int i = 0; i < m_CachedPaths.Length; i++)
            {
                if (i != newActivePathIndex)
                {
                    m_CachedPaths[i].CurrentStep = 0;
                    m_CachedPaths[i].IsTimerActive = false;
                }
            }
        }
    }
}
