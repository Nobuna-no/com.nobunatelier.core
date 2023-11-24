using UnityEngine;

namespace NobunAtelier.Gameplay
{
    // [CreateAssetMenu(menuName = "NobunAtelier/Gameplay/Procedural Movement", fileName = "[Procedural Move] ")]
    public class ProceduralMovementDefinition : DataDefinition
    {
        public Vector3 MovementUnit => m_movementUnit;
        public AnimationCurve MovementAnimationCurve => m_movementAnimationCurve;
        public float DurationInSeconds => m_durationInSeconds;

        [SerializeField]
        private Vector3 m_movementUnit = Vector3.forward;

        [SerializeField, Tooltip("The way the movement will be animated.")]
        private AnimationCurve m_movementAnimationCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [SerializeField, Min(0f)]
        private float m_durationInSeconds = 1f;
    }
}