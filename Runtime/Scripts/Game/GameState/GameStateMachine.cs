using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Game/Game State Machine")]
    public class GameStateMachine : StateMachineWithFixedUpdate<GameStateDefinition, GameStateCollection>
    {
        public virtual void ExitApplication()
        {
            Application.Quit();
        }
    }
}