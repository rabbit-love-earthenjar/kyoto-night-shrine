using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TemporaryCloudPlatform : MonoBehaviour
{
    [SerializeField] private float warningDelay = 0.9f;
    [SerializeField] private float recoveryDelay = 2.8f;
    [SerializeField] private float warningFlashInterval = 0.1f;
    [SerializeField] private Color warningColor = new Color(1f, 0.65f, 0.72f, 0.7f);

    private Collider2D platformCollider;
    private SpriteRenderer spriteRenderer;
    private Color normalColor = Color.white;
    private Coroutine cycleRoutine;

    private void Awake()
    {
        platformCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        GameManager.RetryCompleted += ResetPlatform;
    }

    private void OnDisable()
    {
        GameManager.RetryCompleted -= ResetPlatform;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (cycleRoutine == null && collision.collider.GetComponentInParent<PlayerController>() != null)
        {
            cycleRoutine = StartCoroutine(DisappearAndRecover());
        }
    }

    public void Configure(float disappearSeconds, float recoverSeconds)
    {
        warningDelay = Mathf.Max(0.1f, disappearSeconds);
        recoveryDelay = Mathf.Max(0.1f, recoverSeconds);
    }

    private IEnumerator DisappearAndRecover()
    {
        float elapsed = 0f;
        bool flash = false;

        while (elapsed < warningDelay)
        {
            flash = !flash;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = flash ? warningColor : normalColor;
            }

            float wait = Mathf.Min(Mathf.Max(0.03f, warningFlashInterval), warningDelay - elapsed);
            yield return new WaitForSeconds(wait);
            elapsed += wait;
        }

        SetAvailable(false);
        yield return new WaitForSeconds(recoveryDelay);
        SetAvailable(true);
        cycleRoutine = null;
    }

    private void SetAvailable(bool available)
    {
        if (platformCollider != null)
        {
            platformCollider.enabled = available;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = available;
            spriteRenderer.color = normalColor;
        }
    }

    private void ResetPlatform()
    {
        if (cycleRoutine != null)
        {
            StopCoroutine(cycleRoutine);
            cycleRoutine = null;
        }

        SetAvailable(true);
    }
}
