using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Game/Game State")]
    public class GameState : StateWithTransition<GameStateDefinition, GameStateCollection>
    {
    }
}