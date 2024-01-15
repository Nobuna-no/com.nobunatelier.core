using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Game Mode/Game Mode State Machine")]
    public class GameModeStateMachine : StateMachineWithFixedUpdate<GameModeStateDefinition, GameModeStateCollection>
    { }
}