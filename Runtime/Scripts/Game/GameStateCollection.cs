using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NobunAtelier;

namespace NobunAtelier
{
    [CreateAssetMenu(menuName= "NobunAtelier/Collection/Game State", fileName = "DC_GameStates")]
    public class GameStateCollection : DataCollection<GameStateDefinition>
    { }
}