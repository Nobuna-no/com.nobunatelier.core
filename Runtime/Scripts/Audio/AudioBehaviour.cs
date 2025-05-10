using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField, FormerlySerializedAs("m_audioActionTrigger")]
        private ActivationTrigger m_AudioActionTrigger = ActivationTrigger.OnEnable;

        [SerializeField, ShowIf("DisplayDelay")]
        [FormerlySerializedAs("m_delayBeforeAudioActionInSecond")]
        private float m_DelayBeforeAudioActionInSecond = 0.0f;

        [Header("Audio")]
        [SerializeField]
        [FormerlySerializedAs("m_action")]
        private AudioAction m_Action = AudioAction.PlayResource;

        [SerializeField, ShowIf("DisplayAudioResourceDefinition")]
        [FormerlySerializedAs("m_audioResourceDefinition")]
        private AudioResourceDefinition m_AudioResourceDefinition;

        [SerializeField, ShowIf("DisplayAudioCollection")]
        [FormerlySerializedAs("m_audioCollection")]
        private AudioCollection m_AudioCollection;

        private bool m_DidAction = false;

        private bool DisplayAudioResourceDefinition => m_Action == AudioAction.LoadResource || m_Action == AudioAction.PlayResource || m_Action == AudioAction.UnloadResource
            || m_Action == AudioAction.FadeInResource || m_Action == AudioAction.FadeOutResource;

        private bool DisplayAudioCollection => m_Action == AudioAction.LoadCollection || m_Action == AudioAction.UnloadCollection;

        private void OnEnable()
        {
            if (m_AudioActionTrigger == ActivationTrigger.OnEnable)
            {
                if (m_DelayBeforeAudioActionInSecond <= 0f)
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
            yield return new WaitForSeconds(m_DelayBeforeAudioActionInSecond);
            DoAudioAction();
        }

        private void OnDisable()
        {
            if (m_AudioActionTrigger == ActivationTrigger.OnDisable)
            {
                DoAudioAction();
            }
            else if (m_AudioActionTrigger == ActivationTrigger.OnEnable && !m_DidAction)
            {
                StopAllCoroutines();
                Debug.LogWarning($"{this.name}: did not had time to call DoAudioAction before object was disabled, calling now.");
                DoAudioAction();
            }
        }

        public void DoAudioAction()
        {
            m_DidAction = true;
            Debug.Assert(AudioManager.Instance, $"{this.name}: AudioManager instance is null!");

            switch (m_Action)
            {
                case AudioAction.LoadResource:
                    AudioManager.Instance.LoadAudio(m_AudioResourceDefinition);
                    break;

                case AudioAction.UnloadResource:
                    AudioManager.Instance.UnloadAudio(m_AudioResourceDefinition);
                    break;

                case AudioAction.PlayResource:
                    AudioManager.Instance.PlayAudio(m_AudioResourceDefinition);
                    break;

                case AudioAction.FadeInResource:
                    AudioManager.Instance.FadeInAndPlayAudio(m_AudioResourceDefinition);
                    break;

                case AudioAction.FadeOutResource:
                    AudioManager.Instance.FadeOutAndStopAudio(m_AudioResourceDefinition);
                    break;

                case AudioAction.LoadCollection:
                    AudioManager.Instance.LoadAudioCollection(m_AudioCollection);
                    break;

                case AudioAction.UnloadCollection:
                    AudioManager.Instance.UnloadAudioCollection(m_AudioCollection);
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
