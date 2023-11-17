using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;

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
    public class AudioManager : SingletonManager<AudioManager>
    {
        [Header("Audio Settings")]
        [SerializeField]
        private double m_audioStartDelay = 0.2;

        [SerializeField]
        private float m_audioFadeInTime = 0.25f;

        [SerializeField]
        private AnimationCurve m_audioFadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [SerializeField]
        private float m_audioFadeOutTime = 0.5f;

        [SerializeField]
        private AnimationCurve m_audioFadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Tooltip("Should a warning message be log when trying to play a resource that have not been loaded yet?")]
        [SerializeField]
        private bool m_enableHotLoadingWarning = false;

        [Header("Audio Snapshots")]
        [SerializeField]
        private AudioMixerSnapshot m_defaultAudioSnapshot;

        [SerializeField]
        private float m_defaultSnapshotTransitionInSeconds = 0.5f;

        [SerializeField]
        private AudioMixerSnapshot m_muteAudioSnapshot;

        [SerializeField]
        private float m_muteSnapshotTransitionInSeconds = 0.5f;

        [SerializeField]
        private AudioMixerSnapshot m_pauseAudioSnapshot;

        [SerializeField]
        private float m_pauseSnapshotTransitionInSeconds = 0.5f;

        [Header("3D Audio")]
        [SerializeField]
        private AudioSource m_3DAudioSourceTemplate;

        [Header("Debug")]
        [SerializeField]
        private bool m_logDebug = false;

        private AsyncOperationHandle<AudioClip>[] m_audioToRelease = new AsyncOperationHandle<AudioClip>[2];

        private AudioHandle[] m_audioHandles;

        private Dictionary<AudioDefinition, AudioHandle> m_audioHandlesDictionary = new Dictionary<AudioDefinition, AudioHandle>();

        private HashSet<AudioStitcherDefinition> m_audioStitchers = new HashSet<AudioStitcherDefinition>();

        protected override AudioManager GetInstance()
        {
            return this;
        }

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

            bool isAudioHandleRegistered = m_audioHandlesDictionary.ContainsKey(audioDefinition);
            bool isAudioHandleCreated = isAudioHandleRegistered && m_audioHandlesDictionary[audioDefinition] != null && m_audioHandlesDictionary[audioDefinition].audioSource != null;
            if (isAudioHandleCreated)
            {
                if (m_audioHandlesDictionary[audioDefinition].IsLoading)
                {
                    return;
                }
                else
                {
                    m_audioHandlesDictionary[audioDefinition].Load();
                }

                return;
            }

            if (m_logDebug)
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
                m_audioHandlesDictionary[audioDefinition] = audioHandle;
            }
            else
            {
                m_audioHandlesDictionary.Add(audioDefinition, audioHandle);
            }
        }

        public void LoadAudio(AudioStitcherDefinition audioStitcherDefinition)
        {
            Debug.Assert(audioStitcherDefinition);

            if (m_logDebug)
            {
                Debug.Log($"{this.name}.LoadAudio(AudioStitcherDefinition): {audioStitcherDefinition.name}");
            }

            if (m_audioStitchers.Contains(audioStitcherDefinition))
            {
                return;
            }

            foreach (var stitch in audioStitcherDefinition.StitchedAudios)
            {
                LoadAudio(stitch.AudioDefinition);
            }

            m_audioStitchers.Add(audioStitcherDefinition);
        }

        public void LoadAudioCollection(AudioCollection audioCollection)
        {
            Debug.Assert(audioCollection);

            if (m_logDebug)
            {
                Debug.Log($"{this.name}.LoadAudioCollection: {audioCollection.name}");
            }

            foreach (var audio in audioCollection.DataDefinitions)
            {
                LoadAudio(audio as AudioDefinition);
            }
        }

        // PLAY AUDIO
        public void Play3DAudio(AudioDefinition audioDefinition, Transform newParent)
        {
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.Play3DAudio(AudioDefinition ({audioDefinition.name}), Transform ({newParent.name}))");
            }

            if (!m_audioHandlesDictionary.ContainsKey(audioDefinition))
            {
                LogHotLoadingWarning(audioDefinition.name);
                LoadAudio(audioDefinition, true);
            }

            m_audioHandlesDictionary[audioDefinition].audioSource.transform.parent = newParent;
            m_audioHandlesDictionary[audioDefinition].audioSource.transform.localPosition = Vector3.zero;

            StartCoroutine(AudioHandle_PlayAudio_Coroutine(m_audioHandlesDictionary[audioDefinition], audioDefinition.CanStartDelayed));
        }

        public void Play3DAudio(AudioDefinition audioDefinition, Vector3 position)
        {
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.Play3DAudio(AudioDefinition ({audioDefinition.name}), Vector3 ({position}))");
            }

            if (!m_audioHandlesDictionary.ContainsKey(audioDefinition))
            {
                LogHotLoadingWarning(audioDefinition.name);
                LoadAudio(audioDefinition, true);
            }

            m_audioHandlesDictionary[audioDefinition].audioSource.transform.position = position;
            StartCoroutine(AudioHandle_PlayAudio_Coroutine(m_audioHandlesDictionary[audioDefinition], audioDefinition.CanStartDelayed));
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
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.PlayAudio(AudioDefinition): {audioDefinition.name}");
            }

            if (!m_audioHandlesDictionary.ContainsKey(audioDefinition))
            {
                LogHotLoadingWarning(audioDefinition.name);
                LoadAudio(audioDefinition);
            }

            StartCoroutine(AudioHandle_PlayAudio_Coroutine(m_audioHandlesDictionary[audioDefinition], audioDefinition.CanStartDelayed));
        }

        public void PlayAudio(AudioStitcherDefinition audioStitcherDefinition)
        {
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.PlayAudio(AudioStitcherDefinition): {audioStitcherDefinition.name}");
            }

            if (!m_audioStitchers.Contains(audioStitcherDefinition))
            {
                // No logs here as we might have load a collection holding this stitcher's audio resources.
                LoadAudio(audioStitcherDefinition);
            }

            for (int i = 0, c = audioStitcherDefinition.StitchedAudios.Length; i < c; ++i)
            {
                var audioDefinition = audioStitcherDefinition.StitchedAudios[i].AudioDefinition;
                if (!m_audioHandlesDictionary.ContainsKey(audioDefinition))
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
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.FadeInAndPlayAudio(AudioDefinition): {audioDefinition.name}");
            }

            if (!m_audioHandlesDictionary.ContainsKey(audioDefinition))
            {
                Debug.LogWarning($"{this.name}: {audioDefinition.name} hasn't been loaded yet. Loading now, this might affect the performance." +
                    $"Prefer calling LoadAudio first.");
                LoadAudio(audioDefinition);
            }

            StartCoroutine(AudioHandle_FadeInAndPlayAudio_Coroutine(m_audioHandlesDictionary[audioDefinition], audioDefinition.CanStartDelayed));
        }

        public void FadeOutAndStopAudio(AudioResourceDefinition baseAudioDefinition)
        {
            if (m_logDebug)
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
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.FadeOutAndStopAudio(AudioDefinition): {audioDefinition.name}");
            }

            if (!m_audioHandlesDictionary.ContainsKey(audioDefinition))
            {
                Debug.LogWarning($"{this.name}: {audioDefinition.name} hasn't been loaded yet. Loading now, this might affect the performance." +
                    $"Prefer calling LoadAudio first.");
                LoadAudio(audioDefinition);
            }

            StartCoroutine(AudioHandle_FadeOutAndStopAudio_Coroutine(m_audioHandlesDictionary[audioDefinition]));
        }

        public void FadeOutAndStopAllAudioResources()
        {
            foreach (var audio in m_audioHandlesDictionary.Keys)
            {
                FadeOutAndStopAudio(audio);
            }
        }

        // PAUSE AUDIO VOLUME
        public void PauseAudioVolume()
        {
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.PauseAudioVolume()");
            }

            m_pauseAudioSnapshot.TransitionTo(m_pauseSnapshotTransitionInSeconds);
            StartCoroutine(AudioHandle_PauseAudio_Coroutine(m_pauseSnapshotTransitionInSeconds));
        }

        // RESUME AUDIO VOLUME
        public void ResumeAudioSnapshot()
        {
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.ResumeAudioSnapshot()");
            }

            m_defaultAudioSnapshot.TransitionTo(m_defaultSnapshotTransitionInSeconds);
            StartCoroutine(AudioHandle_ResumeAudio_Coroutine(m_defaultSnapshotTransitionInSeconds));
        }

        // UNLOAD AUDIO
        public void UnloadAudio(AudioResourceDefinition baseAudioDefinition)
        {
            if (m_logDebug)
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
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.UnloadAudio(AudioStitcherDefinition): {audioStitcherDefinition.name}");
            }

            if (m_audioStitchers.Contains(audioStitcherDefinition))
            {
                foreach (var stitch in audioStitcherDefinition.StitchedAudios)
                {
                    UnloadAudio(stitch.AudioDefinition);
                }

                m_audioStitchers.Remove(audioStitcherDefinition);
                return;
            }

            Debug.LogWarning($"{this.name}: {audioStitcherDefinition.name} hasn't been loaded yet. Nothing to unload.");
        }

        public void UnloadAudio(AudioDefinition audioDefinition)
        {
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.UnloadAudio(AudioDefinition): {audioDefinition.name}");
            }

            if (m_audioHandlesDictionary.ContainsKey(audioDefinition))
            {
                AudioHandle_ReleaseAudio(m_audioHandlesDictionary[audioDefinition]);
                m_audioHandlesDictionary[audioDefinition] = null;
                m_audioHandlesDictionary.Remove(audioDefinition);
                return;
            }

            Debug.LogWarning($"{this.name}: {audioDefinition.name} hasn't been loaded yet. Nothing to unload.");
        }

        public void UnloadAudioCollection(AudioCollection audioCollection)
        {
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.UnloadAudioCollection(AudioCollection): {audioCollection.name}");
            }

            foreach (var audio in audioCollection.GetData())
            {
                UnloadAudio(audio);
            }
        }

        // FADE AUDIO
        public void FadeInAudioSnapshot()
        {
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.FadeInAudioSnapshot()");
            }

            m_defaultAudioSnapshot.TransitionTo(m_defaultSnapshotTransitionInSeconds);
        }

        public void FadeOutAudioSnapshot()
        {
            if (m_logDebug)
            {
                Debug.Log($"{this.name}.FadeOutAudioSnapshot()");
            }

            m_muteAudioSnapshot.TransitionTo(m_muteSnapshotTransitionInSeconds);
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
                audioHandle.Play(m_audioStartDelay);
            }
            else
            {
                audioHandle.Play(0.0);
            }

            // wait for the one shot to end before releasing resource
            while (audioHandle.IsPlaying)
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

            audioHandle.SetAudioSourceVolume(m_audioFadeInCurve.Evaluate(0));

            if (canStartDelayed)
            {
                audioHandle.Play(m_audioStartDelay);
                // Waiting a little bit before to start the audio fade in.
                yield return new WaitForSeconds((float)m_audioStartDelay * 0.7f);
            }
            else
            {
                audioHandle.Play();
            }

            float elapsedTime = 0.0f;
            while (elapsedTime < m_audioFadeInTime)
            {
                audioHandle.SetAudioSourceVolume(m_audioFadeInCurve.Evaluate(elapsedTime / m_audioFadeInTime));
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

            while (elapsedTime < m_audioFadeOutTime)
            {
                audioHandle.SetAudioSourceVolume(m_audioFadeOutCurve.Evaluate(elapsedTime / m_audioFadeOutTime));
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

            foreach (var audio in m_audioHandlesDictionary.Values)
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

            foreach (var audio in m_audioHandlesDictionary.Values)
            {
                if (!audio.IsPlaying)
                {
                    audio.Resume();
                }
            }
        }

        private void AudioHandle_ReleaseAudio(AudioHandle audioHandle)
        {
            // If resource already released (or releasing), no more work needed.
            // if (audioHandle.IsResourceReleased || audioHandle.IsFadingOut)
            // {
            //     return;
            // }

            if (!audioHandle.IsResourceReleased)
            {
                // If audio has been stopped already, only need to release resource.
                audioHandle.StopAndReleaseResource();
            }

            Destroy(audioHandle.audioSource.gameObject);

            audioHandle.audioSource = null;

            m_audioHandlesDictionary.Remove(audioHandle.Definition);
        }

        private IEnumerator AudioHandle_PlayStitchedAudio_Coroutine(AudioStitcherDefinition audioStitcherDefinition)
        {
            bool isFirstAudio = true;
            double scheduledStartTime = audioStitcherDefinition.CanStartDelayed ? m_audioStartDelay : 0.0;
            foreach (var stitch in audioStitcherDefinition.StitchedAudios)
            {
                if (!m_audioHandlesDictionary.ContainsKey(stitch.AudioDefinition))
                {
                    Debug.LogError($"{this.name}: {stitch.AudioDefinition.name} of {audioStitcherDefinition.name} hasn't been loaded yet. Cannot play.");
                    continue;
                }

                var audioHandle = m_audioHandlesDictionary[stitch.AudioDefinition];

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
            if (m_enableHotLoadingWarning)
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
                this.audioSource.volume = volume;
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