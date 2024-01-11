using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    [RequireComponent(typeof(PlayerInputManager))]
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }

        [Header("Player Manager")]
        [SerializeField, InfoBox("Maximum number of player + bot combined.")]
        private int m_playerLimit = 4;

        [SerializeField, Required, InfoBox("This prefab will override the PlayerInputManager `Player Prefab`")]
        private PlayerInputParticipant m_playerPrefab;

        [SerializeField]
        private AIParticipant m_botPrefab;

        [SerializeField]
        private bool m_enablePlayerJoiningByDefault = true;

        [SerializeField]
        private bool m_dontDestroyOnLoad = true;

        [SerializeField]
        private bool m_addToGameModeOnPlayerJoin = true;

        [Header("Events")]
        public UnityEvent OnPlayerJoinedEvent;

        public UnityEvent OnPlayerLeftEvent;

        private bool m_addPlayerToGameModeOnPlayerJoin;

        private PlayerInputManager m_manager;
        private GameModeManager m_gamemode;
        private List<PlayerInputParticipant> m_players = new List<PlayerInputParticipant>();
        private List<AIParticipant> m_bots = new List<AIParticipant>();
        private List<GameModeParticipant> m_pendingPlayers = new List<GameModeParticipant>();

        public virtual void EnablePlayerJoining()
        {
            m_manager.EnableJoining();
        }

        public virtual void EnablePlayerJoining(bool addToGameModeOnPlayerJoining)
        {
            m_manager.EnableJoining();
            m_addPlayerToGameModeOnPlayerJoin = addToGameModeOnPlayerJoining;
        }

        public virtual void DisablePlayerJoining()
        {
            m_manager.DisableJoining();
        }

        public virtual void DisablePlayerJoining(bool resetAddToGameModeOnPlayerJoining)
        {
            m_manager.DisableJoining();

            if (resetAddToGameModeOnPlayerJoining)
            {
                m_addPlayerToGameModeOnPlayerJoin = m_addToGameModeOnPlayerJoin;
            }
        }

        public virtual void SetActivePlayerInput(bool enable)
        {
            foreach (var p in m_players)
            {
                if (enable)
                {
                    p.PlayerInput.ActivateInput();
                }
                else
                {
                    p.PlayerInput.DeactivateInput();
                }
            }
        }

        public virtual void SetActiveAIPlayer(bool enable)
        {
            foreach (var p in m_bots)
            {
                if (enable)
                {
                    p.AIController.EnableAI();
                }
                else
                {
                    p.AIController.DisableAI();
                }
            }
        }

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public virtual void AddAIPlayer()
        {
            if (!CanNewPlayerJoin())
            {
                return;
            }

            var bot = Instantiate(m_botPrefab, transform);
            m_pendingPlayers.Add(bot);

            this.enabled = true;
        }

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public virtual void RemoveAIPlayer(AIParticipant botToRemove = null, bool removeFromGameMode = true)
        {
            if (botToRemove == null)
            {
                if (m_bots.Count > 0)
                {
                    botToRemove = m_bots[m_bots.Count - 1];
                }
                else
                {
                    return;
                }
            }

            if (removeFromGameMode)
            {
                m_gamemode = FindFirstObjectByType<GameModeManager>();
                if (m_gamemode)
                {
                    m_gamemode.RemoveAIPlayer(botToRemove);
                }
            }

            m_bots.Remove(botToRemove);
        }

        private bool CanNewPlayerJoin()
        {
            int currentNumberOfPlayers = m_players.Count + m_bots.Count;
            if (currentNumberOfPlayers >= m_playerLimit)
            {
                Debug.LogWarning($"PlayerManager: Cannot add anymore player, maximum player count ({m_playerLimit}) reached!");
                return false;
            }

            return true;
        }

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public virtual void AddPlayersToGameMode()
        {
            m_gamemode = FindFirstObjectByType<GameModeManager>();
            if (!m_gamemode)
            {
                return;
            }

            for (int i = m_players.Count - 1; i >= 0; --i)
            {
                if (m_gamemode.IsParticipantAlreadyInGameMode(m_players[i]))
                {
                    continue;
                }

                if (!m_gamemode.AddPlayer(m_players[i]))
                {
                    Debug.LogWarning($"{this}: failed to add {m_players[i].name} to the game mode. Disabling player.");
                    m_players[i].gameObject.SetActive(false);
                    m_players.RemoveAt(i);
                }
            }
            for (int i = m_bots.Count - 1; i >= 0; --i)
            {
                if (m_gamemode.IsParticipantAlreadyInGameMode(m_bots[i]))
                {
                    continue;
                }

                if (!m_gamemode.AddAIPlayer(m_bots[i]))
                {
                    Debug.LogWarning($"{this}: failed to add {m_bots[i].name} to the game mode. Disabling bot.");
                    m_bots[i].gameObject.SetActive(false);
                    m_bots.RemoveAt(i);
                }
            }
        }

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public virtual void RemovePlayersFromGameMode()
        {
            m_gamemode = FindFirstObjectByType<GameModeManager>();
            if (!m_gamemode)
            {
                return;
            }

            foreach (var p in m_players)
            {
                m_gamemode.RemovePlayer(p);
            }
            foreach (var p in m_bots)
            {
                m_gamemode.RemoveAIPlayer(p);
            }
        }

        protected virtual void OnHumanPlayerLeft(PlayerInput obj)
        {
            m_players.Remove(obj.GetComponent<PlayerInputParticipant>());
            OnPlayerLeftEvent?.Invoke();
        }

        protected virtual void OnHumanPlayerJoined(PlayerInput obj)
        {
            if (!CanNewPlayerJoin())
            {
                return;
            }
            m_pendingPlayers.Add(obj.GetComponent<PlayerInputParticipant>());

            this.enabled = true;
        }

        private void Awake()
        {
            var manager = GetComponent<PlayerInputManager>();
            if (m_playerPrefab != null)
            {
                manager.playerPrefab = m_playerPrefab.gameObject;
            }

            m_addPlayerToGameModeOnPlayerJoin = m_addToGameModeOnPlayerJoin;
        }

        private void OnValidate()
        {
            var manager = GetComponent<PlayerInputManager>();
            if (m_playerPrefab != null)
            {
                manager.playerPrefab = m_playerPrefab.gameObject;
            }
        }

        private void Start()
        {
            if (m_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(this);
            }

            Instance = this;

            m_manager = GetComponent<PlayerInputManager>();
            m_manager.onPlayerJoined += OnHumanPlayerJoined;
            m_manager.onPlayerLeft += OnHumanPlayerLeft;

            if (m_enablePlayerJoiningByDefault)
            {
                EnablePlayerJoining();
            }
            else
            {
                DisablePlayerJoining();
            }

            this.enabled = false;
        }

        private void FixedUpdate()
        {
            if (m_pendingPlayers == null || m_pendingPlayers.Count == 0)
            {
                this.enabled = false;
            }

            for (int i = 0, c = m_pendingPlayers.Count; i < c; ++i)
            {
                m_pendingPlayers[i].gameObject.transform.parent = this.transform;

                if (m_pendingPlayers[i].IsAI)
                {
                    m_pendingPlayers[i].gameObject.name = m_pendingPlayers[i].gameObject.name.Replace("(Clone)", $"#{m_bots.Count}");
                    AIParticipant newBotPlayer = m_pendingPlayers[i] as AIParticipant;
                    Debug.Assert(newBotPlayer, $"{this.name}: player '{m_pendingPlayers[i].name}' is not am AI player.");
                    m_bots.Add(newBotPlayer);
                }
                else
                {
                    m_pendingPlayers[i].gameObject.name = m_pendingPlayers[i].gameObject.name.Replace("(Clone)", $"#{m_players.Count}");
                    PlayerInputParticipant newHumanPlayer = m_pendingPlayers[i] as PlayerInputParticipant;
                    Debug.Assert(newHumanPlayer, $"{this.name}: player '{m_pendingPlayers[i].name}' is not a human player.");
                    m_players.Add(newHumanPlayer);

                    OnHumanPlayerJoined(newHumanPlayer.PlayerInput);
                }
            }
            m_pendingPlayers.Clear();

            if (m_addPlayerToGameModeOnPlayerJoin)
            {
                AddPlayersToGameMode();
            }

            OnPlayerJoinedEvent?.Invoke();

            this.enabled = false;
        }
    }
}