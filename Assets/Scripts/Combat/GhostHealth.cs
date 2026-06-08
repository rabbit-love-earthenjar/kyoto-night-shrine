using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class GhostHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 1;
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private float knockbackDistance = 0.25f;
    [SerializeField] private float deathDelay = 0.05f;
    [SerializeField] private float deathFadeDuration = 0.16f;
    [SerializeField] private float deathFloatDistance = 0.18f;
    [SerializeField] private float hitStunDuration = 0.1f;
    [SerializeField] private float hitStopDuration = 0.035f;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private int faithPointReward = 1;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private bool dropStarSealOnDeath;
    [SerializeField] private Sprite starSealSprite;
    [SerializeField] private int starSealAmount = 1;
    [SerializeField] private Vector2 starSealDropOffset = new Vector2(0f, 0.35f);
    [SerializeField] private float starSealDropScale = 0.06f;
    [SerializeField] private float starSealPickupColliderSize = 18f;
    [SerializeField] private Color starSealDropColor = Color.white;

    private SpriteRenderer spriteRenderer;
    private Collider2D ghostCollider;
    private GhostEnemy ghostMovement;
    private Color originalColor;
    private int currentHP;
    private bool isDead;
    private Coroutine flashRoutine;
    private static bool hitStopActive;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ghostCollider = GetComponent<Collider2D>();
        ghostMovement = GetComponent<GhostEnemy>();
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        currentHP = Mathf.Max(1, maxHP);

        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(int damage, Vector2 attackerPosition)
    {
        if (isDead)
        {
            return;
        }

        currentHP -= Mathf.Max(1, damage);
        ApplyHitFeedback(attackerPosition);
        ApplyHitStop();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        AwardFaithPoints();
        DropStarSeal();
        GameAudio.PlayGhostVanish();
        CombatFeedbackEffects.SpawnGhostVanish(transform.position);

        if (ghostCollider != null)
        {
            ghostCollider.enabled = false;
        }

        if (ghostMovement != null)
        {
            ghostMovement.PauseMovement();
        }

        StartCoroutine(VanishAndDestroyRoutine());
    }

    private void AwardFaithPoints()
    {
        if (gameManager != null && faithPointReward > 0)
        {
            gameManager.AddFaithPoints(faithPointReward);
        }
    }

    private void DropStarSeal()
    {
        if (!dropStarSealOnDeath || starSealSprite == null || starSealAmount <= 0)
        {
            return;
        }

        GameObject drop = new GameObject($"{gameObject.name}_StarSealDrop");
        drop.transform.position = transform.position + (Vector3)starSealDropOffset;
        drop.transform.localScale = Vector3.one * Mathf.Max(0.01f, starSealDropScale);

        SpriteRenderer dropRenderer = drop.AddComponent<SpriteRenderer>();
        dropRenderer.sprite = starSealSprite;
        dropRenderer.color = starSealDropColor;
        dropRenderer.sortingOrder = 8;

        BoxCollider2D dropCollider = drop.AddComponent<BoxCollider2D>();
        dropCollider.isTrigger = true;
        float colliderSize = Mathf.Max(0.1f, starSealPickupColliderSize);
        dropCollider.size = new Vector2(colliderSize, colliderSize);

        PickupItem pickupItem = drop.AddComponent<PickupItem>();
        pickupItem.ConfigureStarSeal(gameManager, starSealAmount);
        CombatFeedbackEffects.SpawnPickupDrop(drop.transform.position);
    }

    private void ApplyHitFeedback(Vector2 attackerPosition)
    {
        Vector2 knockbackDirection = ((Vector2)transform.position - attackerPosition).normalized;

        if (knockbackDirection.sqrMagnitude < 0.01f)
        {
            knockbackDirection = Vector2.right;
        }

        if (ghostMovement != null)
        {
            ghostMovement.ApplyKnockback(knockbackDirection, knockbackDistance);
            ghostMovement.ApplyHitStun(hitStunDuration);
        }

        CombatFeedbackEffects.SpawnGhostHit(transform.position, knockbackDirection);

        if (spriteRenderer == null)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSecondsRealtime(flashDuration);

        if (!isDead && spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void ApplyHitStop()
    {
        if (hitStopDuration <= 0f || hitStopActive || Time.timeScale <= 0f)
        {
            return;
        }

        StartCoroutine(HitStopRoutine());
    }

    private IEnumerator HitStopRoutine()
    {
        hitStopActive = true;
        float previousTimeScale = Time.timeScale;
        float stoppedTimeScale = Mathf.Min(previousTimeScale, 0.08f);
        Time.timeScale = stoppedTimeScale;

        yield return new WaitForSecondsRealtime(hitStopDuration);

        if (Mathf.Approximately(Time.timeScale, stoppedTimeScale))
        {
            Time.timeScale = previousTimeScale;
        }

        hitStopActive = false;
    }

    private IEnumerator VanishAndDestroyRoutine()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        Color[] startColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            startColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
        }

        float duration = Mathf.Max(0.01f, Mathf.Max(deathDelay, deathFadeDuration));
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 0.72f;
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + Vector3.up * Mathf.Max(0f, deathFloatDistance);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            transform.localScale = Vector3.Lerp(startScale, endScale, eased);
            transform.position = Vector3.Lerp(startPosition, endPosition, eased);

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];

                if (renderer == null)
                {
                    continue;
                }

                Color color = startColors[i];
                color.a = Mathf.Lerp(startColors[i].a, 0f, eased);
                renderer.color = color;
            }

            yield return null;
        }

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        Destroy(gameObject);
    }
}
