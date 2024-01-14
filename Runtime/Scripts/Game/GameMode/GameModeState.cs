using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Game Mode/Game Mode State")]
    public class GameModeState : StateWithTransition<GameModeStateDefinition, GameModeStateCollection>
    { }
}