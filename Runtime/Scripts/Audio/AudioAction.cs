using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace NobunAtelier
{
    public class AudioAction : MonoBehaviour
    {
        private enum AudioActionType
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
        private AudioActionType m_Action = AudioActionType.PlayResource;

        [SerializeField, ShowIf("DisplayAudioResourceDefinition")]
        [FormerlySerializedAs("m_audioResourceDefinition")]
        private AudioResourceDefinition m_AudioResourceDefinition;

        [SerializeField, ShowIf("DisplayAudioCollection")]
        [FormerlySerializedAs("m_audioCollection")]
        private AudioCollection m_AudioCollection;

        private bool m_DidAction = false;

        private bool DisplayAudioResourceDefinition => m_Action == AudioActionType.LoadResource || m_Action == AudioActionType.PlayResource || m_Action == AudioActionType.UnloadResource
            || m_Action == AudioActionType.FadeInResource || m_Action == AudioActionType.FadeOutResource;

        private bool DisplayAudioCollection => m_Action == AudioActionType.LoadCollection || m_Action == AudioActionType.UnloadCollection;

        private bool DisplayDelay => m_AudioActionTrigger == ActivationTrigger.OnEnable;

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
                case AudioActionType.LoadResource:
                    AudioManager.Instance.LoadAudio(m_AudioResourceDefinition);
                    break;

                case AudioActionType.UnloadResource:
                    AudioManager.Instance.UnloadAudio(m_AudioResourceDefinition);
                    break;

                case AudioActionType.PlayResource:
                    AudioManager.Instance.PlayAudio(m_AudioResourceDefinition);
                    break;

                case AudioActionType.FadeInResource:
                    AudioManager.Instance.FadeInAndPlayAudio(m_AudioResourceDefinition);
                    break;

                case AudioActionType.FadeOutResource:
                    AudioManager.Instance.FadeOutAndStopAudio(m_AudioResourceDefinition);
                    break;

                case AudioActionType.LoadCollection:
                    AudioManager.Instance.LoadAudioCollection(m_AudioCollection);
                    break;

                case AudioActionType.UnloadCollection:
                    AudioManager.Instance.UnloadAudioCollection(m_AudioCollection);
                    break;

                case AudioActionType.AudioPause:
                    AudioManager.Instance.PauseAudioVolume();
                    break;

                case AudioActionType.AudioResume:
                    AudioManager.Instance.ResumeAudioSnapshot();
                    break;

                case AudioActionType.AudioFadeIn:
                    AudioManager.Instance.FadeInAudioSnapshot();
                    break;

                case AudioActionType.AudioFadeOut:
                    AudioManager.Instance.FadeOutAudioSnapshot();
                    break;
            }
        }
    }
}
