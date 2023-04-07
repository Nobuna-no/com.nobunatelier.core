using UnityEngine;

// [CreateAssetMenu(menuName = "NobunAtelier/Gameplay/Hit", fileName = "[Hit] ")]
namespace NobunAtelier.Gameplay
{
    public class HitDefinition : DataDefinition
    {
        public float DamageAmount => m_damageAmount;
        public ProceduralMovementDefinition PushBackDefinition => m_pushBackDefinition;

        [SerializeField]
        private float m_damageAmount;

        [SerializeField]
        private ProceduralMovementDefinition m_pushBackDefinition;

        public HitDefinition(float damageAmount, ProceduralMovementDefinition pushBackDefinition)
        {
            m_damageAmount = damageAmount;
            m_pushBackDefinition = pushBackDefinition;
        }

        public static HitDefinition Create(float damageAmount)
        {
            var newHit = ScriptableObject.CreateInstance<HitDefinition>();
            newHit.m_damageAmount = damageAmount;
            return newHit;
        }
    }
}