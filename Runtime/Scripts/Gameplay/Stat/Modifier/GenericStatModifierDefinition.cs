using UnityEngine;

namespace NobunAtelier.Gameplay
{
    [CreateAssetMenu(fileName = "[StatModifier_Generic]", menuName = "NobunAtelier/Gameplay/Stat/Generic Modifier")]
    public class GenericStatModifierDefinition : StatModifierDefinition
    {
        [SerializeField] private GenericStatModifier.ApplicationMode m_Mode = GenericStatModifier.ApplicationMode.Flat;

        public override string DisplayName => $"Generic Modifier ({m_Mode})";

        public override IStatModifier CreateRuntimeModifier(float value, object source = null)
        {
            return new GenericStatModifier(value, source, ExecutionOrder, m_Mode);
        }
    }

    public class GenericStatModifier : IStatModifier
    {
        public float Value { get; private set; }
        public object Source { get; private set; }
        public int ExecutionOrder { get; private set; }
        public ApplicationMode Mode { get; private set; }

        public void SetValue(float value)
        {
            Value = value;
        }

        public GenericStatModifier(float value, object source, int executionOrder, ApplicationMode mode)
        {
            Value = value;
            Source = source;
            ExecutionOrder = executionOrder;
            Mode = mode;
        }

        public float ApplyModifier(float baseValue, float currentValue)
        {
            return Mode switch
            {
                ApplicationMode.Flat => currentValue + Value,
                ApplicationMode.Multiplicative => currentValue * (1f + Value),
                ApplicationMode.Percentage => currentValue * (Value / 100f),
                ApplicationMode.Scale => currentValue * Value,
                ApplicationMode.Override => Value,
                _ => currentValue
            };
        }

        public enum ApplicationMode
        {
            [Tooltip("= modifierValue (replaces current value entirely)\n- Recommended order of execution: 0")]
            Override,

            [Tooltip("= currentValue + modifierValue (additive) - Recommended order of execution: 100")]
            Flat,

            [Tooltip("= currentValue * (1f + modifierValue) (scale increase from current)\n- Recommended order of execution: 200")]
            Multiplicative,

            [Tooltip("= currentValue * (modifierValue / 100f) (percentage-based scaling)\n- Recommended order of execution: 300")]
            Percentage,

            [Tooltip("= currentValue * modifierValue (direct multiplication)\n- Recommended order of execution: 300")]
            Scale,
        }
    }
}

/*
Override first - If something needs to completely replace the value, it should happen before any other calculations
Flat additions - Base modifications to the raw value
Multiplicative - Percentage-based increases (like "+50% damage")
Scale last - Final scaling operations
*/
