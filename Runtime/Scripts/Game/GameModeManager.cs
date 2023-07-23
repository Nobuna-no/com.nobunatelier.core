using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace NobunAtelier
{
    public class GameModeManager : MonoBehaviour
    {
        public static GameModeManager Instance { get; private set; }

        public IReadOnlyList<GameModeParticipant> PlayersController => m_characterControllers;
        public LegacyPlayerControllerBase PlayerControllerPrefab => m_playerControllerPrefab;
        public LegacyAIControlerBase AIControllerPrefab => m_aiControllerPrefab;
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
        private LegacyAIControlerBase m_aiControllerPrefab;

        [SerializeField, ShowIf("m_instantiateController")]
        private bool m_enableInputOnJoin = true;

        [SerializeField]
        private bool m_instantiateCharacterMovement = true;

        [SerializeField, ShowIf("m_instantiateCharacterMovement")]
        private LegacyCharacterBase m_characterMovementPrefab;

        private List<GameModeParticipant> m_characterControllers = new List<GameModeParticipant>();

        public UnityEvent OnGameModeEnd;

        public virtual void ExitApplication()
        {
            Application.Quit();
        }

        public virtual void GameModeInit()
        {
            Instance = this;

            m_isInitialized = true;
            //RefreshPlayerList();
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
            for (int i = m_characterControllers.Count - 1; i >= 0; --i)
            {
                if (m_characterControllers[i].Controller.IsAI)
                {
                    var ai = m_characterControllers[i].Controller as LegacyAIControlerBase;

                    if (!ai)
                    {
                        continue;
                    }

                    // RemoveAI(ai, true);
                }
                else
                {
                    var controller = m_characterControllers[i].Controller as LegacyPlayerControllerBase;

                    if (!controller || !controller.PlayerInput)
                    {
                        continue;
                    }

                    RemovePlayer(m_characterControllers[i], true);
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

        public virtual void OnGameModePause() { }

        public virtual void OnGameModeResume() { }

        //public virtual void RefreshPlayerList()
        //{
        //    m_characterControllers.Clear();
        //    var allController = FindObjectsByType<BaseCharacterController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        //    foreach (var c in allController)
        //    {
        //        AddCharacterController(c);
        //    }
        //}

        //public virtual void AddCharacterController(BaseCharacterController controller)
        //{
        //    if (m_characterControllers.Contains(controller))
        //    {
        //        return;
        //    }

        //    m_characterControllers.Add(controller);
        //}

        //public virtual void RemoveCharacterController(BaseCharacterController controller)
        //{
        //    if (!m_characterControllers.Contains(controller))
        //    {
        //        return;
        //    }

        //    m_characterControllers.Remove(controller);
        //}

        public virtual void AddPlayer(GameModeParticipant participant)
        {
            if (m_characterControllers.Contains(participant))
            {
                return;
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

            basePlayerController.MountPlayerInput(humanPlayer.PlayerInput);

            if (m_enableInputOnJoin)
            {
                basePlayerController.EnableInput();
            }

            m_characterControllers.Add(participant);
        }

        public virtual void RemovePlayer(GameModeParticipant participant, bool destroyChildrenPrefab = true)
        {
            if (!m_characterControllers.Contains(participant))
            {
                return;
            }

            if (destroyChildrenPrefab)
            {
                if (participant.CharacterMovement)
                {
                    Destroy(participant.CharacterMovement.gameObject);
                }

                Destroy(participant.Controller.gameObject);
            }

            m_characterControllers.Remove(participant);
        }

        public virtual void AIAdd(PlayerInput player)
        {
            throw new System.NotImplementedException();
        }

        public virtual void AIRemove(PlayerInput player)
        {
            throw new System.NotImplementedException();
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