using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Game/Game State Machine")]
    public class GameStateMachine : BaseStateMachine<GameStateDefinition, GameStateCollection>
    {
        public virtual void ExitApplication()
        {
            Application.Quit();
        }
    }
}