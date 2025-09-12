using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

namespace Ameba {


  /// <summary>
  /// Audio event data for UnityEvents with configurable AudioClip and settings
  /// </summary>
  [System.Serializable]
  public struct AudioEventData {
    [Tooltip("Audio clip to play")]
    public AudioClip clip;
    [Tooltip("Volume multiplier")]
    [Range(0f, 2f)]
    public float volume;
    [Tooltip("Pitch multiplier")]
    [Range(0.1f, 3f)]
    public float pitch;
    [Tooltip("Override default mixer group")]
    public AudioMixerGroup mixerGroup;

    public AudioEventData(AudioClip audioClip, float vol = 1f, float pitchValue = 1f, AudioMixerGroup mixer = null) {
      clip = audioClip;
      volume = vol;
      pitch = pitchValue;
      mixerGroup = mixer;
    }
  }


  /// <summary>
  /// Optimized <see cref="AudioSource"/> pool for frequent UI sounds using <see cref="LinkedPool{T}"/> for better performance
  /// </summary>
  public class AudioSourcePool : MonoBehaviour {

    [Header("Pool Settings")]
    [Tooltip("Pre-warm the pool on Awake")]
    public bool PrewarmOnAwake = true;
    [Tooltip("Maximum number of AudioSources in the pool")]
    [Range(1, 100)]
    public int MaxPoolSize = 10;
    [Tooltip("Initial number of AudioSources to pre-warm in the pool")]
    [Range(1, 20)]
    public int InitialPoolSize = 5;
    [Tooltip("Parent transform for pooled AudioSources. If empty will assume this GameObject")]
    public Transform poolParent;
    [Tooltip("Prefix for pooled AudioSource GameObjects")]
    public string GameObjectPrefix = "PooledAudioSource_";

    [Header("Audio Mixer Settings")]
    [Tooltip("Audio Mixer for routing AudioSources (optional)")]
    public AudioMixer AudioMixer;
    [Tooltip("Default Audio Mixer Group for AudioSources (optional)")]
    public AudioMixerGroup DefaultMixerGroup;

    [Header("Audio Settings")]
    [Tooltip("Default volume for AudioSources")]
    [Range(0f, 1f)]
    public float DefaultVolume = 1f;
    [Tooltip("Default pitch for AudioSources")]
    [Range(0.1f, 3f)]
    public float DefaultPitch = 1f;
    [Tooltip("If true, the pool will adjust the volume of audio clips when multiple sources at playing at once ")]
    public bool ScaleActiveSourcesVolume = true;
    [Tooltip("The factor to adjust the volume. Higher numbers reduce the volume more aggressively, lower numbers allow more sum of sounds (ie: can get louder)")]
    [Range(0.01f, 1f)]
    public float ActiveSourcesVolumeFactor = 0.1f;

    [Header("Randomization Settings")]
    public bool DisableRandomization = false;
    [Tooltip("Randomize volume within this range")]
    public Vector2 VolumeRange = new(0.1f, 2f);
    [Tooltip("Randomize pitch within this range")]
    public Vector2 PitchRange = new(0.3f, 1.5f);

    [Header("Editor Audio Events")]
    [Tooltip("Configure audio clips and settings for UI events in the Editor")]
    public AudioEventConfig[] PresetAudioEvents;


    private ObjectPool<AudioSource> audioSourcePool;

    #region Nested Types

    /// <summary>
    /// Configurable audio events that can be set up in the Editor
    /// </summary>
    [System.Serializable]
    public class AudioEventConfig {
      [Tooltip("Name/ID for this audio event")]
      public string eventName;
      [Tooltip("Audio clip to play")]
      public AudioClip clip;
      [Tooltip("Volume multiplier")]
      [Range(0f, 2f)]
      public float volume = 1f;
      [Tooltip("Pitch multiplier")]
      [Range(0.1f, 3f)]
      public float pitch = 1f;
      [Tooltip("Override mixer group (optional)")]
      public AudioMixerGroup mixerGroup;
      [Tooltip("Enable randomization for this event")]
      public bool enableRandomization = false;
    }

    #endregion



    #region Initialization

    private void Awake() {

      int initialPoolSize = Mathf.Clamp(InitialPoolSize, 1, MaxPoolSize);

      poolParent = (poolParent == null) ? transform : poolParent;
      GameObjectPrefix = string.IsNullOrEmpty(GameObjectPrefix) ? "PooledAudioSource_" : GameObjectPrefix;

      // Create the pool with creation, get, release, and destroy actions
      audioSourcePool = new ObjectPool<AudioSource>(
          createFunc: CreateAudioSource,
          actionOnGet: OnGetAudioSource,
          actionOnRelease: OnReleaseAudioSource,
          actionOnDestroy: OnDestroyAudioSource,
          collectionCheck: true, // Enable collection checks for debugging
          defaultCapacity: InitialPoolSize,
          maxSize: MaxPoolSize
      );

      // Pre-warm the pool
      if (PrewarmOnAwake) PrewarmPool();
    }

    private void Start() {
      LoadAudioMixer();
      LoadAudioMixerGroup();
    }
    private void PrewarmPool() {
      var tempSources = new AudioSource[InitialPoolSize];
      for (int i = 0; i < InitialPoolSize; i++) {
        tempSources[i] = audioSourcePool.Get();
      }

      for (int i = 0; i < InitialPoolSize; i++) {
        audioSourcePool.Release(tempSources[i]);
      }
    }

    // Load the Audio Mixer if assigned, or looks for it in the scene
    private void LoadAudioMixer() {
      if (AudioMixer != null) {
        AudioMixer.FindMatchingGroups("Master");
      }
      else {
        AudioMixer = Resources.Load<AudioMixer>("Audio/MainMixer");
      }
      if (AudioMixer == null) {
        Debug.LogWarning("No AudioMixer assigned or found in Resources/Audio/MainMixer");
      }
    }

    private void LoadAudioMixerGroup() {
      if (AudioMixer != null) {
        if (DefaultMixerGroup == null) {
          var groups = AudioMixer.FindMatchingGroups("Master");
          if (groups.Length > 0) {
            DefaultMixerGroup = groups[0];
          }
        }
      }
      if (DefaultMixerGroup == null) {
        Debug.LogWarning("AudioSourcePool: No DefaultMixerGroup assigned or found in AudioMixer");
      }
    }

    #endregion

    #region LinkedPool Callbacks

    private AudioSource CreateAudioSource() {
      GameObject audioObject = new($"{GameObjectPrefix}{audioSourcePool.CountAll}");
      if (poolParent != null) {
        audioObject.transform.SetParent(poolParent);
      }
      else {
        audioObject.transform.SetParent(transform);
      }
      var audioSource = InstantiateAudioSource();
      return audioSource;
    }

    /// <summary>
    /// Wrapper for creating a new AudioSource with default settings
    /// </summary>
    /// <param name="applyDefaultSettings"></param>
    /// <returns></returns>
    protected virtual AudioSource InstantiateAudioSource(bool applyDefaultSettings = true) {
      var audioSource = gameObject.AddComponent<AudioSource>();
      audioSource.playOnAwake = false;
      audioSource.spatialBlend = 0f; // 2D sound
      if (applyDefaultSettings) {
        if (AudioMixer != null && DefaultMixerGroup != null) {
          audioSource.outputAudioMixerGroup = DefaultMixerGroup;
        }
        audioSource.volume = DefaultVolume;
        audioSource.pitch = DefaultPitch;
        if (!DisableRandomization) {
          audioSource.volume *= Random.Range(VolumeRange.x, VolumeRange.y);
          audioSource.pitch *= Random.Range(PitchRange.x, PitchRange.y);
        }
      }
      audioSource.gameObject.SetActive(false);
      return audioSource;
    }

    private void OnGetAudioSource(AudioSource audioSource) => audioSource.gameObject.SetActive(true);

    private void OnReleaseAudioSource(AudioSource audioSource) {
      audioSource.Stop();
      audioSource.clip = null;
      audioSource.volume = 1f;
      audioSource.pitch = 1f;
      audioSource.gameObject.SetActive(false);
    }

    private void OnDestroyAudioSource(AudioSource audioSource) {
      if (audioSource != null && audioSource.gameObject != null) {
        DestroyImmediate(audioSource.gameObject);
      }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Get an AudioSource manually - for more control
    /// </summary>
    public AudioSource GetAudioSource() => audioSourcePool.Get();

    /// <summary>
    /// Releases the specified <see cref="AudioSource"/> back to the pool for reuse.
    /// </summary>
    /// <remarks>This method returns the provided <see cref="AudioSource"/> to the internal pool, making it
    /// available for future use. Ensure that the <see cref="AudioSource"/> is no longer in use before calling this
    /// method.</remarks>
    /// <param name="audioSource">The <see cref="AudioSource"/> instance to release. Must not be <see langword="null"/>.</param>
    public void ReleaseAudioSource(AudioSource audioSource) {
      if(audioSource != null && audioSource.gameObject != null)
        audioSourcePool.Release(audioSource);
    }

    /// <summary>
    /// Play a UI sound with automatic pooling - optimized for frequent calls
    /// </summary>
    public void PlayUISound(AudioClip clip, float volume = 1f, float pitch = 1f) {
      if (clip == null) return;

      float volumeScale = CalculateVolumeScale();

      AudioSource source = audioSourcePool.Get();
      source.clip = clip;
      source.volume = volume * volumeScale;
      source.pitch = pitch;
      source.Play();

      // Auto-release when done - no coroutine overhead for short UI clips
      StartCoroutine(AutoReleaseCoroutine(source, clip.length / pitch));
    }

    private float CalculateVolumeScale() {
      float volumeScale = 1f;
      if (ScaleActiveSourcesVolume) {
        // Optional: Scale volume based on how many sources are active
        int activeSources = audioSourcePool.CountActive;
        volumeScale = Mathf.Clamp01(1f - (activeSources * ActiveSourcesVolumeFactor)); // Reduce volume as more play
      }
      return volumeScale;
    }

    /// <summary>
    /// Play audio using AudioEventData struct - designed for UnityEvents
    /// </summary>
    public void PlaySound(AudioEventData audioData) {
      if (audioData.clip == null) return;

      AudioSource source = audioSourcePool.Get();
      source.clip = audioData.clip;
      source.volume = audioData.volume;
      source.pitch = audioData.pitch;

      // Override mixer group if specified
      if (audioData.mixerGroup != null) {
        source.outputAudioMixerGroup = audioData.mixerGroup;
      }

      source.Play();
      StartCoroutine(AutoReleaseCoroutine(source, audioData.clip.length / audioData.pitch));
    }

    /// <summary>
    /// Play a preset audio event by name - configured in the Editor
    /// </summary>
    public void PlayPresetAudio(string eventName) {
      var config = System.Array.Find(PresetAudioEvents, e => e.eventName.Equals(eventName, System.StringComparison.OrdinalIgnoreCase));
      if (config != null && config.clip != null) {
        float volume = config.volume;
        float pitch = config.pitch;

        // Apply randomization if enabled for this event
        if (config.enableRandomization && !DisableRandomization) {
          volume *= Random.Range(VolumeRange.x, VolumeRange.y);
          pitch *= Random.Range(PitchRange.x, PitchRange.y);
        }

        AudioSource source = audioSourcePool.Get();
        source.clip = config.clip;
        source.volume = volume;
        source.pitch = pitch;

        if (config.mixerGroup != null) {
          source.outputAudioMixerGroup = config.mixerGroup;
        }

        source.Play();
        StartCoroutine(AutoReleaseCoroutine(source, config.clip.length / pitch));
      }
      else {
        Debug.LogWarning($"AudioSourcePool: Preset audio event '{eventName}' not found or has no clip assigned.");
      }
    }

    /// <summary>
    /// Unity Event compatible method - plays AudioClip with default settings
    /// </summary>
    public void PlayAudioClip(AudioClip clip) {
      PlayUISound(clip, DefaultVolume, DefaultPitch);
    }


    private IEnumerator AutoReleaseCoroutine(AudioSource source, float duration) {
      yield return new WaitForSeconds(duration + 0.1f); // Small buffer
      if (source != null) {
        audioSourcePool.Release(source);
      }
    }

    #endregion

    #region Editor Integration

#if UNITY_EDITOR
    [ContextMenu("Test Random Preset Audio")]
    private void TestRandomPresetAudio() {
      if (PresetAudioEvents != null && PresetAudioEvents.Length > 0) {
        var randomEvent = PresetAudioEvents[Random.Range(0, PresetAudioEvents.Length)];
        PlayPresetAudio(randomEvent.eventName);
        Debug.Log($"Playing preset audio: {randomEvent.eventName}");
      }
    }

    [ContextMenu("List All Preset Events")]
    private void ListPresetEvents() {
      if (PresetAudioEvents != null && PresetAudioEvents.Length > 0) {
        Debug.Log("Available preset audio events:");
        for (int i = 0; i < PresetAudioEvents.Length; i++) {
          var evt = PresetAudioEvents[i];
          Debug.Log($"  {i}: '{evt.eventName}' - Clip: {(evt.clip != null ? evt.clip.name : "None")}");
        }
      }
      else {
        Debug.Log("No preset audio events configured.");
      }
    }
#endif

    #endregion

  }
}