using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Game/Game State: Game Mode Bootstrap")]
    public class GameState_GameModeBootstrap : StateComponent<GameStateDefinition, GameStateCollection>
    {
        [System.Flags]
        private enum BootstrapTypes
        {
            ListenToGameModeStop = 1 << 0,
            GameModeInit = 1 << 1,
            GameModeStart = 1 << 2
        }

        [Header("GameMode")]
        [SerializeField]
        private BootstrapTypes m_gameModeHandling = BootstrapTypes.GameModeInit | BootstrapTypes.ListenToGameModeStop | BootstrapTypes.GameModeStart;

        [SerializeField, ShowIf("IsTransitioningOnGameModeStop")]
        private GameStateDefinition m_nextStateOnGameModeStop;

        [Header("Events")]
        [SerializeField]
        [ShowIf("IsInitializingGameMode")]
        private bool m_addPlayersToGameModeOnInit = false;

        [ShowIf("IsInitializingGameMode")]
        public UnityEvent OnGameModeInitEvent;

        [ShowIf("IsStartingGameMode")]
        public UnityEvent OnGameModeStartEvent;

        [ShowIf("IsTransitioningOnGameModeStop")]
        public UnityEvent OnGameModeStopEvent;

        public override void Enter()
        {
            base.Enter();

            var gamemode = FindFirstObjectByType<GameModeManager>();
            if (!gamemode)
            {
                if (InitLegacyGameMode())
                {
                    return;
                }

                Debug.LogWarning($"{this}: No game mode found");
                return;
            }

            if (IsInitializingGameMode())
            {
                gamemode.GameModeInit();
                OnGameModeInitEvent?.Invoke();

                if (m_addPlayersToGameModeOnInit)
                {
                    LegacyPlayerManager.Instance.AddPlayersToGameMode();
                }
            }
            if (IsTransitioningOnGameModeStop())
            {
                gamemode.OnGameModeEnd.AddListener(OnGameModeEnd);
            }
            if (IsStartingGameMode())
            {
                gamemode.GameModeStart();
                OnGameModeStartEvent?.Invoke();
            }

        }

        private bool InitLegacyGameMode()
        {
            var legacyGamemode = FindFirstObjectByType<LegacyGameModeManager>();
            if (!legacyGamemode)
            {
                return false;
            }

            if (IsInitializingGameMode())
            {
                legacyGamemode.GameModeInit();
                OnGameModeInitEvent?.Invoke();

                if (m_addPlayersToGameModeOnInit)
                {
                    LegacyPlayerManager.Instance.AddPlayersToGameMode();
                }
            }
            if (IsTransitioningOnGameModeStop())
            {
                legacyGamemode.OnGameModeEnd.AddListener(OnGameModeEnd);
            }
            if (IsStartingGameMode())
            {
                legacyGamemode.GameModeStart();
                OnGameModeStartEvent?.Invoke();
            }

            return true;
        }

        private bool IsTransitioningOnGameModeStop()
        {
            return (m_gameModeHandling & BootstrapTypes.ListenToGameModeStop) != 0;
        }

        private bool IsInitializingGameMode()
        {
            return (m_gameModeHandling & BootstrapTypes.GameModeInit) != 0;
        }

        private bool IsStartingGameMode()
        {
            return (m_gameModeHandling & BootstrapTypes.GameModeStart) != 0;
        }

        private void OnGameModeEnd()
        {
            OnGameModeStopEvent?.Invoke();
            if (!m_nextStateOnGameModeStop)
            {
                Log.Record("Game mode ended but no state to follow", ContextualLogManager.LogTypeFilter.Warning);
                return;
            }

            SetState(m_nextStateOnGameModeStop);
        }
    }
}