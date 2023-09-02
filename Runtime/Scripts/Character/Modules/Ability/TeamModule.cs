using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public class TeamModule : CharacterAbilityModuleBase
    {
        public TeamDefinition Team => m_team;
        [SerializeField]
        private TeamDefinition m_team;
    }
}
