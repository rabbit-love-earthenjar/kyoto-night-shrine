using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CombatFeedbackParticle : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Vector3 velocity;
    private Color startColor = Color.white;
    private float duration = 0.25f;
    private float startScale = 0.08f;
    private float endScale = 0.02f;
    private float elapsed;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            startColor = spriteRenderer.color;
        }
    }

    public void Initialize(Vector3 startVelocity, Color color, float lifetime, float initialScale, float finalScale)
    {
        velocity = startVelocity;
        startColor = color;
        duration = Mathf.Max(0.01f, lifetime);
        startScale = Mathf.Max(0.001f, initialScale);
        endScale = Mathf.Max(0.001f, finalScale);
        elapsed = 0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = startColor;
        }

        ApplyVisualState(0f);
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        transform.position += velocity * Time.unscaledDeltaTime;
        ApplyVisualState(progress);

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyVisualState(float progress)
    {
        float scale = Mathf.Lerp(startScale, endScale, progress);
        transform.localScale = new Vector3(scale, scale, 1f);

        if (spriteRenderer == null)
        {
            return;
        }

        Color color = startColor;
        color.a = Mathf.Lerp(startColor.a, 0f, progress);
        spriteRenderer.color = color;
    }
}
