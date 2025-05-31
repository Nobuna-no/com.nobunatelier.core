using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    public class AnimatorSetMoveSpeedModule : AnimationModule
    {
        [AnimatorParam("m_Animator", AnimatorControllerParameterType.Float), FormerlySerializedAs("m_moveSpeedParam")]
        public int m_MoveSpeedParam;

        [SerializeField, FormerlySerializedAs("m_dustParticle")]
        private ParticleSystem m_DustParticle;

        private void LateUpdate()
        {
            float speed = ModuleOwner.GetMoveSpeed();
            m_Animator.SetFloat(m_MoveSpeedParam, speed);

            if (m_DustParticle == null)
            {
                return;
            }

            if (speed > 1f)
            {
                if (m_DustParticle.isPlaying)
                {
                    return;
                }

                m_DustParticle.Play();
            }
            else
            {
                if (!m_DustParticle.isPlaying)
                {
                    return;
                }

                m_DustParticle.Stop();
            }
        }
    }
}
