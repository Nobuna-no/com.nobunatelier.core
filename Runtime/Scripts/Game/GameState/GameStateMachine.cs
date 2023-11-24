#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Game/Game State Machine")]
    public class GameStateMachine : StateMachineWithFixedUpdate<GameStateDefinition, GameStateCollection>
    {
        public virtual void ExitApplication()
        {
#if UNITY_EDITOR
            if (Application.isEditor)
            {
                EditorApplication.ExitPlaymode();
            }
#else
            Application.Quit();
#endif
        }
    }
}