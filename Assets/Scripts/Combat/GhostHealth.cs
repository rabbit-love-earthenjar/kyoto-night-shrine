using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class GhostHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 2;
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private float knockbackDistance = 0.5f;
    [SerializeField] private float deathDelay = 0.05f;
    [SerializeField] private float deathFadeDuration = 0.16f;
    [SerializeField] private float deathFloatDistance = 0.18f;
    [SerializeField] private bool hideBodyImmediatelyOnDeath = true;
    [SerializeField] private float hitStunDuration = 0.14f;
    [SerializeField] private float hitStopDuration = 0.035f;
    [SerializeField] private Color flashColor = Color.white;
    [Header("Health bar")]
    [SerializeField] private bool showHealthBarOnHit = true;
    [SerializeField] private Vector2 healthBarWorldSize = new Vector2(0.95f, 0.08f);
    [SerializeField] private float healthBarVerticalOffset = 0.14f;
    [SerializeField] private float healthBarVisibleDuration = 1.35f;
    [SerializeField] private Color healthBarFillColor = new Color(0.95f, 0.3f, 0.24f, 1f);
    [SerializeField] private Color healthBarBackColor = new Color(0.12f, 0.06f, 0.15f, 0.92f);
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
    private RangedRunnerEnemy rangedMovement;
    private Color originalColor;
    private int currentHP;
    private bool isDead;
    private Coroutine flashRoutine;
    private EnemyWorldHealthBar healthBar;
    private static bool hitStopActive;
    private bool ownsHitStop;
    private float hitStopPreviousTimeScale = 1f;
    private float ownedHitStopTimeScale = 1f;

    public int CurrentHP => currentHP;
    public int MaxHP => Mathf.Max(1, maxHP);
    public bool IsDead => isDead;
    public bool HealthBarVisible => healthBar != null && healthBar.IsVisible;
    public float HealthBarFillFraction => healthBar != null ? healthBar.FillFraction : 0f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ghostCollider = GetComponent<Collider2D>();
        ghostMovement = GetComponent<GhostEnemy>();
        rangedMovement = GetComponent<RangedRunnerEnemy>();
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        currentHP = Mathf.Max(1, maxHP);

        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }
    }

    private void OnDisable()
    {
        ReleaseOwnedHitStop();
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
        currentHP = Mathf.Max(0, currentHP);
        ShowHealthBar();
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
        if (healthBar != null)
        {
            healthBar.RemoveImmediately();
            healthBar = null;
        }
        AwardFaithPoints();
        DropStarSeal();
        GameAudio.PlayGhostVanish();
        CombatFeedbackEffects.SpawnGhostVanish(transform.position);

        foreach (Collider2D enemyCollider in GetComponentsInChildren<Collider2D>(true))
        {
            enemyCollider.enabled = false;
        }

        if (ghostMovement != null)
        {
            ghostMovement.PauseMovement();
        }

        if (rangedMovement != null)
        {
            rangedMovement.PauseMovement();
        }

        SpriteFrameAnimator frameAnimator = GetComponent<SpriteFrameAnimator>();
        if (frameAnimator != null)
        {
            frameAnimator.Stop();
        }

        if (hideBodyImmediatelyOnDeath)
        {
            foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.enabled = false;
            }

            StartCoroutine(DestroyHiddenBodyAfterHitStopRoutine());
            return;
        }

        StartCoroutine(VanishAndDestroyRoutine());
    }

    private void ShowHealthBar()
    {
        if (!showHealthBarOnHit || spriteRenderer == null || currentHP <= 0)
        {
            return;
        }

        if (healthBar == null)
        {
            healthBar = EnemyWorldHealthBar.Create(
                this,
                spriteRenderer,
                healthBarWorldSize,
                healthBarVerticalOffset,
                healthBarFillColor,
                healthBarBackColor);
        }

        healthBar.Show(currentHP, MaxHP, healthBarVisibleDuration);
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

        if (rangedMovement != null)
        {
            rangedMovement.ApplyKnockback(knockbackDirection, knockbackDistance);
            rangedMovement.ApplyHitStun(hitStunDuration);
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
        ownsHitStop = true;
        hitStopPreviousTimeScale = Time.timeScale;
        ownedHitStopTimeScale = Mathf.Min(hitStopPreviousTimeScale, 0.08f);
        Time.timeScale = ownedHitStopTimeScale;

        yield return new WaitForSecondsRealtime(hitStopDuration);

        ReleaseOwnedHitStop();
    }

    private void ReleaseOwnedHitStop()
    {
        if (!ownsHitStop)
        {
            return;
        }

        if (Mathf.Approximately(Time.timeScale, ownedHitStopTimeScale))
        {
            Time.timeScale = hitStopPreviousTimeScale;
        }

        ownsHitStop = false;
        hitStopActive = false;
    }

    private IEnumerator DestroyHiddenBodyAfterHitStopRoutine()
    {
        float cleanupDelay = Mathf.Max(deathDelay, hitStopDuration + 0.01f);
        yield return new WaitForSecondsRealtime(cleanupDelay);
        ReleaseOwnedHitStop();
        Destroy(gameObject);
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

internal sealed class EnemyWorldHealthBar : MonoBehaviour
{
    private static Sprite whiteSprite;
    private GhostHealth owner;
    private SpriteRenderer targetRenderer;
    private SpriteRenderer backRenderer;
    private SpriteRenderer fillRenderer;
    private Vector2 size;
    private float verticalOffset;
    private float visibleUntil;

    public bool IsVisible => fillRenderer != null && fillRenderer.enabled;
    public float FillFraction { get; private set; }

    public static EnemyWorldHealthBar Create(
        GhostHealth owner,
        SpriteRenderer targetRenderer,
        Vector2 requestedSize,
        float verticalOffset,
        Color fillColor,
        Color backColor)
    {
        GameObject root = new GameObject($"{owner.name}_HealthBar");
        EnemyWorldHealthBar bar = root.AddComponent<EnemyWorldHealthBar>();
        bar.owner = owner;
        bar.targetRenderer = targetRenderer;
        bar.size = new Vector2(Mathf.Max(0.25f, requestedSize.x), Mathf.Max(0.035f, requestedSize.y));
        bar.verticalOffset = Mathf.Max(0.02f, verticalOffset);

        bar.backRenderer = CreatePart(root.transform, "Back", backColor, targetRenderer.sortingOrder + 19);
        bar.fillRenderer = CreatePart(root.transform, "Fill", fillColor, targetRenderer.sortingOrder + 20);
        bar.backRenderer.transform.localScale = new Vector3(bar.size.x + 0.08f, bar.size.y + 0.055f, 1f);
        bar.SetVisible(false);
        return bar;
    }

    public void Show(int currentHP, int maxHP, float duration)
    {
        FillFraction = Mathf.Clamp01(currentHP / (float)Mathf.Max(1, maxHP));
        float fillWidth = size.x * FillFraction;
        fillRenderer.transform.localScale = new Vector3(fillWidth, size.y, 1f);
        fillRenderer.transform.localPosition = new Vector3(-size.x * 0.5f + fillWidth * 0.5f, 0f, -0.01f);
        visibleUntil = Time.unscaledTime + Mathf.Max(0.25f, duration);
        SetAlpha(1f);
        SetVisible(true);
        FollowTarget();
    }

    public void RemoveImmediately()
    {
        SetVisible(false);
        Destroy(gameObject);
    }

    private void LateUpdate()
    {
        if (owner == null || targetRenderer == null)
        {
            Destroy(gameObject);
            return;
        }

        FollowTarget();
        if (!IsVisible)
        {
            return;
        }

        float remaining = visibleUntil - Time.unscaledTime;
        if (remaining <= 0f)
        {
            SetVisible(false);
            return;
        }

        SetAlpha(Mathf.Clamp01(remaining / 0.22f));
    }

    private void FollowTarget()
    {
        Bounds bounds = targetRenderer.bounds;
        transform.position = new Vector3(bounds.center.x, bounds.max.y + verticalOffset, owner.transform.position.z - 0.1f);
    }

    private void SetVisible(bool visible)
    {
        if (backRenderer != null)
        {
            backRenderer.enabled = visible;
        }

        if (fillRenderer != null)
        {
            fillRenderer.enabled = visible;
        }
    }

    private void SetAlpha(float alpha)
    {
        SetRendererAlpha(backRenderer, alpha);
        SetRendererAlpha(fillRenderer, alpha);
    }

    private static void SetRendererAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null)
        {
            return;
        }

        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    private static SpriteRenderer CreatePart(Transform parent, string objectName, Color color, int sortingOrder)
    {
        GameObject part = new GameObject(objectName);
        part.transform.SetParent(parent, false);
        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = GetWhiteSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
        {
            return whiteSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "EnemyHealthBarPixel",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        whiteSprite.name = "EnemyHealthBarSprite";
        whiteSprite.hideFlags = HideFlags.HideAndDontSave;
        return whiteSprite;
    }
}
