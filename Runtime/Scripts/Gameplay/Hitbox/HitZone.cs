using UnityEngine;

namespace NobunAtelier.Gameplay
{
    public class HitZone : Hitbox
    {
        [SerializeField, Header("Hitzone")]
        private bool m_startActive = true;

        private void Start()
        {
            if (m_startActive)
            {
                HitBegin();
            }
        }
    }
}