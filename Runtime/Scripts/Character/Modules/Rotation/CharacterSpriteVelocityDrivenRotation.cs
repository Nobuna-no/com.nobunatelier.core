using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/Character/RotationModule: Sprite Velocity Driven")]
    public class CharacterSpriteVelocityDrivenRotation : CharacterRotationModuleBase
    {
        [SerializeField, Required] SpriteRenderer m_targetSprite;
        [SerializeField] private float m_moveTreshold = 0.1f;

        private float m_previousMoveSign = 1f;
        private bool m_originalFlipX = false;

        public override void ModuleInit(Character character)
        {
            base.ModuleInit(character);

            Transform parent = transform.parent;

            Debug.Assert(m_targetSprite != null);
            m_originalFlipX = m_targetSprite.flipX;
        }

        public override void RotationUpdate(float deltaTime)
        {
            Vector3 dir = ModuleOwner.GetMoveVector();

            if (Mathf.Abs(dir.x) < m_moveTreshold)
            {
                return;
            }

            var moveSign = Mathf.Sign(dir.x);
            if (moveSign != m_previousMoveSign)
            {
                if (moveSign > 0f)
                {
                    m_targetSprite.flipX = m_originalFlipX;
                }
                else
                {
                    m_targetSprite.flipX = !m_originalFlipX;
                }

                m_previousMoveSign = moveSign;
            }
        }

        //[SerializeField, Tooltip("Set to 0 the velocity axis you want to ignore.\n i.e. y=0 mean not using y velocity to orient the body.")]
        //protected Vector3 m_forwardSpace = Vector3.one;

        //[SerializeField, Range(0, 50f)]
        //private float m_rotationSpeed = 10f;

        //private float m_initialRotationSpeed = 0f;

        //public void SetRotationSpeed(float rotationSpeed)
        //{
        //    m_rotationSpeed = rotationSpeed;
        //}

        //public void ResetRotationSpeed()
        //{
        //    m_rotationSpeed = m_initialRotationSpeed;
        //}

        //public override void ModuleInit(Character character)
        //{
        //    m_initialRotationSpeed = m_rotationSpeed;

        //    base.ModuleInit(character);
        //}

        //protected void SetForward(Vector3 dir, float deltaTime)
        //{
        //    dir = Vector3.Slerp(ModuleOwner.transform.forward, dir, m_rotationSpeed * deltaTime);
        //    ModuleOwner.Body.Rotation = Quaternion.LookRotation(dir);
        //}

        //public override void RotationUpdate(float deltaTime)
        //{
        //    Vector3 dir = ModuleOwner.GetMoveVector();

        //    dir.x = dir.x * m_forwardSpace.x;
        //    dir.y = dir.y * m_forwardSpace.y;
        //    dir.z = dir.z * m_forwardSpace.z;

        //    if (dir.sqrMagnitude <= 1)
        //    {
        //        return;
        //    }

        //    SetForward(dir.normalized, deltaTime);
        //}
    }
}