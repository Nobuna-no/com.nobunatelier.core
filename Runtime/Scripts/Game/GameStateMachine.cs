using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Game/Game State Machine")]
    public class GameStateMachine : BaseStateMachine<GameStateDefinition>
    {
        public virtual void ExitApplication()
        {
            Application.Quit();
        }
    }
}