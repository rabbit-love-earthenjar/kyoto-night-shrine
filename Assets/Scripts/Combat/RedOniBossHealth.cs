using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RedOniBossHealth : MonoBehaviour
{
    [SerializeField, Min(3)] private int maxHP = 30;
    [SerializeField, Min(1)] private int phaseOneEndHP = 20;
    [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.08f;
    [SerializeField] private RedOniPhaseOneController phaseOneController;

    private int currentHP;
    private bool phaseOneComplete;
    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    private Coroutine flashRoutine;
    private Image healthFill;
    private RectTransform healthFillRect;
    private Text healthLabel;
    private GameObject healthCanvas;

    private static readonly Color NormalFillColor = new Color(0.84f, 0.12f, 0.08f, 1f);
    private static readonly Color HitFillColor = new Color(1f, 0.86f, 0.28f, 1f);

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public int PhaseOneEndHP => phaseOneEndHP;
    public bool PhaseOneComplete => phaseOneComplete;

    private void Awake()
    {
        if (phaseOneController == null)
        {
            phaseOneController = GetComponent<RedOniPhaseOneController>();
        }

        CacheRenderers();
        ResetEncounter();
        EnsureHealthUi();
    }

    private void OnEnable()
    {
        GameManager.RetryCompleted += ResetEncounter;
    }

    private void OnDisable()
    {
        GameManager.RetryCompleted -= ResetEncounter;
    }

    public void Configure(RedOniPhaseOneController controller, int maximumHP, int phaseEndHP)
    {
        phaseOneController = controller;
        maxHP = Mathf.Max(3, maximumHP);
        phaseOneEndHP = Mathf.Clamp(phaseEndHP, 1, maxHP - 1);
        currentHP = maxHP;
    }

    public void TakeDamage(int damage, Vector2 hitPosition)
    {
        if (phaseOneComplete || damage <= 0)
        {
            return;
        }

        currentHP = Mathf.Max(phaseOneEndHP, currentHP - damage);
        UpdateHealthUi(damage);
        CombatFeedbackEffects.SpawnAttackStart(hitPosition, Vector2.up, 2);

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());

        if (currentHP <= phaseOneEndHP)
        {
            CompletePhaseOne();
        }
    }

    public void ResetEncounter()
    {
        currentHP = Mathf.Max(3, maxHP);
        phaseOneComplete = false;
        RestoreRendererColors();
        phaseOneController?.SetEncounterActive(true);
        UpdateHealthUi();
    }

    private void CompletePhaseOne()
    {
        phaseOneComplete = true;
        phaseOneController?.SetEncounterActive(false);
        GameManager.Instance?.ShowStageClear();
    }

    private IEnumerator FlashRoutine()
    {
        for (int index = 0; index < renderers.Length; index++)
        {
            if (renderers[index] != null)
            {
                renderers[index].color = Color.white;
            }
        }

        if (healthFill != null)
        {
            healthFill.color = HitFillColor;
        }

        yield return new WaitForSeconds(hitFlashDuration);
        RestoreRendererColors();

        if (healthFill != null)
        {
            healthFill.color = NormalFillColor;
        }

        yield return new WaitForSeconds(0.24f);
        UpdateHealthUi();
        flashRoutine = null;
    }

    private void CacheRenderers()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];

        for (int index = 0; index < renderers.Length; index++)
        {
            originalColors[index] = renderers[index] != null
                ? renderers[index].color
                : Color.white;
        }
    }

    private void RestoreRendererColors()
    {
        if (renderers == null || originalColors == null)
        {
            return;
        }

        for (int index = 0; index < renderers.Length && index < originalColors.Length; index++)
        {
            if (renderers[index] != null)
            {
                renderers[index].color = originalColors[index];
            }
        }
    }

    private void EnsureHealthUi()
    {
        if (healthCanvas != null)
        {
            return;
        }

        healthCanvas = new GameObject("RedOniBossHealthCanvas");
        Canvas canvas = healthCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 92;

        CanvasScaler scaler = healthCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        GameObject root = new GameObject("BossHealthRoot");
        root.transform.SetParent(healthCanvas.transform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -24f);
        rootRect.sizeDelta = new Vector2(480f, 52f);

        healthLabel = CreateText(root.transform, new Vector2(0f, -2f), new Vector2(480f, 26f), 18);

        GameObject backgroundObject = new GameObject("BossHealthBackground");
        backgroundObject.transform.SetParent(root.transform, false);
        RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 0f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0f);
        backgroundRect.pivot = new Vector2(0.5f, 0f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = new Vector2(450f, 20f);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.06f, 0.08f, 0.92f);

        GameObject fillObject = new GameObject("BossHealthFill");
        fillObject.transform.SetParent(backgroundObject.transform, false);
        healthFillRect = fillObject.AddComponent<RectTransform>();
        healthFillRect.anchorMin = Vector2.zero;
        healthFillRect.anchorMax = Vector2.one;
        healthFillRect.offsetMin = new Vector2(3f, 3f);
        healthFillRect.offsetMax = new Vector2(-3f, -3f);
        healthFill = fillObject.AddComponent<Image>();
        healthFill.color = NormalFillColor;
        healthFill.type = Image.Type.Simple;

        UpdateHealthUi();
    }

    private void UpdateHealthUi(int recentDamage = 0)
    {
        if (healthCanvas == null)
        {
            EnsureHealthUi();
            return;
        }

        if (healthFillRect != null)
        {
            float healthRatio = maxHP > 0
                ? Mathf.Clamp01(currentHP / (float)maxHP)
                : 0f;
            healthFillRect.anchorMax = new Vector2(healthRatio, 1f);
            healthFillRect.offsetMin = new Vector2(3f, 3f);
            healthFillRect.offsetMax = new Vector2(-3f, -3f);
        }

        if (healthLabel != null)
        {
            string damageFeedback = recentDamage > 0 ? $"   -{recentDamage}" : string.Empty;
            healthLabel.text = $"赤鬼　{currentHP} / {maxHP}{damageFeedback}";
        }
    }

    private static Text CreateText(Transform parent, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject textObject = new GameObject("BossHealthLabel");
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.raycastTarget = false;
        return text;
    }
}
