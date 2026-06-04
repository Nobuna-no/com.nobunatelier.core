using System;
using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    public enum ResetMode
    {
        OnCompletion,
        OnTimeout,
        Loop,
    }

    /// <summary>
    /// Defines a character's full move list as a set of combo paths.
    /// Each path is a sequence of steps mapping input slots to abilities.
    /// </summary>
    [CreateAssetMenu(menuName = "NobunAtelier/Moveset/Moveset Definition")]
    public class MovesetDefinition : ScriptableObject
    {
        [SerializeField] private MovesetPath[] m_Paths;

        public MovesetPath[] Paths => m_Paths;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_Paths == null)
            {
                return;
            }

            for (int i = 0; i < m_Paths.Length; i++)
            {
                var path = m_Paths[i];
                if (path.Steps == null)
                {
                    continue;
                }

                for (int s = 0; s < path.Steps.Length; s++)
                {
                    var step = path.Steps[s];

                    if (step.InputSlot == null)
                    {
                        Debug.LogError($"MovesetDefinition '{name}': Path {i} Step {s} has null InputSlot.", this);
                    }

                    if (step.Ability == null)
                    {
                        Debug.LogWarning($"MovesetDefinition '{name}': Path {i} Step {s} has null Ability.", this);
                    }

                    if (step.IsChargeInput && step.Ability != null && step.Ability.Mode != SkillDefinition.SkillMode.Hold)
                    {
                        Debug.LogError(
                            $"MovesetDefinition '{name}': Path {i} Step {s} has IsChargeInput but skill " +
                            $"'{step.Ability.name}' is not Hold mode.", this);
                    }
                }
            }

            ValidateAmbiguousRouting();
        }

        private void ValidateAmbiguousRouting()
        {
            for (int i = 0; i < m_Paths.Length; i++)
            {
                if (m_Paths[i].Steps == null || m_Paths[i].Steps.Length == 0)
                {
                    continue;
                }

                var slotI = m_Paths[i].Steps[0].InputSlot;
                if (slotI == null)
                {
                    continue;
                }

                for (int j = i + 1; j < m_Paths.Length; j++)
                {
                    if (m_Paths[j].Steps == null || m_Paths[j].Steps.Length == 0)
                    {
                        continue;
                    }

                    var slotJ = m_Paths[j].Steps[0].InputSlot;

                    if (slotI == slotJ && m_Paths[i].Priority == m_Paths[j].Priority)
                    {
                        Debug.LogError(
                            $"MovesetDefinition '{name}': Ambiguous routing — Path {i} and Path {j} share " +
                            $"priority {m_Paths[i].Priority} and Step 0 InputSlot '{slotI.name}'.", this);
                    }
                }
            }
        }
#endif
    }

    [Serializable]
    public class MovesetPath
    {
        [SerializeField] private string m_Name;
        [SerializeField] private int m_Priority;
        [SerializeField] private ResetMode m_ResetMode;
        [AllowNesting, ShowIf("IsTimeout")]
        [SerializeField] private float m_ResetTimeout = 1.5f;
        [SerializeField] private MovesetStep[] m_Steps;

        public int Priority => m_Priority;
        public ResetMode ResetMode => m_ResetMode;
        public float ResetTimeout => m_ResetTimeout;
        public MovesetStep[] Steps => m_Steps;

#if UNITY_EDITOR
        private bool IsTimeout => m_ResetMode == ResetMode.OnTimeout;
#endif
    }

    [Serializable]
    public class MovesetStep
    {
        [SerializeField] private InputSlot m_InputSlot;
        [SerializeField] private SkillDefinition m_Ability;
        [SerializeField] private bool m_IsChargeInput;

        public InputSlot InputSlot => m_InputSlot;
        public SkillDefinition Ability => m_Ability;
        public bool IsChargeInput => m_IsChargeInput;
    }
}
