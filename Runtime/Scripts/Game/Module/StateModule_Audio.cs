using NaughtyAttributes;
using UnityEngine;

namespace NobunAtelier
{
    [AddComponentMenu("NobunAtelier/States/Modules/StateModule: Audio")]
    public class StateModule_Audio : StateComponentModule
    {
        private enum AudioAction
        {
            LoadResource,
            LoadCollection,
            PlayResource,
            FadeInResource,
            FadeOutResource,
            UnloadResource,
            UnloadCollection,
            AudioPause,
            AudioResume,
            AudioFadeIn,
            AudioFadeOut,
        }

        public enum ActivationTrigger
        {
            Manual,
            OnStateEnter,
            OnStateExit
        }

        [Header("State")]
        [SerializeField]
        private ActivationTrigger m_audioActionTrigger = ActivationTrigger.OnStateEnter;
        [SerializeField, ShowIf("DisplayDelay")]
        private float m_delayBeforeAudioActionInSecond = 0.0f;

        [Header("Audio")]
        [SerializeField]
        private AudioAction m_action = AudioAction.PlayResource;

        [SerializeField, ShowIf("DisplayAudioResourceDefinition")]
        private AudioResourceDefinition m_audioResourceDefinition;

        [SerializeField, ShowIf("DisplayAudioCollection")]
        private AudioCollection m_audioCollection;

        private float m_currentDelay = 0.0f;

        private bool DisplayDelay => m_audioActionTrigger == ActivationTrigger.OnStateEnter;
        private bool DisplayAudioResourceDefinition => m_action == AudioAction.LoadResource || m_action == AudioAction.PlayResource || m_action == AudioAction.UnloadResource 
            || m_action == AudioAction.FadeInResource || m_action == AudioAction.FadeOutResource;
        private bool DisplayAudioCollection => m_action == AudioAction.LoadCollection || m_action == AudioAction.UnloadCollection;

        public override void Enter()
        {
            if (m_audioActionTrigger == ActivationTrigger.OnStateEnter)
            {
                if (m_delayBeforeAudioActionInSecond <= 0f)
                {
                    DoAudioAction();
                }
                else
                {
                    m_currentDelay = m_delayBeforeAudioActionInSecond;
                }
            }
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            if (m_audioActionTrigger != ActivationTrigger.OnStateEnter || m_currentDelay <= 0f)
            {
                return;
            }

            m_currentDelay -= deltaTime;
            if (m_currentDelay <= 0f)
            {
                DoAudioAction();
                m_currentDelay = 0.0f;
            }
        }

        public override void Exit()
        {
            if (m_audioActionTrigger == ActivationTrigger.OnStateExit)
            {
                DoAudioAction();
            }
            else if (m_audioActionTrigger == ActivationTrigger.OnStateEnter && m_currentDelay > 0f)
            {
                DoAudioAction();
                Debug.LogWarning($"{this.name}: did not had time to call DoAudioAction before state exit, calling now. Remaining delay is {m_currentDelay} sec.");
            }
        }

        public void DoAudioAction()
        {
            Debug.Assert(AudioManager.Instance, $"{this.name}: AudioManager instance is null!");

            switch (m_action)
            {
                case AudioAction.LoadResource:
                    AudioManager.Instance.LoadAudio(m_audioResourceDefinition);
                    break;

                case AudioAction.UnloadResource:
                    AudioManager.Instance.UnloadAudio(m_audioResourceDefinition);
                    break;

                case AudioAction.PlayResource:
                    AudioManager.Instance.PlayAudio(m_audioResourceDefinition);
                    break;

                case AudioAction.FadeInResource:
                    AudioManager.Instance.FadeInAndPlayAudio(m_audioResourceDefinition);
                    break;

                case AudioAction.FadeOutResource:
                    AudioManager.Instance.FadeOutAndStopAudio(m_audioResourceDefinition);
                    break;

                case AudioAction.LoadCollection:
                    AudioManager.Instance.LoadAudioCollection(m_audioCollection);
                    break;

                case AudioAction.UnloadCollection:
                    AudioManager.Instance.UnloadAudioCollection(m_audioCollection);
                    break;

                case AudioAction.AudioPause:
                    AudioManager.Instance.PauseAudioVolume();
                    break;

                case AudioAction.AudioResume:
                    AudioManager.Instance.ResumeAudioSnapshot();
                    break;

                case AudioAction.AudioFadeIn:
                    AudioManager.Instance.FadeInAudioSnapshot();
                    break;

                case AudioAction.AudioFadeOut:
                    AudioManager.Instance.FadeOutAudioSnapshot();
                    break;
            }
        }
    }
}