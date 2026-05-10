using System;
using UnityEngine;

namespace NobunAtelier
{
    /// <summary>
    /// Minimal test effect that logs event context data to the console.
    /// Used to verify the event-driven pipeline works end-to-end.
    /// </summary>
    [Serializable]
    public class DebugLogEffect : AbilityEffect
    {
        [SerializeField] private string m_Message = "Effect fired";
        [SerializeField] private bool m_LogValue = true;
        [SerializeField] private bool m_LogTarget = true;

        public override IAbilityEffectInstance CreateInstance(AbilityController controller)
        {
            return new Instance(this, controller);
        }

        private class Instance : IAbilityEffectInstance
        {
            private readonly DebugLogEffect m_Data;
            private readonly AbilityController m_Controller;

            public bool NeedsUpdate => false;

            public Instance(DebugLogEffect data, AbilityController controller)
            {
                m_Data = data;
                m_Controller = controller;
            }

            public void Execute(AbilityEffectContext context)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append($"[DebugLogEffect] {m_Data.m_Message}");

                if (m_Data.m_LogValue)
                    sb.Append($" | Value={context.Value:F1}");

                if (m_Data.m_LogTarget)
                    sb.Append($" | Target={context.Target}");

                Debug.Log(sb.ToString(), m_Controller);
            }

            public void Update(float deltaTime) { }

            public void Stop()
            {
                Debug.Log($"[DebugLogEffect] {m_Data.m_Message} — Stopped", m_Controller);
            }
        }
    }
}
