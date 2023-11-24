using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public class TeamDefinition : DataDefinition
    {
        [System.Flags]
        public enum Target
        {
            Self = 0x1,
            Allies = 0x2,
            Enemies = 0x4,
        }

        public IReadOnlyList<TeamDefinition> Allies => m_allies;
        public IReadOnlyList<TeamDefinition> Enemies => m_enemies;

        [SerializeField]
        private TeamDefinition[] m_allies;

        [SerializeField]
        private TeamDefinition[] m_enemies;

        public bool IsTargetValid(Target target, TeamDefinition teamB)
        {
            if ((target & Target.Self) != 0)
            {
                return this == teamB;
            }

            if ((target & Target.Allies) != 0)
            {
                if (this == teamB)
                {
                    return true;
                }

                for (int i = m_allies.Length - 1; i >= 0; i--)
                {
                    if (m_allies[i] == teamB)
                    {
                        return true;
                    }
                }
            }

            if ((target & Target.Enemies) != 0)
            {
                for (int i = m_enemies.Length - 1; i >= 0; i--)
                {
                    if (m_enemies[i] == teamB)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}