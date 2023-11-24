using UnityEngine;

namespace NobunAtelier
{
    public class TeamModule : CharacterAbilityModuleBase
    {
        public TeamDefinition Team => m_team;

        [SerializeField]
        private TeamDefinition m_team;
    }
}