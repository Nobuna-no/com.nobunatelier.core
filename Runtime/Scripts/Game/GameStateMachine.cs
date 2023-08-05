using UnityEngine;

namespace NobunAtelier
{
    public class GameStateMachine : BaseStateMachine<GameStateDefinition>
    {
        public virtual void ExitApplication()
        {
            Application.Quit();
        }
    }
}