using System.Collections;
using UnityEngine;

public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioManagerRetryController retryAudioController;
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip playerJumpClip;
    [SerializeField] private AudioClip playerLandClip;
    [SerializeField] private AudioClip playerAttackClip;
    [SerializeField] private AudioClip playerHurtClip;
    [SerializeField] private AudioClip collectFaithPointClip;
    [SerializeField] private AudioClip collectStarSealClip;
    [SerializeField] private AudioClip collectHeartClip;
    [SerializeField] private AudioClip ghostVanishClip;
    [SerializeField] private AudioClip retryFallClip;
    [SerializeField] private AudioClip stageClearClip;
    [SerializeField] private AudioClip hazardSpikeClip;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.2f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.65f;
    [SerializeField, Min(0.05f)] private float playerAttackClipMaxDuration = 0.38f;
    [SerializeField, Range(0.01f, 0.1f)] private float clippedSfxFadeOutDuration = 0.025f;

    private bool bgmPausedByOverlay;

    private void Awake()
    {
        Instance = this;
        EnsureSources();
        PlayBgm();
        EnsureRetryAudioController();
        retryAudioController?.ResetToNormalImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void PlayPlayerJump()
    {
        Instance?.PlayOneShot(Instance.playerJumpClip);
    }

    public static void PlayPlayerLand()
    {
        Instance?.PlayOneShot(Instance.playerLandClip, 0.55f);
    }

    public static void PlayPlayerAttack()
    {
        Instance?.PlayLimitedOneShot(Instance.playerAttackClip, 1f, Instance.playerAttackClipMaxDuration);
    }

    public static void PlayPlayerAttack(int comboStep)
    {
        int safeStep = Mathf.Clamp(comboStep, 1, 3);
        float volumeScale = 1f;

        if (safeStep == 1)
        {
            volumeScale = 0.9f;
        }
        else if (safeStep == 3)
        {
            volumeScale = 1.12f;
        }

        Instance?.PlayLimitedOneShot(Instance.playerAttackClip, volumeScale, Instance.playerAttackClipMaxDuration);
    }

    public static void PlayPlayerHurt()
    {
        Instance?.PlayOneShot(Instance.playerHurtClip, 0.75f);
    }

    public static void PlayCollectFaithPoint()
    {
        Instance?.PlayOneShot(Instance.collectFaithPointClip, 0.55f);
    }

    public static void PlayCollectStarSeal()
    {
        Instance?.PlayOneShot(Instance.collectStarSealClip, 0.7f);
    }

    public static void PlayCollectHeart()
    {
        Instance?.PlayOneShot(Instance.collectHeartClip, 0.7f);
    }

    public static void PlayGhostVanish()
    {
        Instance?.PlayOneShot(Instance.ghostVanishClip, 0.65f);
    }

    public static void PlayRetryFall()
    {
        Instance?.PlayOneShot(Instance.retryFallClip, 0.7f);
    }

    public static void EnterRetryAudioState()
    {
        Instance?.retryAudioController?.OnPlayerDeath();
    }

    public static void ExitRetryAudioState()
    {
        Instance?.retryAudioController?.OnPlayerRespawn();
    }

    public static void PlayStageClear()
    {
        Instance?.PlayOneShot(Instance.stageClearClip, 0.8f);
    }

    public static void PlayHazardSpike()
    {
        Instance?.PlayOneShot(Instance.hazardSpikeClip, 0.65f);
    }

    public static void PauseBgmForOverlay()
    {
        Instance?.PauseBgmForOverlayInternal();
    }

    public static void ResumeBgmFromOverlay()
    {
        Instance?.ResumeBgmFromOverlayInternal();
    }

    private void EnsureSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureSource(bgmSource);
        ConfigureSource(sfxSource);
    }

    private void ConfigureSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    private void EnsureRetryAudioController()
    {
        if (retryAudioController == null)
        {
            retryAudioController = AudioManagerRetryController.Instance;
        }

        if (retryAudioController == null)
        {
            retryAudioController = FindAnyObjectByType<AudioManagerRetryController>();
        }

        if (retryAudioController == null)
        {
            GameObject controllerObject = new GameObject("AudioManagerRetryController");
            retryAudioController = controllerObject.AddComponent<AudioManagerRetryController>();
        }

        retryAudioController.UseBgmSource(bgmSource);
    }

    private void PlayBgm()
    {
        if (bgmSource == null || bgmClip == null)
        {
            return;
        }

        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;

        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    private void PauseBgmForOverlayInternal()
    {
        EnsureSources();

        if (bgmSource == null || !bgmSource.isPlaying)
        {
            return;
        }

        bgmPausedByOverlay = true;
        bgmSource.Pause();
    }

    private void ResumeBgmFromOverlayInternal()
    {
        EnsureSources();

        if (bgmSource == null || !bgmPausedByOverlay)
        {
            return;
        }

        bgmPausedByOverlay = false;
        bgmSource.UnPause();
    }

    private void PlayOneShot(AudioClip clip)
    {
        PlayOneShot(clip, 1f);
    }

    private void PlayOneShot(AudioClip clip, float volumeScale)
    {
        if (sfxSource == null || clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, sfxVolume * Mathf.Clamp01(volumeScale));
    }

    private void PlayLimitedOneShot(AudioClip clip, float volumeScale, float maxDuration)
    {
        if (clip == null)
        {
            return;
        }

        if (maxDuration <= 0f || clip.length <= maxDuration)
        {
            PlayOneShot(clip, volumeScale);
            return;
        }

        StartCoroutine(PlayLimitedOneShotRoutine(clip, sfxVolume * Mathf.Clamp01(volumeScale), maxDuration));
    }

    private IEnumerator PlayLimitedOneShotRoutine(AudioClip clip, float volume, float maxDuration)
    {
        GameObject audioObject = new GameObject($"LimitedSfx_{clip.name}");
        audioObject.transform.SetParent(transform, false);

        AudioSource source = audioObject.AddComponent<AudioSource>();
        ConfigureSource(source);
        source.clip = clip;
        source.volume = volume;
        source.Play();

        float safeDuration = Mathf.Max(0.01f, maxDuration);
        float fadeDuration = Mathf.Min(clippedSfxFadeOutDuration, safeDuration);
        float holdDuration = Mathf.Max(0f, safeDuration - fadeDuration);

        if (holdDuration > 0f)
        {
            yield return new WaitForSeconds(holdDuration);
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration && source != null)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(volume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        Destroy(audioObject);
    }
}
