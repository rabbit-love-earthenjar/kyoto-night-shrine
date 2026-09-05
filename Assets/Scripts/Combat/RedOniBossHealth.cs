using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RedOniBossHealth : MonoBehaviour
{
    [SerializeField, Min(3)] private int maxHP = 60;
    [SerializeField, Min(2)] private int phaseOneEndHP = 40;
    [SerializeField, Min(1)] private int phaseTwoEndHP = 20;
    [SerializeField, Min(0f)] private float phaseTransitionDuration = 0.9f;
    [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.08f;
    [Header("Final Rush")]
    [SerializeField, Min(1f)] private float finalRushMouseSpeedThreshold = 60f;
    [SerializeField, Min(1)] private int finalRushRequiredHits = 20;
    [SerializeField] private RedOniPhaseOneController phaseOneController;
    [SerializeField] private RedOniPhaseThreeAddsController phaseThreeAdds;

    private int currentHP;
    private bool phaseOneComplete;
    private bool phaseTwoComplete;
    private bool phaseThreeComplete;
    private bool bossDefeated;
    private bool phaseTransitioning;
    private int finalRushHits;
    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    private Coroutine flashRoutine;
    private Coroutine phaseTransitionRoutine;
    private Image healthFill;
    private RectTransform healthFillRect;
    private Image residualWhiteFill;
    private RectTransform residualWhiteFillRect;
    private Text healthLabel;
    private GameObject healthCanvas;
    private Text phaseMessage;
    private FaithBeanShooter faithBeanShooter;
    private float nextFinalRushUiRefresh;

    private static readonly Color NormalFillColor = new Color(0.84f, 0.12f, 0.08f, 1f);
    private static readonly Color HitFillColor = new Color(1f, 0.86f, 0.28f, 1f);
    private static readonly Color PhaseTwoFillColor = new Color(0.82f, 0.3f, 0.1f, 1f);
    private static readonly Color PhaseThreeFillColor = new Color(0.68f, 0.16f, 0.72f, 1f);
    private static readonly Color FinalRushFillColor = new Color(0.96f, 0.98f, 1f, 1f);

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public int PhaseOneEndHP => phaseOneEndHP;
    public int PhaseTwoEndHP => phaseTwoEndHP;
    public bool PhaseOneComplete => phaseOneComplete;
    public bool PhaseTwoComplete => phaseTwoComplete;
    public bool PhaseThreeComplete => phaseThreeComplete;
    public bool BossDefeated => bossDefeated;
    public bool IsTransitioning => phaseTransitioning;
    public bool FinalRushActive => phaseThreeComplete && !bossDefeated && !phaseTransitioning;
    public int FinalRushHits => finalRushHits;
    public int FinalRushRequiredHits => Mathf.Max(1, finalRushRequiredHits);
    public float FinalRushMouseSpeedThreshold => Mathf.Max(1f, finalRushMouseSpeedThreshold);
    public int CurrentPhase => phaseThreeComplete ? 4 : phaseTwoComplete ? 3 : phaseOneComplete ? 2 : 1;

    private void Awake()
    {
        if (phaseOneController == null)
        {
            phaseOneController = GetComponent<RedOniPhaseOneController>();
        }

        if (phaseThreeAdds == null)
        {
            phaseThreeAdds = FindAnyObjectByType<RedOniPhaseThreeAddsController>();
        }

        CacheRenderers();
        ResetEncounter();
        EnsureHealthUi();
    }

    private void Update()
    {
        if (!phaseThreeComplete || Time.unscaledTime < nextFinalRushUiRefresh)
        {
            return;
        }

        nextFinalRushUiRefresh = Time.unscaledTime + 0.08f;
        UpdateHealthUi();
    }

    private void OnEnable()
    {
        GameManager.RetryCompleted += RetryCurrentPhase;
    }

    private void OnDisable()
    {
        GameManager.RetryCompleted -= RetryCurrentPhase;
    }

    public void Configure(
        RedOniPhaseOneController controller,
        int maximumHP,
        int firstPhaseEndHP,
        int secondPhaseEndHP)
    {
        phaseOneController = controller;
        maxHP = Mathf.Max(3, maximumHP);
        phaseOneEndHP = Mathf.Clamp(firstPhaseEndHP, 2, maxHP - 1);
        phaseTwoEndHP = Mathf.Clamp(secondPhaseEndHP, 1, phaseOneEndHP - 1);
        currentHP = maxHP;
    }

    public void ConfigurePhaseThreeAdds(RedOniPhaseThreeAddsController addsController)
    {
        phaseThreeAdds = addsController;
    }

    public void ConfigureFinalRush(float mouseSpeedThreshold, int requiredHits)
    {
        finalRushMouseSpeedThreshold = Mathf.Max(1f, mouseSpeedThreshold);
        finalRushRequiredHits = Mathf.Max(1, requiredHits);
    }

    public void Configure(RedOniPhaseOneController controller, int maximumHP, int phaseEndHP)
    {
        Configure(controller, maximumHP, phaseEndHP, Mathf.Max(1, phaseEndHP / 2));
    }

    public void TakeDamage(int damage, Vector2 hitPosition)
    {
        ApplyDamage(damage, hitPosition, false, 0f);
    }

    public void TakeFaithBeanDamage(int damage, Vector2 hitPosition, float mouseAttackSpeed)
    {
        ApplyDamage(damage, hitPosition, true, mouseAttackSpeed);
    }

    private void ApplyDamage(
        int damage,
        Vector2 hitPosition,
        bool isFaithBean,
        float mouseAttackSpeed)
    {
        if (bossDefeated || phaseTransitioning || damage <= 0)
        {
            return;
        }

        if (phaseThreeComplete)
        {
            if (!isFaithBean || mouseAttackSpeed < FinalRushMouseSpeedThreshold)
            {
                UpdateHealthUi();
                return;
            }

            finalRushHits = Mathf.Min(FinalRushRequiredHits, finalRushHits + damage);
            ShowHitFeedback(hitPosition, damage);

            if (finalRushHits >= FinalRushRequiredHits)
            {
                CompleteEncounter();
            }

            return;
        }

        int activeThreshold = phaseTwoComplete
            ? 0
            : phaseOneComplete
                ? phaseTwoEndHP
                : phaseOneEndHP;
        currentHP = Mathf.Max(activeThreshold, currentHP - damage);
        ShowHitFeedback(hitPosition, damage);

        if (!phaseOneComplete && currentHP <= phaseOneEndHP)
        {
            phaseTransitionRoutine = StartCoroutine(BeginPhaseTwo());
        }
        else if (phaseOneComplete && !phaseTwoComplete && currentHP <= phaseTwoEndHP)
        {
            phaseTransitionRoutine = StartCoroutine(BeginPhaseThree());
        }
        else if (phaseTwoComplete && currentHP <= 0)
        {
            BeginFinalRush();
        }
    }

    public void ResetEncounter()
    {
        ResetToPhaseCheckpoint(1);
    }

    private void RetryCurrentPhase()
    {
        ResetToPhaseCheckpoint(CurrentPhase);
    }

    private void ResetToPhaseCheckpoint(int phase)
    {
        if (phaseTransitionRoutine != null)
        {
            StopCoroutine(phaseTransitionRoutine);
            phaseTransitionRoutine = null;
        }

        int checkpointPhase = Mathf.Clamp(phase, 1, 4);
        currentHP = checkpointPhase == 1
            ? Mathf.Max(3, maxHP)
            : checkpointPhase == 2
                ? phaseOneEndHP
                : checkpointPhase == 3
                    ? phaseTwoEndHP
                    : 0;
        phaseOneComplete = checkpointPhase >= 2;
        phaseTwoComplete = checkpointPhase >= 3;
        phaseThreeComplete = checkpointPhase >= 4;
        finalRushHits = 0;
        bossDefeated = false;
        phaseTransitioning = false;
        phaseThreeAdds?.ResetEncounter();
        RestoreRendererColors();
        phaseOneController?.SetCombatPhase(checkpointPhase);

        if (checkpointPhase == 3)
        {
            phaseThreeAdds?.BeginPhaseThree();
        }

        phaseOneController?.SetEncounterActive(true);
        SetPhaseMessage(string.Empty, false);
        UpdateHealthUi();
    }

    private IEnumerator BeginPhaseTwo()
    {
        phaseOneComplete = true;
        phaseTransitioning = true;
        phaseOneController?.SetEncounterActive(false);
        SetPhaseMessage("PHASE 2", true);
        UpdateHealthUi();

        yield return new WaitForSeconds(Mathf.Max(0f, phaseTransitionDuration));

        SetPhaseMessage(string.Empty, false);
        phaseTransitioning = false;
        phaseOneController?.SetCombatPhase(2);
        phaseOneController?.SetEncounterActive(true);
        UpdateHealthUi();
        phaseTransitionRoutine = null;
    }

    private IEnumerator BeginPhaseThree()
    {
        phaseTwoComplete = true;
        phaseTransitioning = true;
        phaseOneController?.SetEncounterActive(false);
        SetPhaseMessage("PHASE 3", true);
        UpdateHealthUi();

        yield return new WaitForSeconds(Mathf.Max(0f, phaseTransitionDuration));

        SetPhaseMessage(string.Empty, false);
        phaseTransitioning = false;
        phaseOneController?.SetCombatPhase(3);
        phaseThreeAdds?.BeginPhaseThree();
        phaseOneController?.SetEncounterActive(true);
        UpdateHealthUi();
        phaseTransitionRoutine = null;
    }

    private void BeginFinalRush()
    {
        phaseThreeComplete = true;
        finalRushHits = 0;
        phaseTransitioning = false;
        phaseThreeAdds?.EndPhaseThree();
        phaseOneController?.SetCombatPhase(4);
        phaseOneController?.SetEncounterActive(true);
        SetPhaseMessage(string.Empty, false);
        UpdateHealthUi();
        phaseTransitionRoutine = null;
    }

    private void CompleteEncounter()
    {
        bossDefeated = true;
        phaseTransitioning = false;
        phaseThreeAdds?.EndPhaseThree();
        phaseOneController?.SetEncounterActive(false);
        GameManager.Instance?.ShowStageClear();
    }

    private void ShowHitFeedback(Vector2 hitPosition, int damage)
    {
        UpdateHealthUi(damage);
        CombatFeedbackEffects.SpawnAttackStart(hitPosition, Vector2.up, 2);

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
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
            healthFill.color = GetPhaseFillColor();
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

        phaseMessage = CreateText(root.transform, new Vector2(0f, -54f), new Vector2(480f, 42f), 30);
        phaseMessage.color = new Color(1f, 0.86f, 0.35f, 1f);
        phaseMessage.gameObject.SetActive(false);

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

        GameObject residualObject = new GameObject("BossResidualWhiteFill");
        residualObject.transform.SetParent(backgroundObject.transform, false);
        residualWhiteFillRect = residualObject.AddComponent<RectTransform>();
        residualWhiteFillRect.anchorMin = Vector2.zero;
        residualWhiteFillRect.anchorMax = Vector2.zero;
        residualWhiteFillRect.offsetMin = new Vector2(3f, 3f);
        residualWhiteFillRect.offsetMax = new Vector2(-3f, -3f);
        residualWhiteFill = residualObject.AddComponent<Image>();
        residualWhiteFill.color = FinalRushFillColor;
        residualWhiteFill.type = Image.Type.Simple;

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

        float normalHealthRatio = maxHP > 0
            ? Mathf.Clamp01(currentHP / (float)maxHP)
            : 0f;
        float finalRushSectionRatio = maxHP > 0
            ? Mathf.Clamp01(phaseTwoEndHP / (float)maxHP)
            : 0f;

        if (healthFillRect != null)
        {
            float healthRatio = phaseThreeComplete
                ? 0f
                : normalHealthRatio;
            healthFillRect.anchorMax = new Vector2(healthRatio, 1f);
            healthFillRect.offsetMin = new Vector2(3f, 3f);
            healthFillRect.offsetMax = new Vector2(-3f, -3f);
        }

        if (residualWhiteFillRect != null)
        {
            bool previewResidual = phaseTwoComplete
                && !phaseThreeComplete
                && currentHP <= Mathf.Max(1, phaseTwoEndHP / 2);
            float residualRatio = phaseThreeComplete
                ? finalRushSectionRatio
                    * (1f - Mathf.Clamp01(finalRushHits / (float)FinalRushRequiredHits))
                : previewResidual
                    ? finalRushSectionRatio
                    : 0f;
            residualWhiteFillRect.anchorMax = new Vector2(residualRatio, 1f);
            residualWhiteFillRect.offsetMin = new Vector2(3f, 3f);
            residualWhiteFillRect.offsetMax = new Vector2(-3f, -3f);
        }

        if (healthLabel != null)
        {
            if (phaseThreeComplete)
            {
                float mouseSpeed = ResolveFaithBeanShooter() != null
                    ? faithBeanShooter.CurrentMouseAttackSpeed
                    : 0f;
                string speedState = mouseSpeed >= FinalRushMouseSpeedThreshold
                    ? "READY"
                    : "KEEP SPEED";
                healthLabel.text = $"FINAL RUSH   Speed {mouseSpeed:0}/{FinalRushMouseSpeedThreshold:0} "
                    + $"{speedState}   Hits {finalRushHits}/{FinalRushRequiredHits}";
            }
            else
            {
                string damageFeedback = recentDamage > 0 ? $"   -{recentDamage}" : string.Empty;
                healthLabel.text = $"Red Oni  Phase {CurrentPhase}   {currentHP} / {maxHP}{damageFeedback}";
            }
        }

        if (healthFill != null && flashRoutine == null)
        {
            healthFill.color = GetPhaseFillColor();
        }
    }

    private Color GetPhaseFillColor()
    {
        if (phaseThreeComplete)
        {
            return FinalRushFillColor;
        }

        if (phaseTwoComplete)
        {
            return PhaseThreeFillColor;
        }

        return phaseOneComplete ? PhaseTwoFillColor : NormalFillColor;
    }

    private FaithBeanShooter ResolveFaithBeanShooter()
    {
        if (faithBeanShooter == null)
        {
            faithBeanShooter = FindFirstObjectByType<FaithBeanShooter>();
        }

        return faithBeanShooter;
    }

    private void SetPhaseMessage(string message, bool visible)
    {
        if (phaseMessage == null)
        {
            return;
        }

        phaseMessage.text = message;
        phaseMessage.gameObject.SetActive(visible);
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
        text.raycastTarget = false;
        NightShrineTextStyle.Apply(text, NightShrineTextRole.Number);
        return text;
    }
}
