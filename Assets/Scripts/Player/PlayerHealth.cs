using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 3;
    [SerializeField] private float invincibilityTime = 0.8f;
    [SerializeField] private float hitFlashTime = 0.1f;
    [SerializeField] private float knockbackDistance = 0.45f;
    [SerializeField] private Color fullHeartColor = new Color(1f, 0.18f, 0.28f, 1f);
    [SerializeField] private Color emptyHeartColor = new Color(0.22f, 0.22f, 0.28f, 0.9f);
    [SerializeField] private int heartFontSize = 34;

    private SpriteRenderer spriteRenderer;
    private GameManager gameManager;
    private Color originalColor;
    private Text[] heartTexts;
    private int currentHP;
    private bool isInvincible;
    private bool isDead;
    private Coroutine invincibilityRoutine;

    private const string FullHeart = "\u2665";
    private const string EmptyHeart = "\u2661";

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
        CreateHpUi();
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
        GameAudio.PlayPlayerHurt();
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

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
        {
            return;
        }

        currentHP = Mathf.Min(Mathf.Max(1, maxHP), currentHP + amount);
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
        if (heartTexts != null && heartTexts.Length == Mathf.Max(1, maxHP))
        {
            return;
        }

        GameObject canvasObject = new GameObject("PlayerHeartCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        GameObject heartsObject = new GameObject("HeartContainer");
        heartsObject.transform.SetParent(canvasObject.transform, false);

        RectTransform heartsRect = heartsObject.AddComponent<RectTransform>();
        heartsRect.anchorMin = new Vector2(0f, 1f);
        heartsRect.anchorMax = new Vector2(0f, 1f);
        heartsRect.pivot = new Vector2(0f, 1f);
        heartsRect.anchoredPosition = new Vector2(24f, -20f);
        heartsRect.sizeDelta = new Vector2(180f, 44f);

        HorizontalLayoutGroup layout = heartsObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 8f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        int heartCount = Mathf.Max(1, maxHP);
        heartTexts = new Text[heartCount];

        for (int i = 0; i < heartCount; i++)
        {
            heartTexts[i] = CreateHeartText(heartsObject.transform, i);
        }
    }

    private void UpdateHpUi()
    {
        if (heartTexts == null || heartTexts.Length == 0)
        {
            return;
        }

        for (int i = 0; i < heartTexts.Length; i++)
        {
            if (heartTexts[i] == null)
            {
                continue;
            }

            bool heartIsFull = i < currentHP;
            heartTexts[i].text = heartIsFull ? FullHeart : EmptyHeart;
            heartTexts[i].color = heartIsFull ? fullHeartColor : emptyHeartColor;
        }
    }

    private Text CreateHeartText(Transform parent, int index)
    {
        GameObject heartObject = new GameObject($"Heart{index + 1}");
        heartObject.transform.SetParent(parent, false);

        RectTransform heartRect = heartObject.AddComponent<RectTransform>();
        heartRect.sizeDelta = new Vector2(36f, 36f);

        Text heartText = heartObject.AddComponent<Text>();
        heartText.text = FullHeart;
        heartText.alignment = TextAnchor.MiddleCenter;
        heartText.fontSize = heartFontSize;
        heartText.color = fullHeartColor;
        heartText.font = GetUiFont();
        heartText.raycastTarget = false;

        return heartText;
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
