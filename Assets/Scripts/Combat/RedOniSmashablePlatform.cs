using System.Collections;
using UnityEngine;

public class RedOniSmashablePlatform : MonoBehaviour
{
    [SerializeField] private Collider2D platformCollider;
    [SerializeField] private SpriteRenderer[] visualRenderers;
    [SerializeField] private Color warningColor = new Color(1f, 0.32f, 0.16f, 1f);
    [SerializeField, Range(0.02f, 0.5f)] private float brokenAlpha = 0.08f;

    private Color[] originalColors;
    private Coroutine restoreRoutine;

    public bool IsWarning { get; private set; }
    public bool IsBroken { get; private set; }
    public bool IsAvailable => isActiveAndEnabled && !IsWarning && !IsBroken;
    public int CompletedSmashCount { get; private set; }

    private void Awake()
    {
        CacheReferences();
        RestoreImmediately();
    }

    private void OnEnable()
    {
        GameManager.RetryCompleted += RestoreImmediately;
    }

    private void OnDisable()
    {
        GameManager.RetryCompleted -= RestoreImmediately;
    }

    public void BeginWarning()
    {
        if (!IsAvailable)
        {
            return;
        }

        CacheReferences();
        IsWarning = true;
        SetWarningProgress(0f);
    }

    public void SetWarningProgress(float progress)
    {
        if (!IsWarning)
        {
            return;
        }

        float pulse = 0.35f + 0.65f * Mathf.PingPong(Mathf.Clamp01(progress) * 5f, 1f);

        for (int index = 0; index < visualRenderers.Length; index++)
        {
            SpriteRenderer renderer = visualRenderers[index];

            if (renderer != null)
            {
                renderer.color = Color.Lerp(originalColors[index], warningColor, pulse);
            }
        }
    }

    public void CancelWarning()
    {
        IsWarning = false;
        RestoreColors();
    }

    public void BreakFor(float restoreDelay)
    {
        CacheReferences();

        if (restoreRoutine != null)
        {
            StopCoroutine(restoreRoutine);
        }

        IsWarning = false;
        IsBroken = true;
        CompletedSmashCount++;

        if (platformCollider != null)
        {
            platformCollider.enabled = false;
        }

        for (int index = 0; index < visualRenderers.Length; index++)
        {
            SpriteRenderer renderer = visualRenderers[index];

            if (renderer != null)
            {
                Color color = originalColors[index];
                color.a *= brokenAlpha;
                renderer.color = color;
            }
        }

        restoreRoutine = StartCoroutine(RestoreAfterDelay(Mathf.Max(0.1f, restoreDelay)));
    }

    public void RestoreImmediately()
    {
        if (restoreRoutine != null)
        {
            StopCoroutine(restoreRoutine);
            restoreRoutine = null;
        }

        CacheReferences();
        IsWarning = false;
        IsBroken = false;

        if (platformCollider != null)
        {
            platformCollider.enabled = true;
        }

        RestoreColors();
    }

    private IEnumerator RestoreAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        restoreRoutine = null;
        RestoreImmediately();
    }

    private void CacheReferences()
    {
        if (platformCollider == null)
        {
            platformCollider = GetComponent<Collider2D>();
        }

        if (visualRenderers == null || visualRenderers.Length == 0)
        {
            visualRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (originalColors == null || originalColors.Length != visualRenderers.Length)
        {
            originalColors = new Color[visualRenderers.Length];

            for (int index = 0; index < visualRenderers.Length; index++)
            {
                originalColors[index] = visualRenderers[index] != null
                    ? visualRenderers[index].color
                    : Color.white;
            }
        }
    }

    private void RestoreColors()
    {
        if (visualRenderers == null || originalColors == null)
        {
            return;
        }

        for (int index = 0; index < visualRenderers.Length && index < originalColors.Length; index++)
        {
            if (visualRenderers[index] != null)
            {
                visualRenderers[index].color = originalColors[index];
            }
        }
    }
}
