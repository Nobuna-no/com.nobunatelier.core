using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public class AnimatorSetMoveSpeedModule : AnimationModule
    {
        [AnimatorParam("m_animator", AnimatorControllerParameterType.Float)]
        public int m_moveSpeedParam;

        [SerializeField] private ParticleSystem m_dustParticle;

        private void LateUpdate()
        {
            float speed = ModuleOwner.GetMoveSpeed();
            m_animator.SetFloat(m_moveSpeedParam, speed);

            if (m_dustParticle == null)
            {
                return;
            }

            if (speed > 1f)
            {
                if (m_dustParticle.isPlaying)
                {
                    return;
                }

                m_dustParticle.Play();
            }
            else
            {
                if (!m_dustParticle.isPlaying)
                {
                    return;
                }

                m_dustParticle.Stop();
            }
        }
    }
}
