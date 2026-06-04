using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AttackEffectVisual : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color startColor = Color.white;
    private Vector3 baseScale = Vector3.one;
    private float duration = 0.2f;
    private float startScale = 0.85f;
    private float endScale = 1.12f;
    private float facingDirection = 1f;
    private float travelDistance = 0.12f;
    private float elapsed;
    private Vector3 startLocalPosition;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        startLocalPosition = transform.localPosition;

        if (spriteRenderer != null)
        {
            startColor = spriteRenderer.color;
        }
    }

    public void Play(float effectDuration, float effectStartScale, float effectEndScale)
    {
        duration = Mathf.Max(0.01f, effectDuration);
        startScale = Mathf.Max(0.01f, effectStartScale);
        endScale = Mathf.Max(0.01f, effectEndScale);
        elapsed = 0f;
        ApplyVisualState(0f);
    }

    public void Play(float effectDuration, float effectStartScale, float effectEndScale, float effectFacingDirection, float effectTravelDistance)
    {
        facingDirection = effectFacingDirection >= 0f ? 1f : -1f;
        travelDistance = Mathf.Max(0f, effectTravelDistance);
        Play(effectDuration, effectStartScale, effectEndScale);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        ApplyVisualState(progress);
    }

    private void ApplyVisualState(float progress)
    {
        float scale = Mathf.Lerp(startScale, endScale, progress);
        transform.localScale = baseScale * scale;
        transform.localPosition = startLocalPosition + new Vector3(facingDirection * travelDistance * progress, 0f, 0f);

        if (spriteRenderer == null)
        {
            return;
        }

        Color color = startColor;
        color.a = Mathf.Lerp(startColor.a, 0f, progress);
        spriteRenderer.color = color;
    }
}
