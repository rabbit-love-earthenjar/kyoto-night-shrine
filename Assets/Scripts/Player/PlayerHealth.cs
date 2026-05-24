using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 3;
    [SerializeField] private float invincibilityTime = 0.8f;
    [SerializeField] private float hitFlashTime = 0.1f;
    [SerializeField] private float knockbackDistance = 0.45f;
    [SerializeField] private Text hpText;

    private SpriteRenderer spriteRenderer;
    private GameManager gameManager;
    private Color originalColor;
    private int currentHP;
    private bool isInvincible;
    private bool isDead;
    private Coroutine invincibilityRoutine;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public bool IsInvincible => isInvincible;

    private void Awake()
    {
        spriteRenderer = FindActiveSpriteRenderer();
        gameManager = FindAnyObjectByType<GameManager>();
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        currentHP = Mathf.Max(1, maxHP);
    }

    private void Start()
    {
        if (hpText == null)
        {
            CreateHpUi();
        }

        UpdateHpUi();
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(int damage, Vector2 damageSource)
    {
        if (isDead || isInvincible || damage <= 0)
        {
            return;
        }

        currentHP = Mathf.Max(0, currentHP - damage);
        UpdateHpUi();
        ApplyHitFeedback(damageSource);

        if (currentHP <= 0)
        {
            Die();
            return;
        }

        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
        }

        invincibilityRoutine = StartCoroutine(InvincibilityRoutine());
    }

    public void ResetHealth()
    {
        isDead = false;
        isInvincible = false;
        currentHP = Mathf.Max(1, maxHP);

        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
            invincibilityRoutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        UpdateHpUi();
    }

    private void ApplyHitFeedback(Vector2 damageSource)
    {
        ApplyKnockback(damageSource);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.55f, 0.55f, originalColor.a);
        }
    }

    private void ApplyKnockback(Vector2 damageSource)
    {
        Vector2 direction = ((Vector2)transform.position - damageSource).normalized;

        if (Mathf.Abs(direction.x) < 0.01f)
        {
            direction.x = 1f;
        }

        transform.position += new Vector3(Mathf.Sign(direction.x) * knockbackDistance, 0.08f, 0f);
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(hitFlashTime);

        if (!isDead && spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        float remainingTime = Mathf.Max(0f, invincibilityTime - hitFlashTime);

        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        isInvincible = false;
        invincibilityRoutine = null;
    }

    private void Die()
    {
        isDead = true;
        isInvincible = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        if (gameManager != null)
        {
            gameManager.PlayerFell();
        }
    }

    private void CreateHpUi()
    {
        GameObject canvasObject = new GameObject("PlayerHpCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        GameObject textObject = new GameObject("PlayerHpText");
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = new Vector2(24f, -24f);
        textRect.sizeDelta = new Vector2(180f, 40f);

        hpText = textObject.AddComponent<Text>();
        hpText.alignment = TextAnchor.MiddleLeft;
        hpText.fontSize = 24;
        hpText.color = Color.white;
        hpText.font = GetUiFont();
    }

    private void UpdateHpUi()
    {
        if (hpText != null)
        {
            hpText.text = $"HP: {currentHP}/{Mathf.Max(1, maxHP)}";
        }
    }

    private Font GetUiFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont("Arial", 18);
        }

        return font;
    }

    private SpriteRenderer FindActiveSpriteRenderer()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer.enabled)
            {
                return renderer;
            }
        }

        return GetComponent<SpriteRenderer>();
    }
}
