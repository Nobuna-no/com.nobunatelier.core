using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NobunAtelier;

namespace NobunAtelier
{
    [CreateAssetMenu(menuName= "NobunAtelier/Collection/Game States", fileName = "DC_GameStates")]
    public class GameStateCollection : DataCollection<GameStateDefinition>
    { }
}