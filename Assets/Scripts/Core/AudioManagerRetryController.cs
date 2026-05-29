using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManagerRetryController : MonoBehaviour
{
    public static AudioManagerRetryController Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private bool persistAcrossScenes = true;

    [Header("Optional Mixer Snapshots")]
    [SerializeField] private bool useMixerSnapshots;
    [SerializeField] private AudioMixerSnapshot normalSnapshot;
    [SerializeField] private AudioMixerSnapshot playerDeadSnapshot;

    [Header("Manual Low-Pass Fallback")]
    [SerializeField] private AudioLowPassFilter lowPassFilter;
    [SerializeField] private float normalCutoffFrequency = 22000f;
    [SerializeField] private float deadCutoffFrequency = 400f;
    [SerializeField, Range(0f, 1f)] private float deadVolumeScale = 0.4f;
    [SerializeField] private float deathTransitionDuration = 0.3f;
    [SerializeField] private float respawnTransitionDuration = 0.2f;

    private Coroutine transitionCoroutine;
    private float originalBgmVolume = 1f;
    private bool playerIsDead;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        ResolveAudioComponents();
        CacheOriginalVolume();
        SetNormalStateImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void OnPlayerDeath()
    {
        if (playerIsDead)
        {
            return;
        }

        playerIsDead = true;
        TransitionToDeathState();
    }

    public void OnPlayerRespawn()
    {
        if (!playerIsDead)
        {
            return;
        }

        playerIsDead = false;
        TransitionToNormalState();
    }

    public void ResetToNormalImmediate()
    {
        playerIsDead = false;
        StopActiveTransition();
        SetNormalStateImmediate();
    }

    public void UseBgmSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        bgmAudioSource = source;
        lowPassFilter = bgmAudioSource.GetComponent<AudioLowPassFilter>();
        ResolveAudioComponents();
        CacheOriginalVolume();

        if (playerIsDead)
        {
            ApplyManualState(deadCutoffFrequency, originalBgmVolume * deadVolumeScale);
            return;
        }

        SetNormalStateImmediate();
    }

    private void ResolveAudioComponents()
    {
        if (bgmAudioSource == null)
        {
            bgmAudioSource = GetComponent<AudioSource>();
        }

        if (bgmAudioSource == null)
        {
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (bgmMixerGroup != null)
        {
            bgmAudioSource.outputAudioMixerGroup = bgmMixerGroup;
        }

        if (lowPassFilter == null)
        {
            lowPassFilter = bgmAudioSource.GetComponent<AudioLowPassFilter>();
        }

        if (lowPassFilter == null)
        {
            lowPassFilter = bgmAudioSource.gameObject.AddComponent<AudioLowPassFilter>();
        }

        lowPassFilter.cutoffFrequency = normalCutoffFrequency;
    }

    private void CacheOriginalVolume()
    {
        if (bgmAudioSource != null)
        {
            originalBgmVolume = bgmAudioSource.volume;
        }
    }

    private void TransitionToDeathState()
    {
        TransitionMixerSnapshot(playerDeadSnapshot, deathTransitionDuration);
        StartManualTransition(deadCutoffFrequency, originalBgmVolume * deadVolumeScale, deathTransitionDuration);
    }

    private void TransitionToNormalState()
    {
        TransitionMixerSnapshot(normalSnapshot, respawnTransitionDuration);
        StartManualTransition(normalCutoffFrequency, originalBgmVolume, respawnTransitionDuration);
    }

    private void TransitionMixerSnapshot(AudioMixerSnapshot targetSnapshot, float duration)
    {
        if (!useMixerSnapshots || targetSnapshot == null)
        {
            return;
        }

        if (audioMixer != null)
        {
            audioMixer.TransitionToSnapshots(new[] { targetSnapshot }, new[] { 1f }, Mathf.Max(0f, duration));
            return;
        }

        targetSnapshot.TransitionTo(Mathf.Max(0f, duration));
    }

    private void StartManualTransition(float targetCutoffFrequency, float targetVolume, float duration)
    {
        StopActiveTransition();

        if (duration <= 0f)
        {
            ApplyManualState(targetCutoffFrequency, targetVolume);
            return;
        }

        transitionCoroutine = StartCoroutine(ManualTransitionRoutine(targetCutoffFrequency, targetVolume, duration));
    }

    private IEnumerator ManualTransitionRoutine(float targetCutoffFrequency, float targetVolume, float duration)
    {
        float startCutoffFrequency = lowPassFilter != null ? lowPassFilter.cutoffFrequency : normalCutoffFrequency;
        float startVolume = bgmAudioSource != null ? bgmAudioSource.volume : originalBgmVolume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float cutoffFrequency = Mathf.Lerp(startCutoffFrequency, targetCutoffFrequency, t);
            float volume = Mathf.Lerp(startVolume, targetVolume, t);
            ApplyManualState(cutoffFrequency, volume);
            yield return null;
        }

        ApplyManualState(targetCutoffFrequency, targetVolume);
        transitionCoroutine = null;
    }

    private void ApplyManualState(float cutoffFrequency, float volume)
    {
        if (lowPassFilter != null)
        {
            lowPassFilter.cutoffFrequency = Mathf.Max(10f, cutoffFrequency);
        }

        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = Mathf.Max(0f, volume);
        }
    }

    private void SetNormalStateImmediate()
    {
        ApplyManualState(normalCutoffFrequency, originalBgmVolume);
    }

    private void StopActiveTransition()
    {
        if (transitionCoroutine == null)
        {
            return;
        }

        StopCoroutine(transitionCoroutine);
        transitionCoroutine = null;
    }
}
