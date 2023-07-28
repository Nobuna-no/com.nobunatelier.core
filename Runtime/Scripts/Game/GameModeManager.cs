using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NobunAtelier
{
    public class GameModeManager : MonoBehaviour
    {
        public static GameModeManager Instance { get; private set; }

        public IReadOnlyList<GameModeParticipant> PlayersController => m_participants;
        public LegacyPlayerControllerBase PlayerControllerPrefab => m_playerControllerPrefab;
        public LegacyAIControllerBase AIControllerPrefab => m_aiControllerPrefab;
        public LegacyCharacterBase CharacterMovementPrefab => m_characterMovementPrefab;

        [Header("GameMode")]
        [SerializeField]
        private bool m_initGameModeOnStart = true;

        [SerializeField]
        protected bool m_isInitMandatory = true;

        [SerializeField]
        protected bool m_callGameModeStopOnDestroy = true;

        [SerializeField, ReadOnly]
        protected bool m_isInitialized = false;

        [SerializeField, ReadOnly]
        protected bool m_isPaused = false;

        [Header("ModuleOwner & LegacyPlayerControllerBase")]
        [InfoBox("This settings allow to instantiate controller and character on a player spawned by a PlayerControllerManager")]
        [SerializeField]
        private bool m_instantiateController = true;

        [SerializeField, ShowIf("m_instantiateController")]
        private LegacyPlayerControllerBase m_playerControllerPrefab;

        [SerializeField, ShowIf("m_instantiateController")]
        private LegacyAIControllerBase m_aiControllerPrefab;

        [SerializeField, ShowIf("m_instantiateController")]
        private bool m_enableInputOnJoin = true;

        [SerializeField]
        private bool m_instantiateCharacterMovement = true;

        [SerializeField, ShowIf("m_instantiateCharacterMovement")]
        private LegacyCharacterBase m_characterMovementPrefab;

        public IList<GameModeParticipant> Participants => m_participants;
        private List<GameModeParticipant> m_participants = new List<GameModeParticipant>();

        public UnityEvent OnGameModeEnd;

        public virtual void ExitApplication()
        {
            Application.Quit();
        }

        public virtual void GameModeInit()
        {
            Instance = this;

            m_isInitialized = true;
        }

        public virtual void GameModeStart()
        {
            if (m_isInitMandatory)
            {
                Debug.Assert(m_isInitialized, "Trying to start game mode without initializing. Call GameModeInit first.");
            }
        }

        public virtual void GameModeStop()
        {
            for (int i = m_participants.Count - 1; i >= 0; --i)
            {
                if (m_participants[i].Controller == null)
                {
                    RemovePlayerWithoutController(m_participants[i]);
                    continue;
                }

                if (m_participants[i].Controller.IsAI)
                {
                    RemoveAIPlayer(m_participants[i], true);
                }
                else
                {
                    var controller = m_participants[i].Controller as LegacyPlayerControllerBase;

                    if (!controller || !controller.PlayerInput)
                    {
                        continue;
                    }

                    RemovePlayer(m_participants[i], true);
                }
            }

            OnGameModeEnd?.Invoke();
        }

        public virtual void GameModePause()
        {
            m_isPaused = true;
            if (PlayerManager.Instance)
            {
                PlayerManager.Instance.SetActivePlayerInput(false);
            }
            OnGameModePause();
        }

        public virtual void GameModeResume()
        {
            m_isPaused = false;
            if (PlayerManager.Instance)
            {
                PlayerManager.Instance.SetActivePlayerInput(true);
            }
            OnGameModeResume();
        }

        public virtual void OnGameModePause()
        { }

        public virtual void OnGameModeResume()
        { }

        public virtual bool IsParticipantAlreadyInGameMode(GameModeParticipant participant)
        {
            return m_participants != null && m_participants.Contains(participant);
        }

        public virtual bool AddPlayer(GameModeParticipant participant)
        {
            if (m_participants.Contains(participant))
            {
                return false;
            }

            if (m_instantiateCharacterMovement)
            {
                participant.InstantiateCharacter(m_characterMovementPrefab);
            }

            if (m_instantiateController)
            {
                participant.InstantiateController(m_playerControllerPrefab);
            }

            var basePlayerController = participant.Controller as LegacyPlayerControllerBase;
            var humanPlayer = participant as Player;
            Debug.Assert(humanPlayer, "LegacyPlayerControllerBase is BasePlayerController but it is not a Human Player!");

            // Pretty sure this is not needed anymore - TO REMOVE
            // basePlayerController.MountPlayerInput(humanPlayer.PlayerInput);

            if (m_enableInputOnJoin)
            {
                basePlayerController.EnableInput();
            }

            m_participants.Add(participant);

            return true;
        }

        public virtual bool RemovePlayer(GameModeParticipant participant, bool destroyChildrenPrefab = true)
        {
            if (!m_participants.Contains(participant))
            {
                return false;
            }

            if (destroyChildrenPrefab)
            {
                if (participant.CharacterMovement)
                {
                    Destroy(participant.CharacterMovement.gameObject);
                }

                Destroy(participant.Controller.gameObject);
            }

            m_participants.Remove(participant);
            return true;
        }

        public virtual bool AddAIPlayer(GameModeParticipant bot)
        {
            if (m_participants.Contains(bot))
            {
                return false;
            }

            if (m_instantiateCharacterMovement)
            {
                bot.InstantiateCharacter(m_characterMovementPrefab);
            }

            if (m_instantiateController)
            {
                bot.InstantiateController(m_aiControllerPrefab);
            }

            // Do we need to mount the input on the AI controller ?
            var basePlayerController = bot.Controller as LegacyAIControllerBase;
            var botPlayer = bot as AIPlayer;
            Debug.Assert(botPlayer, $"{this.name}: {bot.name} is not an AIPlayer!");

            if (m_enableInputOnJoin)
            {
                if (basePlayerController == null)
                {
                    Debug.LogWarning($"{this}: Trying to enable {bot.name}'s AI, but controller is null. Have you set a valid AIControllerPrefab on the game mode?");
                    return false;
                }
                else
                {
                    basePlayerController.EnableAI();
                }
            }

            m_participants.Add(bot);

            return true;
        }

        public virtual bool RemoveAIPlayer(GameModeParticipant bot, bool destroyChildrenPrefab = true)
        {
            if (!m_participants.Contains(bot))
            {
                return false;
            }

            if (destroyChildrenPrefab)
            {
                if (bot.CharacterMovement)
                {
                    Destroy(bot.CharacterMovement.gameObject);
                }

                Destroy(bot.Controller.gameObject);
            }

            m_participants.Remove(bot);
            return true;
        }

        // Used when a player without a controller is remove from the game mode.
        public virtual bool RemovePlayerWithoutController(GameModeParticipant participant)
        {
            if (!m_participants.Contains(participant))
            {
                return false;
            }

            m_participants.Remove(participant);
            return true;
        }

        protected virtual void Awake()
        {
            Instance = null;

            if (m_instantiateCharacterMovement)
            {
                Debug.Assert(m_characterMovementPrefab, $"{this} character movement prefab not assigned.");
            }

            if (m_instantiateController)
            {
                Debug.Assert(m_playerControllerPrefab, $"{this} player controller prefab not assigned.");
            }
        }

        protected virtual void Start()
        {
            if (m_initGameModeOnStart)
            {
                GameModeInit();
            }
        }

        protected virtual void OnDestroy()
        {
            if (m_callGameModeStopOnDestroy)
            {
                GameModeStop();
            }
        }
    }
}