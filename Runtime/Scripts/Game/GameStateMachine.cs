using UnityEngine;

namespace NobunAtelier
{
    public class GameStateMachine : StateMachineComponent<GameStateDefinition>
    {
        public virtual void ExitApplication()
        {
            Application.Quit();
        }

        private void FixedUpdate()
        {
            Tick(Time.fixedDeltaTime);
        }
    }
}