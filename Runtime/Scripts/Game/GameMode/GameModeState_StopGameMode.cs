using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Game Mode/Game Mode State: Stop Game Mode")]
    public class GameModeState_StopGameMode : StateComponent<GameModeStateDefinition, GameModeStateCollection>
    {
        public override void Enter()
        {
            base.Enter();
            if (LegacyGameModeManager.Instance)
            {
                LegacyGameModeManager.Instance.GameModeStop();
            }
            else
            {
                GameModeManager.Instance.GameModeStop();
            }
        }
    }
}