using System.Collections;
using UnityEngine;

public class RedOniPhaseMusicController : MonoBehaviour
{
    [SerializeField] private RedOniBossHealth bossHealth;
    [SerializeField] private RedOniPhaseOneController combatController;
    [SerializeField] private AudioClip phaseOneBgm;
    [SerializeField] private AudioClip phaseTwoBgm;
    [SerializeField] private AudioClip phaseThreeBgm;
    [SerializeField, Min(0f)] private float transitionDuration = 0.55f;
    [SerializeField, Min(0f)] private float defeatFadeDuration = 0.9f;

    private Coroutine transitionRoutine;
    private int playingPhase;
    private bool musicStoppedForDefeat;

    public bool IsConfigured => bossHealth != null
        && phaseOneBgm != null
        && phaseTwoBgm != null
        && phaseThreeBgm != null;

    public void Configure(
        RedOniBossHealth health,
        AudioClip firstPhase,
        AudioClip secondPhase,
        AudioClip thirdPhase)
    {
        bossHealth = health;
        combatController = health != null ? health.GetComponent<RedOniPhaseOneController>() : null;
        phaseOneBgm = firstPhase;
        phaseTwoBgm = secondPhase;
        phaseThreeBgm = thirdPhase;
    }

    private IEnumerator Start()
    {
        while (GameAudio.Instance == null)
        {
            yield return null;
        }

        if (bossHealth == null)
        {
            bossHealth = GetComponent<RedOniBossHealth>();
        }

        if (combatController == null)
        {
            combatController = GetComponent<RedOniPhaseOneController>();
        }

        SwitchToPhase(GetActiveCombatPhase(), true);
    }

    private void Update()
    {
        if (bossHealth == null || GameAudio.Instance == null)
        {
            return;
        }

        if (bossHealth.BossDefeated)
        {
            if (!musicStoppedForDefeat)
            {
                musicStoppedForDefeat = true;
                StopMusicForDefeat();
            }

            return;
        }

        if (musicStoppedForDefeat)
        {
            musicStoppedForDefeat = false;
            playingPhase = 0;
        }

        int activePhase = GetActiveCombatPhase();

        if (playingPhase != activePhase)
        {
            SwitchToPhase(activePhase, false);
        }
    }

    private void OnDisable()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }
    }

    private void SwitchToPhase(int phase, bool immediate)
    {
        int safePhase = Mathf.Clamp(phase, 1, 3);
        AudioClip targetClip = GetPhaseClip(safePhase);
        AudioSource source = GameAudio.Instance != null ? GameAudio.Instance.BgmSource : null;

        if (source == null || targetClip == null)
        {
            return;
        }

        playingPhase = safePhase;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        if (source.clip == targetClip && source.isPlaying)
        {
            source.volume = GameAudio.Instance.BgmVolume;
            transitionRoutine = null;
            return;
        }

        transitionRoutine = StartCoroutine(ChangeMusicRoutine(source, targetClip, immediate));
    }

    private IEnumerator ChangeMusicRoutine(AudioSource source, AudioClip targetClip, bool immediate)
    {
        float targetVolume = GameAudio.Instance != null ? GameAudio.Instance.BgmVolume : 0.2f;

        if (!immediate && source.isPlaying && transitionDuration > 0f)
        {
            float startVolume = source.volume;
            float halfDuration = transitionDuration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / halfDuration);
                yield return null;
            }
        }

        source.Stop();
        source.clip = targetClip;
        source.loop = true;
        source.volume = immediate ? targetVolume : 0f;
        source.Play();

        if (!immediate && transitionDuration > 0f)
        {
            float halfDuration = transitionDuration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(0f, targetVolume, elapsed / halfDuration);
                yield return null;
            }
        }

        source.volume = targetVolume;
        transitionRoutine = null;
    }

    private AudioClip GetPhaseClip(int phase)
    {
        if (phase >= 3)
        {
            return phaseThreeBgm;
        }

        return phase == 2 ? phaseTwoBgm : phaseOneBgm;
    }

    private int GetActiveCombatPhase()
    {
        if (combatController != null)
        {
            return combatController.CurrentCombatPhase;
        }

        return bossHealth != null ? bossHealth.CurrentPhase : 1;
    }

    private void StopMusicForDefeat()
    {
        AudioSource source = GameAudio.Instance != null ? GameAudio.Instance.BgmSource : null;

        if (source == null)
        {
            return;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(FadeOutAndStopRoutine(source));
    }

    private IEnumerator FadeOutAndStopRoutine(AudioSource source)
    {
        float startVolume = source.volume;
        float duration = Mathf.Max(0f, defeatFadeDuration);
        float elapsed = 0f;

        while (elapsed < duration && source != null)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        if (source != null)
        {
            source.Stop();
            source.volume = GameAudio.Instance != null ? GameAudio.Instance.BgmVolume : startVolume;
        }

        playingPhase = 0;
        transitionRoutine = null;
    }
}
