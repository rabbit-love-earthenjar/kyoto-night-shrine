using UnityEngine;

public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
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

    private void Awake()
    {
        Instance = this;
        EnsureSources();
        PlayBgm();
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
        Instance?.PlayOneShot(Instance.playerAttackClip);
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

    public static void PlayStageClear()
    {
        Instance?.PlayOneShot(Instance.stageClearClip, 0.8f);
    }

    public static void PlayHazardSpike()
    {
        Instance?.PlayOneShot(Instance.hazardSpikeClip, 0.65f);
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
}
