using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

// What is the goal of this Manager?
// - Play a sound?
//      - Which kind of sound? Music? SFX? Both?
//          - Music seems to be the best candidate as SFX need to be spawned in the scene
//          - SFX tends to be loaded with the scene/objects that need them
//      - What is the benefit of using AssetReferences?
//          - We can load the audio on demand, and unload it when we don't need it anymore.
//          - We can use the same audio in multiple places without having to load it multiple times.
//          - No need to keep a reference to the audio clip in the scene.
// For now only static audio will be handled by this manager.

namespace NobunAtelier
{
    /// <summary>
    /// Provides methods for loading, playing, pausing, resuming, and unloading audio resources.
    /// It also handles 3D audio and audio fading in and out (using Unity's AudioMixerSnapshot).
    /// </summary>
    public class AudioManager : MonoBehaviourService<AudioManager>
    {
        [Header("Audio Settings")]
        [SerializeField]
        [FormerlySerializedAs("m_audioStartDelay")]
        private double m_AudioStartDelay = 0.2;

        [SerializeField]
        [FormerlySerializedAs("m_audioFadeInTime")]
        private float m_AudioFadeInTime = 0.25f;

        [SerializeField]
        [FormerlySerializedAs("m_audioFadeInCurve")]
        private AnimationCurve m_AudioFadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [SerializeField]
        [FormerlySerializedAs("m_audioFadeOutTime")]
        private float m_AudioFadeOutTime = 0.5f;

        [SerializeField]
        [FormerlySerializedAs("m_audioFadeOutCurve")]
        private AnimationCurve m_AudioFadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Tooltip("Should a warning message be log when trying to play a resource that have not been loaded yet?")]
        [SerializeField]
        [FormerlySerializedAs("m_enableHotLoadingWarning")]
        private bool m_EnableHotLoadingWarning = false;

        [Header("Audio Snapshots")]
        [SerializeField]
        [FormerlySerializedAs("m_defaultAudioSnapshot")]
        private AudioMixerSnapshot m_DefaultAudioSnapshot;

        [SerializeField]
        [FormerlySerializedAs("m_defaultSnapshotTransitionInSeconds")]
        private float m_DefaultSnapshotTransitionInSeconds = 0.5f;

        [SerializeField]
        [FormerlySerializedAs("m_muteAudioSnapshot")]
        private AudioMixerSnapshot m_MuteAudioSnapshot;

        [SerializeField]
        [FormerlySerializedAs("m_muteSnapshotTransitionInSeconds")]
        private float m_MuteSnapshotTransitionInSeconds = 0.5f;

        [SerializeField]
        [FormerlySerializedAs("m_pauseAudioSnapshot")]
        private AudioMixerSnapshot m_PauseAudioSnapshot;

        [SerializeField]
        [FormerlySerializedAs("m_pauseSnapshotTransitionInSeconds")]
        private float m_PauseSnapshotTransitionInSeconds = 0.5f;

        [Header("3D Audio")]
        [SerializeField]
        private AudioSource m_3DAudioSourceTemplate;

        [Header("Debug")]
        [SerializeField]
        [FormerlySerializedAs("m_logDebug")]
        private bool m_LogDebug = false;

        private Dictionary<AudioDefinition, AudioHandle> m_AudioHandlesDictionary = new Dictionary<AudioDefinition, AudioHandle>();

        private HashSet<AudioStitcherDefinition> m_AudioStitchers = new HashSet<AudioStitcherDefinition>();

        // LOAD AUDIO
        public void LoadAudio(AudioResourceDefinition baseAudioDefinition)
        {
            Debug.Assert(baseAudioDefinition);

            var audioDefinition = baseAudioDefinition as AudioDefinition;
            if (audioDefinition)
            {
                LoadAudio(audioDefinition);
                return;
            }

            var audioStitcher = baseAudioDefinition as AudioStitcherDefinition;
            if (audioStitcher)
            {
                LoadAudio(audioStitcher);
                return;
            }

            Debug.LogError($"{this.name}.LoadAudio: {baseAudioDefinition.name} type is not recognized!");
        }

        public void LoadAudio(AudioDefinition audioDefinition, bool is3DAudio = false)
        {
            Debug.Assert(audioDefinition);

            bool isAudioHandleRegistered = m_AudioHandlesDictionary.ContainsKey(audioDefinition);
            bool isAudioHandleCreated = isAudioHandleRegistered && m_AudioHandlesDictionary[audioDefinition] != null && m_AudioHandlesDictionary[audioDefinition].audioSource != null;
            if (isAudioHandleCreated)
            {
                if (m_AudioHandlesDictionary[audioDefinition].IsLoading)
                {
                    return;
                }
                else
                {
                    m_AudioHandlesDictionary[audioDefinition].Load();
                }

                return;
            }

            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.LoadAudio(AudioDefinition): {audioDefinition.name}");
            }

            AudioHandle audioHandle = null;
            if (!is3DAudio)
            {
                GameObject child = new GameObject($"AudioSource - {audioDefinition.name}");
                child.transform.parent = transform;
                child.isStatic = true;

                AudioSource audioSource = child.AddComponent<AudioSource>();
                audioHandle = new AudioHandle(audioDefinition, audioSource);
            }
            else if (m_3DAudioSourceTemplate == null)
            {
                // In case no 3D audio source template is provided, we generate a very basic 3D source.
                GameObject gao = new GameObject($"AudioSource 3D - {audioDefinition.name}");
                gao.isStatic = false;
                AudioSource audioSource = gao.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1;
                audioHandle = new AudioHandle(audioDefinition, audioSource);
            }
            else
            {
                // This audio source is going to move in the world...
                GameObject gao = GameObject.Instantiate(m_3DAudioSourceTemplate.gameObject);
                gao.name = $"AudioSource 3D - {audioDefinition.name}";
                gao.isStatic = false;
                audioHandle = new AudioHandle(audioDefinition, gao.GetComponent<AudioSource>());
            }

            audioHandle.Load();
            if (isAudioHandleRegistered)
            {
                m_AudioHandlesDictionary[audioDefinition] = audioHandle;
            }
            else
            {
                m_AudioHandlesDictionary.Add(audioDefinition, audioHandle);
            }
        }

        public void LoadAudio(AudioStitcherDefinition audioStitcherDefinition)
        {
            Debug.Assert(audioStitcherDefinition);

            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.LoadAudio(AudioStitcherDefinition): {audioStitcherDefinition.name}");
            }

            if (m_AudioStitchers.Contains(audioStitcherDefinition))
            {
                return;
            }

            foreach (var stitch in audioStitcherDefinition.StitchedAudios)
            {
                LoadAudio(stitch.AudioDefinition);
            }

            m_AudioStitchers.Add(audioStitcherDefinition);
        }

        public void LoadAudioCollection(AudioCollection audioCollection)
        {
            Debug.Assert(audioCollection);

            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.LoadAudioCollection: {audioCollection.name}");
            }

            foreach (var audio in audioCollection.Definitions)
            {
                LoadAudio(audio as AudioDefinition);
            }
        }

        // PLAY AUDIO
        public void Play3DAudio(AudioDefinition audioDefinition, Transform newParent)
        {
            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.Play3DAudio(AudioDefinition ({audioDefinition.name}), Transform ({newParent.name}))");
            }

            if (!m_AudioHandlesDictionary.ContainsKey(audioDefinition))
            {
                LogHotLoadingWarning(audioDefinition.name);
                LoadAudio(audioDefinition, true);
            }

            m_AudioHandlesDictionary[audioDefinition].audioSource.transform.parent = newParent;
            m_AudioHandlesDictionary[audioDefinition].audioSource.transform.localPosition = Vector3.zero;

            StartCoroutine(AudioHandle_PlayAudio_Coroutine(m_AudioHandlesDictionary[audioDefinition], audioDefinition.CanStartDelayed));
        }

        public void Play3DAudio(AudioDefinition audioDefinition, Vector3 position)
        {
            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.Play3DAudio(AudioDefinition ({audioDefinition.name}), Vector3 ({position}))");
            }

            if (!m_AudioHandlesDictionary.ContainsKey(audioDefinition))
            {
                LogHotLoadingWarning(audioDefinition.name);
                LoadAudio(audioDefinition, true);
            }

            m_AudioHandlesDictionary[audioDefinition].audioSource.transform.position = position;
            StartCoroutine(AudioHandle_PlayAudio_Coroutine(m_AudioHandlesDictionary[audioDefinition], audioDefinition.CanStartDelayed));
        }

        public void PlayAudio(AudioResourceDefinition baseAudioDefinition)
        {
            var audioDefinition = baseAudioDefinition as AudioDefinition;
            if (audioDefinition)
            {
                PlayAudio(audioDefinition);
                return;
            }

            var audioStitcher = baseAudioDefinition as AudioStitcherDefinition;
            if (audioStitcher)
            {
                PlayAudio(audioStitcher);
                return;
            }

            Debug.LogError($"{this.name}.PlayAudio: {baseAudioDefinition.name} type is not recognized!");
        }

        public void PlayAudio(AudioDefinition audioDefinition)
        {
            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.PlayAudio(AudioDefinition): {audioDefinition.name}");
            }

            if (!m_AudioHandlesDictionary.ContainsKey(audioDefinition))
            {
                LogHotLoadingWarning(audioDefinition.name);
                LoadAudio(audioDefinition);
            }

            StartCoroutine(AudioHandle_PlayAudio_Coroutine(m_AudioHandlesDictionary[audioDefinition], audioDefinition.CanStartDelayed));
        }

        public void PlayAudio(AudioStitcherDefinition audioStitcherDefinition)
        {
            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.PlayAudio(AudioStitcherDefinition): {audioStitcherDefinition.name}");
            }

            if (!m_AudioStitchers.Contains(audioStitcherDefinition))
            {
                // No logs here as we might have load a collection holding this stitcher's audio resources.
                LoadAudio(audioStitcherDefinition);
            }

            for (int i = 0, c = audioStitcherDefinition.StitchedAudios.Length; i < c; ++i)
            {
                var audioDefinition = audioStitcherDefinition.StitchedAudios[i].AudioDefinition;
                if (!m_AudioHandlesDictionary.ContainsKey(audioDefinition))
                {
                    LogHotLoadingWarning(audioDefinition.name);
                    LoadAudio(audioDefinition);
                }
            }

            StartCoroutine(AudioHandle_PlayStitchedAudio_Coroutine(audioStitcherDefinition));
        }

        // FADE AUDIO
        public void FadeInAndPlayAudio(AudioResourceDefinition audioResourceDefinition)
        {
            var audioDefinition = audioResourceDefinition as AudioDefinition;
            if (audioDefinition)
            {
                FadeInAndPlayAudio(audioDefinition);
                return;
            }

            var audioStitcher = audioResourceDefinition as AudioStitcherDefinition;
            if (audioStitcher)
            {
                //for (int i = 0, c = audioStitcher.StitchedAudios.Length; i < c; ++i)
                //{
                // Audio stitcher doesn't support fade in yet.
                PlayAudio(audioStitcher);
                //}
                return;
            }

            Debug.LogError($"{this.name}.FadeInAndPlayAudio: {audioResourceDefinition.name} type is not recognized!");
        }

        public void FadeInAndPlayAudio(AudioDefinition audioDefinition)
        {
            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.FadeInAndPlayAudio(AudioDefinition): {audioDefinition.name}");
            }

            if (!m_AudioHandlesDictionary.ContainsKey(audioDefinition))
            {
                Debug.LogWarning($"{this.name}: {audioDefinition.name} hasn't been loaded yet. Loading now, this might affect the performance." +
                    $"Prefer calling LoadAudio first.");
                LoadAudio(audioDefinition);
            }

            StartCoroutine(AudioHandle_FadeInAndPlayAudio_Coroutine(m_AudioHandlesDictionary[audioDefinition], audioDefinition.CanStartDelayed));
        }

        public void FadeOutAndStopAudio(AudioResourceDefinition baseAudioDefinition)
        {
            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.FadeOutAndStopAudio(AudioDefinition): {baseAudioDefinition.name}");
            }

            var audioDefinition = baseAudioDefinition as AudioDefinition;
            if (audioDefinition)
            {
                FadeOutAndStopAudio(audioDefinition);
                return;
            }

            var audioStitcher = baseAudioDefinition as AudioStitcherDefinition;
            if (audioStitcher)
            {
                for (int i = 0, c = audioStitcher.StitchedAudios.Length; i < c; ++i)
                {
                    FadeOutAndStopAudio(audioStitcher.StitchedAudios[i].AudioDefinition);
                }

                return;
            }

            Debug.LogError($"{this.name}.FadeOutAndStopAudio: {baseAudioDefinition.name} type is not recognized!");
        }

        public void FadeOutAndStopAudio(AudioDefinition audioDefinition)
        {
            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.FadeOutAndStopAudio(AudioDefinition): {audioDefinition.name}");
            }

            if (!m_AudioHandlesDictionary.ContainsKey(audioDefinition))
            {
                Debug.LogWarning($"{this.name}: {audioDefinition.name} hasn't been loaded yet. Loading now, this might affect the performance." +
                    $"Prefer calling LoadAudio first.");
                LoadAudio(audioDefinition);
            }

            StartCoroutine(AudioHandle_FadeOutAndStopAudio_Coroutine(m_AudioHandlesDictionary[audioDefinition]));
        }

        public void FadeOutAndStopAllAudioResources()
        {
            foreach (var audio in m_AudioHandlesDictionary.Keys)
            {
                FadeOutAndStopAudio(audio);
            }
        }

        // PAUSE AUDIO VOLUME
        public void PauseAudioVolume()
        {
            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.PauseAudioVolume()");
            }

            m_PauseAudioSnapshot.TransitionTo(m_PauseSnapshotTransitionInSeconds);
            StartCoroutine(AudioHandle_PauseAudio_Coroutine(m_PauseSnapshotTransitionInSeconds));
        }

        // RESUME AUDIO VOLUME
        public void ResumeAudioSnapshot()
        {
            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.ResumeAudioSnapshot()");
            }

            m_DefaultAudioSnapshot.TransitionTo(m_DefaultSnapshotTransitionInSeconds);
            StartCoroutine(AudioHandle_ResumeAudio_Coroutine(m_DefaultSnapshotTransitionInSeconds));
        }

        // UNLOAD AUDIO
        public void UnloadAudio(AudioResourceDefinition baseAudioDefinition)
        {
            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.UnloadAudio(AudioResourceDefinition): {baseAudioDefinition.name}");
            }

            var audioDefinition = baseAudioDefinition as AudioDefinition;
            if (audioDefinition)
            {
                UnloadAudio(audioDefinition);
                return;
            }

            var audioStitcher = baseAudioDefinition as AudioStitcherDefinition;
            if (audioStitcher)
            {
                UnloadAudio(audioStitcher);
                return;
            }

            Debug.LogError($"{this.name}.UnloadAudio: {baseAudioDefinition.name} type is not recognized!");
        }

        public void UnloadAudio(AudioStitcherDefinition audioStitcherDefinition)
        {
            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.UnloadAudio(AudioStitcherDefinition): {audioStitcherDefinition.name}");
            }

            if (m_AudioStitchers.Contains(audioStitcherDefinition))
            {
                foreach (var stitch in audioStitcherDefinition.StitchedAudios)
                {
                    UnloadAudio(stitch.AudioDefinition);
                }

                m_AudioStitchers.Remove(audioStitcherDefinition);
                return;
            }


            if (m_LogDebug)
            {
                Debug.Log($"{this.name}: {audioStitcherDefinition.name} hasn't been loaded yet. Nothing to unload.");
            }
        }

        public void UnloadAudio(AudioDefinition audioDefinition)
        {
            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.UnloadAudio(AudioDefinition): {audioDefinition.name}");
            }

            if (m_AudioHandlesDictionary.ContainsKey(audioDefinition))
            {
                AudioHandle_ReleaseAudio(m_AudioHandlesDictionary[audioDefinition]);
                m_AudioHandlesDictionary[audioDefinition] = null;
                m_AudioHandlesDictionary.Remove(audioDefinition);
                return;
            }

            if (m_LogDebug)
            {
                // Not really important here as the resource is already unloaded, but still good to keep some log.
                Debug.Log($"{this.name}: {audioDefinition.name} hasn't been loaded yet. Nothing to unload.");
            }
        }

        public void UnloadAudioCollection(AudioCollection audioCollection)
        {
            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.UnloadAudioCollection(AudioCollection): {audioCollection.name}");
            }

            foreach (var audio in audioCollection.Definitions)
            {
                UnloadAudio(audio);
            }
        }

        // FADE AUDIO
        public void FadeInAudioSnapshot()
        {
            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.FadeInAudioSnapshot()");
            }

            m_DefaultAudioSnapshot.TransitionTo(m_DefaultSnapshotTransitionInSeconds);
        }

        public void FadeOutAudioSnapshot()
        {
            if (m_LogDebug)
            {
                Debug.Log($"{this.name}.FadeOutAudioSnapshot()");
            }

            m_MuteAudioSnapshot.TransitionTo(m_MuteSnapshotTransitionInSeconds);
        }

        private void OnDestroy()
        {
            var definitions = new List<AudioDefinition>(m_AudioHandlesDictionary.Keys);

            foreach (var key in definitions)
            {
                AudioHandle_ReleaseAudio(m_AudioHandlesDictionary[key]);
            }
            m_AudioHandlesDictionary.Clear();
            m_AudioStitchers.Clear();
        }

        private IEnumerator AudioHandle_PlayAudio_Coroutine(AudioHandle audioHandle, bool canStartDelayed)
        {
            while (!audioHandle.IsReadyToPlay())
            {
                yield return null;
            }

            if (audioHandle.HasBeenStopped)
            {
                yield break;
            }

            if (canStartDelayed)
            {
                audioHandle.Play(m_AudioStartDelay);
            }
            else
            {
                audioHandle.Play(0.0);
            }

            if (audioHandle.IsLooping)
            {
                yield break;
            }

            // wait for the one shot to end before releasing resource
            while (!audioHandle.HasBeenStopped && audioHandle.IsPlaying)
            {
                yield return new WaitForFixedUpdate();
            }

            audioHandle.StopAndReleaseResource();
        }

        private IEnumerator AudioHandle_FadeInAndPlayAudio_Coroutine(AudioHandle audioHandle, bool canStartDelayed)
        {
            while (!audioHandle.IsReadyToPlay())
            {
                yield return null;
            }

            if (audioHandle.HasBeenStopped)
            {
                yield break;
            }

            audioHandle.SetAudioSourceVolume(m_AudioFadeInCurve.Evaluate(0));

            if (canStartDelayed)
            {
                audioHandle.Play(m_AudioStartDelay);
                // Waiting a little bit before to start the audio fade in.
                yield return new WaitForSeconds((float)m_AudioStartDelay * 0.7f);
            }
            else
            {
                audioHandle.Play();
            }

            float elapsedTime = 0.0f;
            while (elapsedTime < m_AudioFadeInTime)
            {
                audioHandle.SetAudioSourceVolume(m_AudioFadeInCurve.Evaluate(elapsedTime / m_AudioFadeInTime));
                elapsedTime += Time.deltaTime;
                yield return null;

                if (audioHandle.HasBeenStopped || audioHandle.IsFadingOut)
                {
                    yield break;
                }
            }
        }

        private IEnumerator AudioHandle_FadeOutAndStopAudio_Coroutine(AudioHandle audioHandle)
        {
            // If resource already released (or releasing), no more work needed.
            // If audio has been stopped already, only need to release resource.
            if (audioHandle.IsResourceReleased || audioHandle.IsFadingOut || audioHandle.HasBeenStopped || !audioHandle.IsPlaying)
            {
                yield break;
            }

            audioHandle.StartFadeOut();

            // If audio is playing, need to fade out before releasing resource.
            float elapsedTime = 0.0f;

            while (elapsedTime < m_AudioFadeOutTime)
            {
                audioHandle.SetAudioSourceVolume(m_AudioFadeOutCurve.Evaluate(elapsedTime / m_AudioFadeOutTime));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            audioHandle.Stop();

            if (audioHandle.ReleaseResourceOnStop)
            {
                AudioHandle_ReleaseAudio(audioHandle);
            }
        }

        private IEnumerator AudioHandle_PauseAudio_Coroutine(float transitionTime)
        {
            yield return new WaitForSeconds(transitionTime);

            foreach (var audio in m_AudioHandlesDictionary.Values)
            {
                if (audio.IsPlaying)
                {
                    audio.Pause();
                }
            }
        }

        private IEnumerator AudioHandle_ResumeAudio_Coroutine(float transitionTime)
        {
            yield return new WaitForSeconds(transitionTime);

            foreach (var audio in m_AudioHandlesDictionary.Values)
            {
                if (!audio.IsPlaying)
                {
                    audio.Resume();
                }
            }
        }

        private void AudioHandle_ReleaseAudio(AudioHandle audioHandle)
        {
            if (!audioHandle.IsResourceReleased)
            {
                // If audio has been stopped already, only need to release resource.
                audioHandle.StopAndReleaseResource();
            }

            Destroy(audioHandle.audioSource.gameObject);

            audioHandle.audioSource = null;

            m_AudioHandlesDictionary.Remove(audioHandle.Definition);
        }

        private IEnumerator AudioHandle_PlayStitchedAudio_Coroutine(AudioStitcherDefinition audioStitcherDefinition)
        {
            bool isFirstAudio = true;
            double scheduledStartTime = audioStitcherDefinition.CanStartDelayed ? m_AudioStartDelay : 0.0;
            foreach (var stitch in audioStitcherDefinition.StitchedAudios)
            {
                if (!m_AudioHandlesDictionary.ContainsKey(stitch.AudioDefinition))
                {
                    Debug.LogError($"{this.name}: {stitch.AudioDefinition.name} of {audioStitcherDefinition.name} hasn't been loaded yet. Cannot play.");
                    continue;
                }

                var audioHandle = m_AudioHandlesDictionary[stitch.AudioDefinition];

                while (!audioHandle.IsReadyToPlay())
                {
                    yield return null;
                }

                if (audioHandle.HasBeenStopped)
                {
                    continue;
                }

                if (isFirstAudio)
                {
                    isFirstAudio = false;
                    scheduledStartTime = audioHandle.Play(scheduledStartTime + stitch.Delay);
                }
                else
                {
                    audioHandle.PlayScheduled(scheduledStartTime, stitch.Delay);
                }

                scheduledStartTime += audioHandle.ClipLength;
            }
        }

        private void LogHotLoadingWarning(string resourceName)
        {
            if (m_EnableHotLoadingWarning)
            {
                Debug.LogWarning($"{this.name}: {resourceName} hasn't been loaded yet. Loading now, this might affect the performance." +
                 $"Prefer calling LoadAudio first.");
            }
        }

        public class AudioHandle
        {
            public AssetReference audioAssetReference;
            public AudioSource audioSource;
            public AsyncOperationHandle<AudioResource> resourceHandle;
            private float originalAudioVolume = 1.0f;

            public AudioDefinition Definition { get; private set; }
            public double ClipLength => audioSource.clip.length;
            public bool HasBeenStopped { get; private set; } = false;
            public bool IsFadingOut { get; private set; } = false;
            public bool IsResourceReleased => !resourceHandle.IsValid();
            public bool IsPlaying => audioSource.isPlaying;
            public bool IsLoading => resourceHandle.IsValid();
            public bool IsLooping => this.audioSource.loop;
            public bool ReleaseResourceOnStop { get; private set; }

            public AudioHandle(AudioDefinition audioDefinition, AudioSource audioSource)
            {
                this.Definition = audioDefinition;
                this.audioAssetReference = audioDefinition.AudioAssetReference;
                this.audioSource = audioSource;
                this.audioSource.playOnAwake = false;
                this.audioSource.loop = audioDefinition.Loop;
                this.originalAudioVolume = audioDefinition.Volume;
                this.audioSource.volume = audioDefinition.Volume;
                this.audioSource.outputAudioMixerGroup = audioDefinition.MixerGroup;
                this.ReleaseResourceOnStop = audioDefinition.ReleaseResourceOnStop;
            }

            public void Load()
            {
                HasBeenStopped = false;

                if (!resourceHandle.IsValid())
                {
                    resourceHandle = audioAssetReference.LoadAssetAsync<AudioResource>();
                }
            }

            public double Play(double startDelay = 0.0)
            {
                HasBeenStopped = false;

                ResetAudioSourceVolume();
                this.audioSource.resource = resourceHandle.Result;

                double scheduledStartTime = 0;
                if (this.audioSource.resource is AudioClip)
                {
                    scheduledStartTime = AudioSettings.dspTime + startDelay;
                    this.audioSource.PlayScheduled(scheduledStartTime);
                }
                else
                {
                    this.audioSource.PlayDelayed((float)startDelay);
                }

                return scheduledStartTime;
            }

            public void PlayScheduled(double dspTime, double startDelay)
            {
                HasBeenStopped = false;

                ResetAudioSourceVolume();
                this.audioSource.resource = resourceHandle.Result;
                if (this.audioSource.resource is AudioClip)
                {
                    this.audioSource.PlayScheduled(dspTime + startDelay);
                }
                else
                {
                    this.audioSource.PlayDelayed((float)startDelay);
                }
            }

            public bool IsReadyToPlay()
            {
                Debug.Assert(resourceHandle.IsValid(), $"{audioSource.name} resource is not initialized yet.");
                return resourceHandle.IsDone;
            }

            public bool NeedInitialization()
            {
                return audioSource == null;
            }

            public void SetAudioSourceVolume(float volume)
            {
                this.audioSource.volume = originalAudioVolume * volume;
            }

            public void ResetAudioSourceVolume()
            {
                this.audioSource.volume = originalAudioVolume;
            }

            public void Pause()
            {
                this.audioSource.Pause();
            }

            public void Resume()
            {
                this.audioSource.UnPause();
            }

            public void Stop()
            {
                if (HasBeenStopped)
                {
                    return;
                }

                if (ReleaseResourceOnStop)
                {
                    StopAndReleaseResource();
                }
                else
                {
                    IsFadingOut = false;
                    HasBeenStopped = true;
                    this.audioSource.Stop();
                }
            }

            public void StartFadeOut()
            {
                IsFadingOut = true;
            }

            public void StopAndReleaseResource()
            {
                if (HasBeenStopped)
                {
                    return;
                }

                IsFadingOut = false;
                HasBeenStopped = true;
                this.audioSource.Stop();

                if (resourceHandle.IsValid())
                {
                    Addressables.Release(resourceHandle);
                }
                else
                {
                    Debug.LogWarning($"{this.audioSource.name} - resourceHandle has already be released. You might be trying to release several time the same audio handler.");
                }
            }
        }
    }
}