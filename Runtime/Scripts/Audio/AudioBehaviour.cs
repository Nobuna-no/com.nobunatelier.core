using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobunAtelier
{
    public class AudioBehaviour : MonoBehaviour
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
            OnEnable,
            OnDisable
        }

        [Header("State")]
        [SerializeField]
        private ActivationTrigger m_audioActionTrigger = ActivationTrigger.OnEnable;

        [SerializeField, ShowIf("DisplayDelay")]
        private float m_delayBeforeAudioActionInSecond = 0.0f;

        [Header("Audio")]
        [SerializeField]
        private AudioAction m_action = AudioAction.PlayResource;

        [SerializeField, ShowIf("DisplayAudioResourceDefinition")]
        private AudioResourceDefinition m_audioResourceDefinition;

        [SerializeField, ShowIf("DisplayAudioCollection")]
        private AudioCollection m_audioCollection;

        private bool m_didAction = false;

        private bool DisplayAudioResourceDefinition => m_action == AudioAction.LoadResource || m_action == AudioAction.PlayResource || m_action == AudioAction.UnloadResource
            || m_action == AudioAction.FadeInResource || m_action == AudioAction.FadeOutResource;

        private bool DisplayAudioCollection => m_action == AudioAction.LoadCollection || m_action == AudioAction.UnloadCollection;

        private void OnEnable()
        {
            if (m_audioActionTrigger == ActivationTrigger.OnEnable)
            {
                if (m_delayBeforeAudioActionInSecond <= 0f)
                {
                    DoAudioAction();
                }
                else
                {
                    StartCoroutine(UpdateRoutine());
                }
            }
        }

        private IEnumerator UpdateRoutine()
        {
            yield return new WaitForSeconds(m_delayBeforeAudioActionInSecond);
            DoAudioAction();
        }

        private void OnDisable()
        {
            if (m_audioActionTrigger == ActivationTrigger.OnDisable)
            {
                DoAudioAction();
            }
            else if (m_audioActionTrigger == ActivationTrigger.OnEnable && !m_didAction)
            {
                StopAllCoroutines();
                Debug.LogWarning($"{this.name}: did not had time to call DoAudioAction before object was disabled, calling now.");
                DoAudioAction();
            }
        }

        public void DoAudioAction()
        {
            m_didAction = true;
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
