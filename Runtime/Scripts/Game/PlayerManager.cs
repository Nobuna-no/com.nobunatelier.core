using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    [RequireComponent(typeof(PlayerInputManager))]
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }

        [Header("Player Manager")]
        [SerializeField, Required, InfoBox("This prefab will override the PlayerInputManager `Player Prefab`")]
        private Player m_playerPrefab;

        [SerializeField]
        private bool m_enablePlayerJoiningByDefault = true;

        [SerializeField]
        private bool m_dontDestroyOnLoad = true;

        [SerializeField]
        private bool m_addToGameModeOnPlayerJoin = true;

        private bool m_currentAddToGameModeOnPlayerJoin;

        private PlayerInputManager m_manager;
        private GameModeManager m_gamemode;
        private List<Player> m_players = new List<Player>();

        public virtual void EnablePlayerJoining()
        {
            m_manager.EnableJoining();
        }

        public virtual void EnablePlayerJoining(bool addToGameModeOnPlayerJoining)
        {
            m_manager.EnableJoining();
            m_currentAddToGameModeOnPlayerJoin = addToGameModeOnPlayerJoining;
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
                m_currentAddToGameModeOnPlayerJoin = m_addToGameModeOnPlayerJoin;
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

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public virtual void AddPlayersToGameMode()
        {
            m_gamemode = FindFirstObjectByType<GameModeManager>();
            if (!m_gamemode)
            {
                return;
            }

            foreach (var p in m_players)
            {
                m_gamemode.AddPlayer(p);
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
        }

        private void Awake()
        {
            var manager = GetComponent<PlayerInputManager>();
            if (m_playerPrefab == null)
            {
                return;
            }

            manager.playerPrefab = m_playerPrefab.gameObject;
            m_currentAddToGameModeOnPlayerJoin = m_addToGameModeOnPlayerJoin;
        }

        private void OnValidate()
        {
            var manager = GetComponent<PlayerInputManager>();
            if (m_playerPrefab == null)
            {
                return;
            }

            manager.playerPrefab = m_playerPrefab.gameObject;
        }

        private void Start()
        {
            if (m_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(this);
            }

            Instance = this;

            m_manager = GetComponent<PlayerInputManager>();
            m_manager.onPlayerJoined += OnPlayerJoined;
            m_manager.onPlayerLeft += OnPlayerLeft;

            if (m_enablePlayerJoiningByDefault)
            {
                EnablePlayerJoining();
            }
            else
            {
                DisablePlayerJoining();
            }
        }

        protected virtual void OnPlayerLeft(PlayerInput obj)
        {
            m_players.Remove(obj.GetComponent<Player>());
        }

        protected virtual void OnPlayerJoined(PlayerInput obj)
        {
            m_players.Add(obj.GetComponent<Player>());
            obj.gameObject.name = obj.gameObject.name.Replace("(Clone)", $"#{m_players.Count}");
            obj.gameObject.transform.parent = this.transform;

            if (m_currentAddToGameModeOnPlayerJoin)
            {
                AddPlayersToGameMode();
            }
        }
    }
}