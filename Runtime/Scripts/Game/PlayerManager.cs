using NaughtyAttributes;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    [RequireComponent(typeof(PlayerInputManager))]
    public class PlayerManager : Singleton<PlayerManager>
    {
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
        private bool m_addToGameModeOnPlayerJoin = true;

        [Header("Events")]
        public UnityEvent OnParticipantJoinedEvent;

        public UnityEvent OnParticipantLeftEvent;

        [Header("Split-Screen")]
        [SerializeField, Tooltip("Override PlayerInputManager splitscreen behavior." +
            "This allows AI participant to be handled.")]
        private bool m_enableSplitScreen = false;

        [SerializeField] private SplitScreenRatioCollection m_splitScreenViewportCollection;

        public int ParticipantCount => m_participants.Count;
        public IReadOnlyList<GameModeParticipant> Participants => m_participants;

        private PlayerInputManager m_manager;
        private GameModeManager m_gamemode;
        private List<GameModeParticipant> m_participants = new List<GameModeParticipant>();
        private List<GameModeParticipant> m_pendingParticipants = new List<GameModeParticipant>();
        private bool m_addPlayerToGameModeOnPlayerJoin;

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

        public virtual void SetActiveParticipantsInput(bool enable)
        {
            foreach (var p in m_participants)
            {
                if (enable)
                {
                    p.EnableInput();
                }
                else
                {
                    p.DisableInput();
                }
            }
        }

        public virtual void SetActivePlayersInput(bool enable)
        {
            foreach (var p in m_participants)
            {
                if (p.IsAI)
                {
                    continue;
                }

                if (enable)
                {
                    p.EnableInput();
                }
                else
                {
                    p.DisableInput();
                }
            }
        }

        public virtual void SetActiveAIPlayersInput(bool enable)
        {
            foreach (var p in m_participants)
            {
                if (!p.IsAI)
                {
                    continue;
                }

                if (enable)
                {
                    p.EnableInput();
                }
                else
                {
                    p.DisableInput();
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
            m_pendingParticipants.Add(bot);

            this.enabled = true;
        }

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public virtual void RemoveAIPlayer(GameModeParticipant botToRemove = null, bool removeFromGameMode = true)
        {
            if (botToRemove == null)
            {
                foreach (var p in m_participants)
                {
                    if (!p.IsAI)
                    {
                        continue;
                    }

                    botToRemove = p;
                }

                if (botToRemove == null)
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

            m_participants.Remove(botToRemove);
            OnParticipantLeftEvent?.Invoke();
        }

        private bool CanNewPlayerJoin()
        {
            if (m_participants.Count >= m_playerLimit)
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

            for (int i = m_participants.Count - 1; i >= 0; --i)
            {
                if (m_gamemode.IsParticipantAlreadyInGameMode(m_participants[i]))
                {
                    continue;
                }

                if (!m_gamemode.AddPlayer(m_participants[i]))
                {
                    Debug.LogWarning($"{this}: failed to add {m_participants[i].name} to the game mode. Disabling participant.");
                    m_participants[i].gameObject.SetActive(false);
                    m_participants.RemoveAt(i);
                }
            }

            // No longer need as there is no more distinction
            //for (int i = m_bots.Count - 1; i >= 0; --i)
            //{
            //    if (m_gamemode.IsParticipantAlreadyInGameMode(m_bots[i]))
            //    {
            //        continue;
            //    }

            //    if (!m_gamemode.AddAIPlayer(m_bots[i]))
            //    {
            //        Debug.LogWarning($"{this}: failed to add {m_bots[i].name} to the game mode. Disabling bot.");
            //        m_bots[i].gameObject.SetActive(false);
            //        m_bots.RemoveAt(i);
            //    }
            //}
        }

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        public virtual void RemovePlayersFromGameMode()
        {
            m_gamemode = FindFirstObjectByType<GameModeManager>();
            if (!m_gamemode)
            {
                return;
            }

            foreach (var p in m_participants)
            {
                m_gamemode.RemovePlayer(p);
            }

            // No longer require as there is no more distinction
            //foreach (var p in m_bots)
            //{
            //    m_gamemode.RemoveAIPlayer(p);
            //}
        }

        public void EnableSplitScreen()
        {
            m_manager.splitScreen = false;
            m_enableSplitScreen = true;
            RefreshSplitScreenCameras();
        }

        public void DisableSplitScreen()
        {
            m_enableSplitScreen = false;
            RefreshSplitScreenCameras();
        }

        protected virtual void OnHumanPlayerLeft(PlayerInput obj)
        {
            m_participants.Remove(obj.GetComponent<PlayerInputParticipant>());
            OnParticipantLeftEvent?.Invoke();
        }

        protected virtual void OnHumanPlayerJoined(PlayerInput obj)
        {
            if (!CanNewPlayerJoin())
            {
                return;
            }
            m_pendingParticipants.Add(obj.GetComponent<PlayerInputParticipant>());

            this.enabled = true;
        }

        protected override void OnSingletonAwake()
        {
            var manager = GetComponent<PlayerInputManager>();
            manager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            if (m_enableSplitScreen)
            {
                manager.splitScreen = false;
            }

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

            // If this is called in Awake, Start is not going to be call as the component will be disabled.
            this.enabled = false;
        }

        private void FixedUpdate()
        {
            if (m_pendingParticipants == null || m_pendingParticipants.Count == 0)
            {
                this.enabled = false;
            }

            for (int i = 0, c = m_pendingParticipants.Count; i < c; ++i)
            {
                m_pendingParticipants[i].gameObject.transform.parent = this.transform;

                if (m_pendingParticipants[i].IsAI)
                {
                    m_pendingParticipants[i].gameObject.name = m_pendingParticipants[i].gameObject.name.Replace("(Clone)", $"#{m_participants.Count}");
                    AIParticipant newBotPlayer = m_pendingParticipants[i] as AIParticipant;
                    Debug.Assert(newBotPlayer, $"{this.name}: player '{m_pendingParticipants[i].name}' is not an AI player.");
                    m_participants.Add(newBotPlayer);
                }
                else
                {
                    m_pendingParticipants[i].gameObject.name = m_pendingParticipants[i].gameObject.name.Replace("(Clone)", $"#{m_participants.Count}");
                    PlayerInputParticipant newHumanPlayer = m_pendingParticipants[i] as PlayerInputParticipant;
                    Debug.Assert(newHumanPlayer, $"{this.name}: player '{m_pendingParticipants[i].name}' is not a human player.");
                    m_participants.Add(newHumanPlayer);

                    OnHumanPlayerJoined(newHumanPlayer.PlayerInput);
                }
            }
            m_pendingParticipants.Clear();

            if (m_addPlayerToGameModeOnPlayerJoin)
            {
                AddPlayersToGameMode();
            }

            OnParticipantJoinedEvent?.Invoke();

            if (m_enableSplitScreen)
            {
                RefreshSplitScreenCameras();
            }

            this.enabled = false;
        }

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void ToggleSplitScreen()
        {
            // We don't want to use the Unity PlayerManager split-screen.
            m_enableSplitScreen = !m_enableSplitScreen;
            RefreshSplitScreenCameras();
        }

        [Button]
        private void RefreshSplitScreenCameras()
        {
            if (!m_enableSplitScreen)
            {
                foreach (var p in m_participants)
                {
                    Camera camera = p.GetComponentInChildren<Camera>();
                    if (!camera)
                    {
                        continue;
                    }

                    camera.enabled = false;
                }
            }

            if (m_splitScreenViewportCollection == null)
            {
                Debug.LogWarning($"{this.name}: No {typeof(SplitScreenRatioDefinition).Name} collection supplied." +
                    $"Cannot update camera viewports.", this);
                return;
            }

            Debug.Assert(m_splitScreenViewportCollection.DataDefinitions.Length >= m_participants.Count,
            $"{this.name}: {m_participants.Count} participant(s) and only {m_splitScreenViewportCollection.DataDefinitions.Length} " +
            $"viewport element in the {m_splitScreenViewportCollection.name} collection.", this);

            int i = 0;
            foreach (var p in m_participants)
            {
                // Every participant need to have it's own Camera.
                Camera camera = p.GetComponentInChildren<Camera>();
                if (!camera)
                {
                    Debug.LogWarning($"{this.name}: {p.name} doesn't have a Camera in its children, cannot split-screen" +
                        $"this participant.", this);
                    continue;
                }

                camera.enabled = true;

                SplitScreenRatioDefinition definition = m_splitScreenViewportCollection.GetData()[m_participants.Count];
                Debug.Assert(definition.Viewports.Count >= i,
                    $"{this.name}: Not enough viewport rect in {definition.name}, expecting at least {i + 1}.", this);

                camera.rect = m_splitScreenViewportCollection.GetData()[m_participants.Count - 1].Viewports[i];

                CinemachineBrain cmBrain = camera.GetComponent<CinemachineBrain>();
                if (cmBrain)
                {
                    cmBrain.ChannelMask = (OutputChannels)((int)OutputChannels.Channel01 << i);

                    CinemachineCamera targetCm = p.GetComponentInChildren<CinemachineCamera>();
                    if (targetCm)
                    {
                        targetCm.OutputChannel = cmBrain.ChannelMask;
                    }
                }

                ++i;
            }
        }
    }
}