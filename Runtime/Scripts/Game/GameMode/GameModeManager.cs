using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace NobunAtelier
{
    /// <summary>
    ///  responsible for managing the game mode in a Unity project.
    ///  It provides methods for initializing, starting, stopping, pausing, and resuming the game mode.
    ///  It also handles the addition and removal of players and AI players to the game mode.
    /// </summary>
    public class GameModeManager : MonoBehaviour
    {
        public static GameModeManager Instance { get; private set; }

        public IReadOnlyList<GameModeParticipant> PlayersController => m_participants;
        public PlayerController PlayerControllerPrefab => m_playerControllerPrefab;
        public AIController AIControllerPrefab => m_aiControllerPrefab;
        public Character CharacterMovementPrefab => m_characterMovementPrefab;

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
        private PlayerController m_playerControllerPrefab;

        [SerializeField, ShowIf("m_instantiateController")]
        private AIController m_aiControllerPrefab;

        [SerializeField, ShowIf("m_instantiateController")]
        private bool m_enableInputOnJoin = true;

        [SerializeField]
        private bool m_instantiateCharacterMovement = true;

        [SerializeField, ShowIf("m_instantiateCharacterMovement")]
        private Character m_characterMovementPrefab;

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

                if (m_participants[i].IsAI)
                {
                    RemoveAIPlayer(m_participants[i], true);
                }
                else
                {
                    var controller = m_participants[i].Controller as PlayerController;

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
            if (LegacyPlayerManager.Instance)
            {
                LegacyPlayerManager.Instance.SetActivePlayerInput(false);
            }
            OnGameModePause();
        }

        public virtual void GameModeResume()
        {
            m_isPaused = false;
            if (LegacyPlayerManager.Instance)
            {
                LegacyPlayerManager.Instance.SetActivePlayerInput(true);
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

            if (participant.IsAI)
            {
                var botPlayer = participant as AIParticipant;
                Debug.Assert(botPlayer, $"{this.name}: {participant.name} is not an AIPlayer!");
            }
            else
            {
                var humanPlayer = participant as PlayerInputParticipant;
                Debug.Assert(humanPlayer, $"{this.name}: {participant.name} is not a Human Player!");
            }

            if (m_enableInputOnJoin)
            {
                participant.Controller.EnableInput();
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
                if (participant.Character)
                {
                    Destroy(participant.Character.gameObject);
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
            var basePlayerController = bot.Controller as AIController;
            var botPlayer = bot as AIParticipant;
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
                    basePlayerController.EnableInput();
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
                if (bot.Character)
                {
                    Destroy(bot.Character.gameObject);
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