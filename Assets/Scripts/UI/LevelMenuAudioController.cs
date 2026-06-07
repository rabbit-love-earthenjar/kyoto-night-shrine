using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class LevelMenuAudioController : MonoBehaviour
{
    [SerializeField] private AudioClip hoverFurinWindchime;
    [SerializeField] private AudioClip igniteLanternFlame;
    [SerializeField] private float hoverVol = 0.4f;
    [SerializeField] private float igniteVol = 1f;
    [SerializeField, Min(0.1f)] private float hoverMaxDuration = 4f;

    private AudioSource audioSource;
    private Coroutine hoverRoutine;

    private void Awake()
    {
        EnsureAudioSource();
    }

    public void Configure(AudioClip hoverClip, AudioClip igniteClip, float hoverVolume, float igniteVolume)
    {
        hoverFurinWindchime = hoverClip;
        igniteLanternFlame = igniteClip;
        hoverVol = hoverVolume;
        igniteVol = igniteVolume;
        EnsureAudioSource();
    }

    public void PlayHoverSFX()
    {
        EnsureAudioSource();

        if (audioSource == null || hoverFurinWindchime == null)
        {
            return;
        }

        StopHoverPlayback();

        audioSource.pitch = 1f;
        audioSource.clip = hoverFurinWindchime;
        audioSource.volume = Mathf.Max(0f, hoverVol);
        audioSource.Play();

        float safeDuration = Mathf.Min(Mathf.Max(0.1f, hoverMaxDuration), hoverFurinWindchime.length);
        hoverRoutine = StartCoroutine(StopHoverAfterDelay(safeDuration));
    }

    public void PlayIgniteSFX()
    {
        EnsureAudioSource();

        if (audioSource == null || igniteLanternFlame == null)
        {
            return;
        }

        StopHoverPlayback();

        audioSource.pitch = 1f;
        audioSource.volume = 1f;
        audioSource.PlayOneShot(igniteLanternFlame, Mathf.Max(0f, igniteVol));
    }

    private void EnsureAudioSource()
    {
        if (audioSource != null)
        {
            return;
        }

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private IEnumerator StopHoverAfterDelay(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);

        if (audioSource != null && audioSource.clip == hoverFurinWindchime)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        hoverRoutine = null;
    }

    private void StopHoverPlayback()
    {
        if (hoverRoutine != null)
        {
            StopCoroutine(hoverRoutine);
            hoverRoutine = null;
        }

        if (audioSource != null && audioSource.clip == hoverFurinWindchime)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
    }
}
