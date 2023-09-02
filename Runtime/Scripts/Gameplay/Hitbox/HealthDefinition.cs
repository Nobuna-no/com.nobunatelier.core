using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier.Gameplay
{
    [CreateAssetMenu(menuName = "NobunAtelier/Gameplay/Health", fileName = "[Health] ")]
    public class HealthDefinition : DataDefinition
    {
        [System.Serializable]
        public enum BurialType
        {
            None,
            Resurect,
            Disappear,
            Destroy
        }

        public float InitialValue => m_InitialValue;
        public float MaxValue => m_MaxValue;
        public float InvulnerabilityDuration => m_invulnerabilityDuration;
        public HealthDefinition.BurialType Burial => m_burialType;
        public Vector2 BurialDelay => m_burialDelay;

        [SerializeField]
        private float m_InitialValue = 1f;

        [SerializeField]
        private float m_MaxValue = 100f;

        [SerializeField]
        private float m_invulnerabilityDuration = 0.15f;

        [SerializeField, Header("Death")]
        private HealthDefinition.BurialType m_burialType = BurialType.Resurect;

        [SerializeField, MinMaxSlider(0, 10), Header("Death")]
        private Vector2 m_burialDelay;
    }
}