using UnityEngine;

namespace NobunAtelier
{
    public class CharacterControllerBase : MonoBehaviour
    {
        [SerializeField]
        protected Character m_controlledCharacter;

        public Character ControlledCharacter => m_controlledCharacter;

        [SerializeField]
        protected bool m_mountCharacterOnStart = true;

        protected virtual void Start()
        {
            if (!m_mountCharacterOnStart)
            {
                return;
            }

            m_controlledCharacter?.Mount(this);
        }

        protected virtual void UpdateController()
        { }

        protected virtual void OnEnable()
        { }

        protected virtual void OnDisable()
        { }
    }

    public abstract class CharacterControllerBase<T> : CharacterControllerBase
        where T : CharacterControllerModuleBase
    {
        [SerializeField]
        protected T[] m_modules;

        public abstract bool IsAI { get; }

        protected virtual void Awake()
        {
            if (!m_controlledCharacter)
            {
                Debug.LogWarning($"No Character set on {this}, searching for one in the parent hierarchy...");
                m_controlledCharacter = transform.parent.GetComponentInChildren<Character>();
                Debug.Assert(m_controlledCharacter, $"No Character found for {this.transform.parent.gameObject.name}/{this}");
            }

            foreach (var extension in m_modules)
            {
                extension.InitModule(this);
            }
        }

        protected override void UpdateController()
        {
            if (m_controlledCharacter == null)
            {
                return;
            }

            foreach (var module in m_modules)
            {
                if (!module.IsAvailable())
                {
                    continue;
                }

                module.UpdateModule(Time.deltaTime);
            }
        }

        protected override void OnEnable()
        {
            foreach (var module in m_modules)
            {
                module.enabled = true;
            }
        }

        protected override void OnDisable()
        {
            foreach (var module in m_modules)
            {
                module.enabled = false;
            }
        }
    }
}