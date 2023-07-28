using NobunAtelier.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NaughtyAttributes;
using System.Runtime.CompilerServices;

namespace NobunAtelier
{
    public class CharacterComboModule : CharacterAbilityModuleBase
    {
        private enum ComboState
        {
            Idle,
            WantToAttack,
            Attacking, // State during which hitbox is active, can't attack
            FollowUpReady // State after the hitbox is disabled, can do next attack of the combo
        }

        [SerializeField, Required]
        private HitboxBehaviour m_hitbox;
        [SerializeField]
        private ComboDefinition[] m_combo;

        private int m_comboIndex;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);

            Debug.Assert(m_hitbox, $"{this.name}: HitboxBehaviour is required.");

            m_comboIndex = -1;
        }

        public virtual void DoCombo()
        {
            if (m_combo.Length == 0)
            {
                Debug.LogWarning($"{this.name}: No combo assigned.");
                return;
            }

            switch (m_state)
            {
                case ComboState.WantToAttack:
                case ComboState.Attacking:
                    // ignore
                    break;
                case ComboState.Idle:
                    this.enabled = true;
                    m_comboIndex = 0;
                    m_state = ComboState.WantToAttack;
                    m_hitbox.SetHitDefinition(m_combo[m_comboIndex].Hit);
                    return;
                case ComboState.FollowUpReady:
                    m_comboIndex = (int)Mathf.Repeat(m_comboIndex + 1, m_combo.Length);
                    m_state = ComboState.WantToAttack;
                    m_hitbox.SetHitDefinition(m_combo[m_comboIndex].Hit);
                    break;
            }
        }

        private ComboState m_state = ComboState.Idle;
        private float m_timeBuffer = 0;

        private void FixedUpdate()
        {
            m_timeBuffer += Time.fixedDeltaTime;
            switch (m_state)
            {
                case ComboState.Idle:
                    return;
                case ComboState.WantToAttack:
                    m_hitbox.HitBegin();
                    break;
                case ComboState.Attacking:
                    // if time attack finished
                    m_hitbox.HitEnd();
                    break;
                case ComboState.FollowUpReady:
                    break;
            }

            // at the end of the combo, disable
            // this.enabled = false;
        }

        private struct ComboDefinition
        {
            // The hit of the attack
            public HitDefinition Hit;

            public float HitDuration;

            // The timing to chain with the next hit
            [NaughtyAttributes.MinMaxSlider(0, 5)]
            public Vector2 FollowUpTiming;
        }
    }
}
