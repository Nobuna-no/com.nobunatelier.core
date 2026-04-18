using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Thin MonoBehaviour shell for the Moveset system.
    /// Forwards player input to MovesetInstance for routing, then calls AbilityController API.
    /// </summary>
    public class MovesetController : MonoBehaviour
    {
        [SerializeField] private MovesetDefinition m_Moveset;
        [SerializeField] private AbilityController m_AbilityController;
        [SerializeField] private float m_InputBufferDuration = 0.2f;

        private MovesetInstance m_Instance;
        private InputSlot m_HeldSlot;

        public int ActivePathIndex => m_Instance?.ActivePathIndex ?? -1;
        public MovesetDefinition CurrentMoveset => m_Moveset;

        public int GetComboStep(int pathIndex)
        {
            return m_Instance?.GetComboStep(pathIndex) ?? 0;
        }

        public void SetMoveset(MovesetDefinition moveset)
        {
            m_Moveset = moveset;
            InitializeInstance();
        }

        public void ResetAllPaths()
        {
            m_Instance?.ResetAllPaths();
        }

        public void PressSlot(InputSlot slot)
        {
            if (m_Instance == null)
            {
                return;
            }

            var resolved = m_Instance.ResolvePress(slot);
            if (!resolved.HasValue)
            {
                return;
            }

            var r = resolved.Value;
            bool accepted = m_AbilityController.TryExecute(r.Ability, CreateContext(r));

            if (accepted)
            {
                m_Instance.AdvanceStep(r);
            }
            else
            {
                m_Instance.BufferInput(r);
            }
        }

        public void HoldSlot(InputSlot slot)
        {
            if (m_Instance == null)
            {
                return;
            }

            m_HeldSlot = slot;
            var resolved = m_Instance.ResolveHold(slot);
            if (!resolved.HasValue)
            {
                return;
            }

            var r = resolved.Value;
            bool accepted;

            if (r.IsChargeInput)
            {
                accepted = m_AbilityController.StartCharge(r.Ability);
            }
            else
            {
                accepted = m_AbilityController.TryExecute(r.Ability, CreateContext(r));
            }

            if (accepted)
            {
                m_Instance.AdvanceStep(r);
            }
            else
            {
                m_Instance.BufferInput(r);
            }
        }

        public void ReleaseSlot(InputSlot slot)
        {
            if (m_HeldSlot != slot)
            {
                return;
            }

            m_HeldSlot = null;

            if (m_AbilityController.IsCharging)
            {
                m_AbilityController.ReleaseCharge();
            }
        }

        private void Awake()
        {
            Debug.Assert(m_AbilityController != null, $"{name}: AbilityController reference is required.", this);
        }

        private void OnEnable()
        {
            InitializeInstance();
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            m_Instance?.Update(Time.deltaTime);
        }

        private void InitializeInstance()
        {
            if (m_Moveset == null)
            {
                return;
            }

            if (m_Instance == null)
            {
                m_Instance = new MovesetInstance();
            }

            m_Instance.Initialize(m_Moveset, m_InputBufferDuration);
        }

        private void SubscribeToEvents()
        {
            if (m_AbilityController == null)
            {
                return;
            }

            m_AbilityController.OnRecoveryWindowOpen.AddListener(OnRecoveryWindowOpen);
            m_AbilityController.OnAbilityCompleted.AddListener(OnAbilityCompleted);
            m_AbilityController.OnAbilityCancelled.AddListener(OnAbilityCancelled);
        }

        private void UnsubscribeFromEvents()
        {
            if (m_AbilityController == null)
            {
                return;
            }

            m_AbilityController.OnRecoveryWindowOpen.RemoveListener(OnRecoveryWindowOpen);
            m_AbilityController.OnAbilityCompleted.RemoveListener(OnAbilityCompleted);
            m_AbilityController.OnAbilityCancelled.RemoveListener(OnAbilityCancelled);
        }

        private void OnRecoveryWindowOpen()
        {
            if (m_Instance == null)
            {
                return;
            }

            var resolved = m_Instance.FlushBuffer();
            if (!resolved.HasValue)
            {
                return;
            }

            var r = resolved.Value;
            bool accepted;

            if (r.IsChargeInput && m_HeldSlot != null)
            {
                accepted = m_AbilityController.StartCharge(r.Ability);
            }
            else if (r.IsChargeInput)
            {
                accepted = m_AbilityController.TryExecute(r.Ability, CreateContext(r));
            }
            else
            {
                accepted = m_AbilityController.TryExecute(r.Ability, CreateContext(r));
            }

            if (accepted)
            {
                m_Instance.AdvanceStep(r);
            }
        }

        private void OnAbilityCompleted()
        {
            m_Instance?.ApplyResetRule();
        }

        private void OnAbilityCancelled()
        {
            m_Instance?.ResetOnCancel();
        }

        private AbilityExecutionContext CreateContext(ResolvedInput resolved)
        {
            return new AbilityExecutionContext(resolved.StepIndex, resolved.PathIndex);
        }
    }
}
