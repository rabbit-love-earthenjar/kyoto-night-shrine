using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HubFarmPanelController : MonoBehaviour
{
    private const string FarmMarkerName = "FarmField_IngredientGarden";
    private const string FarmTitle = "素材畑";
    private const int MinimumFarmPlotCount = 9;
    private static readonly Vector2[] FarmPlotPositions =
    {
        new Vector2(-198f, 102f),
        new Vector2(0f, 102f),
        new Vector2(198f, 102f),
        new Vector2(-198f, -8f),
        new Vector2(0f, -8f),
        new Vector2(198f, -8f),
        new Vector2(-198f, -118f),
        new Vector2(0f, -118f),
        new Vector2(198f, -118f)
    };

    [Header("World Marker")]
    [SerializeField] private Sprite farmMarkerSprite;
    [SerializeField] private string farmMarkerSpritePath = "Art/stage_icon/farm_icon.png";
    [SerializeField] private Vector2 farmPosition = new Vector2(-2.55f, 2.28f);
    [SerializeField] private Vector3 farmScale = new Vector3(0.135f, 0.135f, 1f);
    [SerializeField] private Vector2 farmColliderSize = new Vector2(1.7f, 1.35f);
    [SerializeField] private Vector2 farmLabelOffset = new Vector2(0f, -0.95f);

    [Header("Panel Art")]
    [SerializeField] private Sprite farmBackgroundSprite;
    [SerializeField] private string farmBackgroundPath = "Art/Backgrounds/farm.png";

    private FarmController farmController;
    private GameObject panelObject;
    private Text statusText;
    private Text stockText;
    private Text selectedCropText;
    private Text[] plotTexts;
    private Image[] plotImages;
    private Image[] plotCropImages;
    private Image[] plotProgressTrackImages;
    private Image[] plotProgressFillImages;
    private Text[] plotActionTexts;
    private GameObject seedSelectionObject;
    private Text seedFaithText;
    private readonly Text[] seedCountTexts = new Text[3];
    private int pendingPlantPlotIndex = -1;
    private GameObject actionPopupObject;
    private Image actionAnimationImage;
    private Text actionTitleText;
    private Text actionHintText;
    private Sprite loadedFarmMarkerSprite;
    private Sprite loadedFarmBackgroundSprite;
    private ResourceInventory resourceInventory;
    private readonly Dictionary<string, Sprite> cropSpriteCache = new Dictionary<string, Sprite>();
    private Coroutine actionAnimationRoutine;
    private bool actionAnimationPlaying;
    private float nextPanelRefreshTime;

    private void Awake()
    {
        ResolveFarmController();
        CreateFarmMarker();
    }

    private void Update()
    {
        if (panelObject == null || !panelObject.activeSelf || Time.unscaledTime < nextPanelRefreshTime)
        {
            return;
        }

        nextPanelRefreshTime = Time.unscaledTime + 0.25f;
        RefreshPanel();
    }

    public void ShowPanel()
    {
        EnsureEventSystem();
        EnsurePanel();
        ResolveResourceInventory().EnsureFarmStarterSeeds();
        RefreshPanel();
        panelObject.SetActive(true);
    }

    public void HidePanel()
    {
        StopFarmActionAnimation();

        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }
    }

    public bool TryCloseOverlay()
    {
        if (actionAnimationPlaying)
        {
            return true;
        }

        if (seedSelectionObject != null && seedSelectionObject.activeSelf)
        {
            HideSeedSelection();
            return true;
        }

        if (panelObject == null || !panelObject.activeSelf)
        {
            return false;
        }

        HidePanel();
        return true;
    }

    private void ClickPlot(int plotIndex)
    {
        if (actionAnimationPlaying)
        {
            statusText.text = "作業中です。";
            return;
        }

        FarmPlotPhase phase = farmController.GetPlotPhase(plotIndex);

        if (phase == FarmPlotPhase.Empty)
        {
            ShowSeedSelection(plotIndex);
            return;
        }

        if (phase == FarmPlotPhase.Ready)
        {
            FarmCropDefinition crop = farmController.GetPlotCropDefinition(plotIndex);
            string outputName = crop != null ? crop.OutputIngredientId : "ingredient";

            if (farmController.TryHarvestCrop(plotIndex))
            {
                statusText.text = $"{GetIngredientDisplayName(outputName)}を収穫しました。";
                PlayFarmActionAnimation(true);
            }
            else
            {
                statusText.text = "まだ収穫できません。";
            }

            RefreshPanel();
            return;
        }

        statusText.text = "まだ育っています。";
        RefreshPanel();
    }

    private void PlantPendingPlot(FarmCropKind cropKind)
    {
        if (pendingPlantPlotIndex < 0)
        {
            HideSeedSelection();
            return;
        }

        string cropId = GetCropId(cropKind);
        string seedId = FarmEconomyFormula.GetSeedId(cropId);

        if (!ResolveResourceInventory().HasSeed(seedId, 1))
        {
            statusText.text = "種がありません。購入してください。";
            RefreshSeedSelection();
            return;
        }

        if (farmController.TryPlantCrop(pendingPlantPlotIndex, cropKind))
        {
            statusText.text = "種をまきました。";
            HideSeedSelection();
            PlayFarmActionAnimation(false);
            RefreshPanel();
            return;
        }

        statusText.text = "ここには植えられません。";
        HideSeedSelection();
        RefreshPanel();
    }

    private void PurchaseSeed(FarmCropKind cropKind)
    {
        string cropId = GetCropId(cropKind);
        string seedId = FarmEconomyFormula.GetSeedId(cropId);
        int price = FarmEconomyFormula.CalculateSeedPrice(cropId);
        ResourceInventory inventory = ResolveResourceInventory();

        if (!inventory.SpendFaithPoints(price))
        {
            statusText.text = "信仰値が足りません。";
            RefreshSeedSelection();
            return;
        }

        inventory.AddSeed(seedId, 1);
        statusText.text = $"{GetSeedDisplayName(cropKind)}を購入しました。";
        RefreshSeedSelection();
        RefreshPanel();
    }

    private void CreateFarmMarker()
    {
        GameObject existingFarmObject = GameObject.Find(FarmMarkerName);

        if (existingFarmObject != null)
        {
            ApplyFarmMarkerLayout(existingFarmObject);
            return;
        }

        GameObject farmObject = new GameObject(FarmMarkerName);
        Transform buildingsRoot = transform.Find("Buildings");

        if (buildingsRoot != null)
        {
            farmObject.transform.SetParent(buildingsRoot, false);
        }

        farmObject.transform.position = farmPosition;

        GameObject visualObject = new GameObject("FarmFieldVisual");
        visualObject.transform.SetParent(farmObject.transform, false);
        visualObject.transform.localScale = farmScale;

        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = ResolveFarmMarkerSprite();
        renderer.color = renderer.sprite != null
            ? Color.white
            : new Color(0.45f, 0.78f, 0.34f, 1f);
        renderer.sortingOrder = 2;

        if (renderer.sprite == null)
        {
            renderer.sprite = CreateFallbackSquareSprite();
        }

        BoxCollider2D collider = farmObject.AddComponent<BoxCollider2D>();
        collider.size = farmColliderSize;

        HubMapInteractable interactable = farmObject.AddComponent<HubMapInteractable>();
        interactable.Configure(FindAnyObjectByType<HubMapController>(), HubInteractionType.Farm);

        GameObject labelObject = new GameObject("FarmFieldLabel");
        labelObject.transform.SetParent(farmObject.transform, false);
        labelObject.transform.localPosition = farmLabelOffset;

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = "畑";
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 42;
        label.characterSize = 0.06f;
        label.color = new Color(0.88f, 1f, 0.78f, 1f);
    }

    private void ApplyFarmMarkerLayout(GameObject farmObject)
    {
        farmObject.transform.position = farmPosition;

        Transform visual = farmObject.transform.Find("FarmFieldVisual");
        if (visual != null)
        {
            visual.localScale = farmScale;
        }

        BoxCollider2D collider = farmObject.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.size = farmColliderSize;
        }

        Transform label = farmObject.transform.Find("FarmFieldLabel");
        if (label != null)
        {
            label.localPosition = farmLabelOffset;
        }
    }

    private void EnsurePanel()
    {
        if (panelObject != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("HubFarmCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 128;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasObject.AddComponent<GraphicRaycaster>();

        panelObject = new GameObject("HubFarmPanel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(920f, 560f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.sprite = ResolveFarmBackgroundSprite();
        panelImage.preserveAspect = false;
        panelImage.color = panelImage.sprite != null
            ? Color.white
            : new Color(0.11f, 0.18f, 0.08f, 1f);

        CreateDecorPanel("FarmHeader", panelObject.transform, new Vector2(0f, 226f), new Vector2(420f, 48f), new Color(0.1f, 0.065f, 0.035f, 0.78f));
        Text title = CreateText(FarmTitle, panelObject.transform, new Vector2(0f, 232f), new Vector2(360f, 34f), 28, TextAnchor.MiddleCenter);
        title.color = new Color(1f, 0.94f, 0.78f, 1f);

        stockText = CreateText(string.Empty, panelObject.transform, new Vector2(0f, 194f), new Vector2(620f, 28f), 16, TextAnchor.MiddleCenter);
        selectedCropText = CreateText(string.Empty, panelObject.transform, new Vector2(0f, 152f), new Vector2(620f, 24f), 15, TextAnchor.MiddleCenter);
        selectedCropText.text = string.Empty;

        plotTexts = new Text[farmController.Plots.Count];
        plotImages = new Image[farmController.Plots.Count];
        plotCropImages = new Image[farmController.Plots.Count];
        plotProgressTrackImages = new Image[farmController.Plots.Count];
        plotProgressFillImages = new Image[farmController.Plots.Count];
        plotActionTexts = new Text[farmController.Plots.Count];

        for (int i = 0; i < farmController.Plots.Count; i++)
        {
            int plotIndex = i;
            Vector2 position = GetFarmPlotPosition(i);
            Button plotButton = CreateTransparentPlotButton($"FarmPlot_{i + 1:00}", panelObject.transform, position, new Vector2(174f, 86f), () => ClickPlot(plotIndex));
            plotImages[i] = plotButton.GetComponent<Image>();
            plotCropImages[i] = CreateIconImage("CropIcon", plotButton.transform, new Vector2(0f, -22f), new Vector2(98f, 98f));
            plotCropImages[i].rectTransform.pivot = new Vector2(0.5f, 0f);
            plotProgressTrackImages[i] = CreateDecorPanel("GrowthTrack", plotButton.transform, new Vector2(0f, -40f), new Vector2(112f, 7f), new Color(0.16f, 0.1f, 0.05f, 0.72f));
            plotProgressFillImages[i] = CreateDecorPanel("GrowthFill", plotButton.transform, new Vector2(-56f, -40f), new Vector2(0f, 7f), new Color(0.55f, 0.86f, 0.36f, 0.95f));
            plotProgressFillImages[i].rectTransform.pivot = new Vector2(0f, 0.5f);
            plotTexts[i] = CreateText(string.Empty, plotButton.transform, new Vector2(0f, -50f), new Vector2(154f, 20f), 12, TextAnchor.MiddleCenter);
            plotTexts[i].color = new Color(0.08f, 0.05f, 0.025f, 1f);
            plotActionTexts[i] = CreateText(string.Empty, plotButton.transform, new Vector2(0f, -62f), new Vector2(154f, 18f), 1, TextAnchor.MiddleCenter);
            plotActionTexts[i].color = new Color(0.2f, 0.1f, 0.02f, 1f);
            plotActionTexts[i].enabled = false;
        }

        statusText = CreateText(string.Empty, panelObject.transform, new Vector2(0f, -224f), new Vector2(660f, 30f), 17, TextAnchor.MiddleCenter);
        CreateButtonWithLabel("CloseButton", "閉じる", panelObject.transform, new Vector2(0f, -260f), new Vector2(160f, 42f), HidePanel);
        CreateSeedSelectionPopup();
        CreateActionPopup();
        panelObject.SetActive(false);
    }

    private void RefreshPanel()
    {
        ResourceInventory inventory = ResolveResourceInventory();
        stockText.text =
            $"小麦粉 {inventory.GetIngredientCount(ResourceInventory.FlourId)}   "
            + $"コーヒー豆 {inventory.GetIngredientCount(ResourceInventory.CoffeeBeanId)}   "
            + $"砂糖 {inventory.GetIngredientCount(ResourceInventory.SugarId)}";
        selectedCropText.text = string.Empty;

        for (int i = 0; i < plotTexts.Length; i++)
        {
            FarmPlotPhase phase = farmController.GetPlotPhase(i);
            FarmCropDefinition crop = farmController.GetPlotCropDefinition(i);
            float growth = farmController.GetPlotGrowth01(i);

            if (plotImages[i] != null)
            {
                plotImages[i].color = Color.clear;
            }

            if (phase == FarmPlotPhase.Empty)
            {
                SetPlotCropIcon(i, null, null, false);
                SetPlotGrowthBar(i, 0f, false, false);
                plotTexts[i].text = string.Empty;
                plotActionTexts[i].text = string.Empty;
                continue;
            }

            string cropName = crop != null ? crop.DisplayName : "Crop";
            SetPlotCropIcon(i, GetCropSprite(crop, phase), crop, true);
            SetPlotGrowthBar(i, growth, true, phase == FarmPlotPhase.Ready);
            plotTexts[i].text = string.Empty;
            plotActionTexts[i].text = string.Empty;
        }

        if (seedSelectionObject != null && seedSelectionObject.activeSelf)
        {
            RefreshSeedSelection();
        }
    }

    private void CreateSeedSelectionPopup()
    {
        seedSelectionObject = new GameObject("FarmSeedSelectionRoot");
        seedSelectionObject.transform.SetParent(panelObject.transform, false);

        RectTransform rootRect = seedSelectionObject.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = Vector2.zero;

        Image dimImage = seedSelectionObject.AddComponent<Image>();
        dimImage.color = new Color(0.02f, 0.018f, 0.012f, 0.28f);

        GameObject cardObject = new GameObject("SeedCard");
        cardObject.transform.SetParent(seedSelectionObject.transform, false);

        RectTransform cardRect = cardObject.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = new Vector2(0f, 8f);
        cardRect.sizeDelta = new Vector2(500f, 270f);

        Image cardImage = cardObject.AddComponent<Image>();
        cardImage.color = new Color(0.93f, 0.84f, 0.64f, 0.98f);

        Text title = CreateText("種を選ぶ / 種子商店", cardObject.transform, new Vector2(0f, 105f), new Vector2(420f, 30f), 22, TextAnchor.MiddleCenter);
        title.color = new Color(0.13f, 0.07f, 0.03f, 1f);

        seedFaithText = CreateText(string.Empty, cardObject.transform, new Vector2(0f, 78f), new Vector2(420f, 24f), 15, TextAnchor.MiddleCenter);
        seedFaithText.color = new Color(0.13f, 0.07f, 0.03f, 1f);

        seedCountTexts[(int)FarmCropKind.Wheat] = CreateCropButton("SeedButton_Wheat", "麦", FarmCropKind.Wheat, new Vector2(-150f, 22f), () => PlantPendingPlot(FarmCropKind.Wheat), cardObject.transform);
        seedCountTexts[(int)FarmCropKind.CoffeeBean] = CreateCropButton("SeedButton_Coffee", "コーヒー", FarmCropKind.CoffeeBean, new Vector2(0f, 22f), () => PlantPendingPlot(FarmCropKind.CoffeeBean), cardObject.transform);
        seedCountTexts[(int)FarmCropKind.Sugarcane] = CreateCropButton("SeedButton_Sugarcane", "砂糖きび", FarmCropKind.Sugarcane, new Vector2(150f, 22f), () => PlantPendingPlot(FarmCropKind.Sugarcane), cardObject.transform);

        CreateButtonWithLabel("BuySeed_Wheat", $"購入 {FarmEconomyFormula.CalculateSeedPrice("wheat")}信仰", cardObject.transform, new Vector2(-150f, -50f), new Vector2(130f, 34f), () => PurchaseSeed(FarmCropKind.Wheat));
        CreateButtonWithLabel("BuySeed_Coffee", $"購入 {FarmEconomyFormula.CalculateSeedPrice("coffee_bean")}信仰", cardObject.transform, new Vector2(0f, -50f), new Vector2(130f, 34f), () => PurchaseSeed(FarmCropKind.CoffeeBean));
        CreateButtonWithLabel("BuySeed_Sugarcane", $"購入 {FarmEconomyFormula.CalculateSeedPrice("sugarcane")}信仰", cardObject.transform, new Vector2(150f, -50f), new Vector2(130f, 34f), () => PurchaseSeed(FarmCropKind.Sugarcane));
        CreateButtonWithLabel("CancelSeedButton", "やめる", cardObject.transform, new Vector2(0f, -104f), new Vector2(128f, 34f), HideSeedSelection);

        seedSelectionObject.SetActive(false);
    }

    private void ShowSeedSelection(int plotIndex)
    {
        pendingPlantPlotIndex = plotIndex;
        statusText.text = "種を選んでください。";

        if (seedSelectionObject != null)
        {
            seedSelectionObject.SetActive(true);
            seedSelectionObject.transform.SetAsLastSibling();
            RefreshSeedSelection();
        }
    }

    private void RefreshSeedSelection()
    {
        ResourceInventory inventory = ResolveResourceInventory();

        if (seedFaithText != null)
        {
            seedFaithText.text = $"信仰値 {inventory.FaithPoints}";
        }

        RefreshSeedCount(FarmCropKind.Wheat, "麦");
        RefreshSeedCount(FarmCropKind.CoffeeBean, "コーヒー");
        RefreshSeedCount(FarmCropKind.Sugarcane, "砂糖きび");
    }

    private void RefreshSeedCount(FarmCropKind cropKind, string label)
    {
        Text countText = seedCountTexts[(int)cropKind];

        if (countText == null)
        {
            return;
        }

        string cropId = GetCropId(cropKind);
        string seedId = FarmEconomyFormula.GetSeedId(cropId);
        int outputAmount = FarmEconomyFormula.StarterOutputAmount;
        int growthSeconds = FarmEconomyFormula.CalculateGrowthSeconds(cropId, outputAmount);
        countText.text = $"{label}\n種 x{ResolveResourceInventory().GetSeedCount(seedId)}\n収穫 x{outputAmount} / {growthSeconds}秒";
    }

    private void HideSeedSelection()
    {
        pendingPlantPlotIndex = -1;

        if (seedSelectionObject != null)
        {
            seedSelectionObject.SetActive(false);
        }
    }

    private void CreateActionPopup()
    {
        actionPopupObject = new GameObject("FarmActionPopupRoot");
        actionPopupObject.transform.SetParent(panelObject.transform, false);

        RectTransform popupRootRect = actionPopupObject.AddComponent<RectTransform>();
        popupRootRect.anchorMin = Vector2.zero;
        popupRootRect.anchorMax = Vector2.one;
        popupRootRect.pivot = new Vector2(0.5f, 0.5f);
        popupRootRect.anchoredPosition = Vector2.zero;
        popupRootRect.sizeDelta = Vector2.zero;

        Image dimImage = actionPopupObject.AddComponent<Image>();
        dimImage.color = new Color(0.03f, 0.025f, 0.018f, 0.48f);

        GameObject cardObject = new GameObject("ActionCard");
        cardObject.transform.SetParent(actionPopupObject.transform, false);

        RectTransform cardRect = cardObject.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(360f, 330f);

        Image cardImage = cardObject.AddComponent<Image>();
        cardImage.color = new Color(0.92f, 0.82f, 0.62f, 0.96f);

        CreateDecorPanel("ActionCardInner", cardObject.transform, Vector2.zero, new Vector2(330f, 300f), new Color(0.18f, 0.11f, 0.06f, 0.24f));

        actionTitleText = CreateText("ActionTitle", cardObject.transform, new Vector2(0f, 126f), new Vector2(280f, 4f), 1, TextAnchor.MiddleCenter);
        actionTitleText.color = new Color(0.12f, 0.07f, 0.035f, 1f);
        actionTitleText.enabled = false;

        actionAnimationImage = CreateIconImage("ActionAnimation", cardObject.transform, new Vector2(0f, 42f), new Vector2(230f, 230f));

        actionHintText = CreateText("ActionHint", cardObject.transform, new Vector2(0f, -124f), new Vector2(300f, 28f), 17, TextAnchor.MiddleCenter);
        actionHintText.color = new Color(0.12f, 0.07f, 0.035f, 1f);

        actionPopupObject.SetActive(false);
    }

    private Text CreateCropButton(string objectName, string label, FarmCropKind cropKind, Vector2 position, UnityEngine.Events.UnityAction action, Transform parent)
    {
        Button button = CreateButton(objectName, parent, position, new Vector2(136f, 64f), action);
        FarmCropDefinition crop = farmController.GetCropDefinition(GetCropId(cropKind));
        Image iconImage = CreateIconImage("CropIcon", button.transform, new Vector2(-46f, 14f), new Vector2(28f, 28f));
        iconImage.sprite = GetCropSprite(crop, FarmPlotPhase.Seed);
        iconImage.enabled = iconImage.sprite != null;

        Text buttonText = CreateText("Label", button.transform, new Vector2(8f, 0f), new Vector2(112f, 58f), 13, TextAnchor.MiddleCenter);
        buttonText.text = label;
        buttonText.color = new Color(0.07f, 0.04f, 0.02f, 1f);
        return buttonText;
    }

    private static string GetSeedDisplayName(FarmCropKind cropKind)
    {
        switch (cropKind)
        {
            case FarmCropKind.Wheat:
                return "麦の種";
            case FarmCropKind.CoffeeBean:
                return "コーヒーの種";
            case FarmCropKind.Sugarcane:
                return "砂糖きびの種";
            default:
                return "種";
        }
    }

    private Button CreateButton(string objectName, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = size;

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.86f, 0.78f, 0.58f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();

        if (action != null)
        {
            button.onClick.AddListener(action);
        }

        return button;
    }

    private Button CreateTransparentPlotButton(string objectName, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        Button button = CreateButton(objectName, parent, position, size, action);
        button.transition = Selectable.Transition.None;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
        }

        return button;
    }

    private void CreateButtonWithLabel(string objectName, string label, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        Button button = CreateButton(objectName, parent, position, size, action);
        Text buttonText = CreateText("Label", button.transform, Vector2.zero, new Vector2(size.x - 10f, size.y - 6f), 18, TextAnchor.MiddleCenter);
        buttonText.text = label;
        buttonText.color = new Color(0.07f, 0.04f, 0.02f, 1f);
    }

    private Image CreateDecorPanel(string objectName, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(objectName);
        panel.transform.SetParent(parent, false);

        RectTransform rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private Image CreateIconImage(string objectName, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject iconObject = new GameObject(objectName);
        iconObject.transform.SetParent(parent, false);

        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = position;
        iconRect.sizeDelta = size;

        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.enabled = false;
        return iconImage;
    }

    private Text CreateText(string objectName, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = position;
        textRect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.text = objectName;
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.font = GetUiFont();
        text.color = Color.white;
        return text;
    }

    private Font GetUiFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Meiryo", "Arial" }, 18);
        }

        return font;
    }

    private FarmController ResolveFarmController()
    {
        if (farmController != null)
        {
            farmController.EnsureMinimumPlotCount(MinimumFarmPlotCount);
            return farmController;
        }

        farmController = GetComponent<FarmController>();

        if (farmController == null)
        {
            farmController = gameObject.AddComponent<FarmController>();
        }

        farmController.EnsureMinimumPlotCount(MinimumFarmPlotCount);
        return farmController;
    }

    private static Vector2 GetFarmPlotPosition(int plotIndex)
    {
        if (plotIndex >= 0 && plotIndex < FarmPlotPositions.Length)
        {
            return FarmPlotPositions[plotIndex];
        }

        int extraIndex = Mathf.Max(0, plotIndex - FarmPlotPositions.Length);
        int row = extraIndex / 3;
        int column = extraIndex % 3;
        return new Vector2(-222f + column * 222f, -228f - row * 110f);
    }

    private ResourceInventory ResolveResourceInventory()
    {
        if (resourceInventory != null)
        {
            return resourceInventory;
        }

        resourceInventory = ResourceInventory.Instance;

        if (resourceInventory == null)
        {
            resourceInventory = FindAnyObjectByType<ResourceInventory>();
        }

        if (resourceInventory == null)
        {
            GameObject inventoryObject = new GameObject("ResourceInventory");
            resourceInventory = inventoryObject.AddComponent<ResourceInventory>();
        }

        return resourceInventory;
    }

    private Sprite ResolveFarmMarkerSprite()
    {
        if (farmMarkerSprite != null)
        {
            return farmMarkerSprite;
        }

        if (loadedFarmMarkerSprite == null)
        {
            loadedFarmMarkerSprite = LoadSpriteFromAssetPath(farmMarkerSpritePath, "RuntimeFarmMarkerSprite", true);
        }

        return loadedFarmMarkerSprite;
    }

    private Sprite ResolveFarmBackgroundSprite()
    {
        if (farmBackgroundSprite != null)
        {
            return farmBackgroundSprite;
        }

        if (loadedFarmBackgroundSprite == null)
        {
            loadedFarmBackgroundSprite = LoadSpriteFromAssetPath(farmBackgroundPath, "RuntimeFarmBackgroundSprite", false);
        }

        return loadedFarmBackgroundSprite;
    }

    private void SetPlotCropIcon(int plotIndex, Sprite sprite, FarmCropDefinition crop, bool visible)
    {
        if (plotCropImages == null || plotIndex < 0 || plotIndex >= plotCropImages.Length || plotCropImages[plotIndex] == null)
        {
            return;
        }

        Image cropImage = plotCropImages[plotIndex];
        ApplyPlotCropLayout(cropImage.rectTransform, crop);
        cropImage.sprite = sprite;
        cropImage.enabled = visible && sprite != null;
    }

    private void ApplyPlotCropLayout(RectTransform cropRect, FarmCropDefinition crop)
    {
        if (cropRect == null)
        {
            return;
        }

        Vector2 position = new Vector2(0f, -22f);
        Vector2 size = new Vector2(98f, 98f);

        if (crop != null)
        {
            switch (crop.CropId)
            {
                case "wheat":
                    position = new Vector2(8f, -22f);
                    size = new Vector2(100f, 100f);
                    break;
                case "coffee_bean":
                    position = new Vector2(0f, -22f);
                    size = new Vector2(102f, 102f);
                    break;
                case "sugarcane":
                    position = new Vector2(-8f, -22f);
                    size = new Vector2(106f, 106f);
                    break;
            }
        }

        cropRect.pivot = new Vector2(0.5f, 0f);
        cropRect.anchoredPosition = position;
        cropRect.sizeDelta = size;
    }

    private void SetPlotGrowthBar(int plotIndex, float progress, bool visible, bool ready)
    {
        if (plotProgressFillImages == null || plotIndex < 0 || plotIndex >= plotProgressFillImages.Length || plotProgressFillImages[plotIndex] == null)
        {
            return;
        }

        if (plotProgressTrackImages != null && plotIndex < plotProgressTrackImages.Length && plotProgressTrackImages[plotIndex] != null)
        {
            plotProgressTrackImages[plotIndex].enabled = visible;
        }

        Image fillImage = plotProgressFillImages[plotIndex];
        fillImage.enabled = visible;

        RectTransform fillRect = fillImage.rectTransform;
        float width = visible ? Mathf.Lerp(4f, 112f, Mathf.Clamp01(progress)) : 0f;
        fillRect.sizeDelta = new Vector2(width, 8f);
        fillImage.color = ready
            ? new Color(1f, 0.82f, 0.22f, 0.96f)
            : new Color(0.55f, 0.86f, 0.36f, 0.9f);
    }

    private static string GetIngredientDisplayName(string ingredientId)
    {
        switch (ingredientId)
        {
            case ResourceInventory.FlourId:
                return "小麦粉";
            case ResourceInventory.CoffeeBeanId:
                return "コーヒー豆";
            case ResourceInventory.SugarId:
                return "砂糖";
            default:
                return string.IsNullOrEmpty(ingredientId) ? "素材" : ingredientId;
        }
    }

    private Sprite GetCropSprite(FarmCropDefinition crop, FarmPlotPhase phase)
    {
        if (crop == null)
        {
            return null;
        }

        string state = phase == FarmPlotPhase.Seed ? "seed" : "growing";
        string cacheKey = $"{crop.CropId}.{state}";

        if (cropSpriteCache.TryGetValue(cacheKey, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        string path = GetCropSpritePath(crop.CropId, state);
        Sprite sprite = LoadSpriteFromAssetPath(path, $"RuntimeFarmCrop_{cacheKey}", true);
        cropSpriteCache[cacheKey] = sprite;
        return sprite;
    }

    private string GetCropSpritePath(string cropId, string state)
    {
        switch (cropId)
        {
            case "wheat":
                return state == "seed"
                    ? "Art/farm_icon/wheat_seed.png"
                    : "Art/farm_icon/wheat_growing.png";
            case "coffee_bean":
                return state == "seed"
                    ? "Art/farm_icon/coffeebean_seed.png"
                    : "Art/farm_icon/coffeebean_growing.png";
            case "sugarcane":
                return state == "seed"
                    ? "Art/farm_icon/sugarcane_seed.png"
                    : "Art/farm_icon/sugarcane_growing.png";
            default:
                return string.Empty;
        }
    }

    private void PlayFarmActionAnimation(bool harvest)
    {
        if (actionPopupObject == null || actionAnimationImage == null || panelObject == null || !panelObject.activeInHierarchy)
        {
            return;
        }

        StopFarmActionAnimation();

        actionAnimationRoutine = StartCoroutine(PlayFarmActionAnimationRoutine(harvest));
    }

    private IEnumerator PlayFarmActionAnimationRoutine(bool harvest)
    {
        string[] framePaths = harvest
            ? GetHarvestFramePaths()
            : GetPlantingFramePaths();

        actionAnimationPlaying = true;
        actionPopupObject.SetActive(true);
        actionPopupObject.transform.SetAsLastSibling();
        actionTitleText.text = string.Empty;
        actionHintText.text = harvest ? "収穫中..." : "種まき中...";
        actionAnimationImage.enabled = true;
        actionAnimationImage.color = Color.white;

        for (int i = 0; i < framePaths.Length; i++)
        {
            Sprite frame = LoadSpriteFromAssetPath(framePaths[i], $"RuntimeFarmAction_{i:00}", true, true);

            if (frame != null)
            {
                actionAnimationImage.sprite = frame;
            }

            yield return new WaitForSecondsRealtime(0.28f);
        }

        yield return new WaitForSecondsRealtime(0.45f);
        actionAnimationImage.enabled = false;
        actionPopupObject.SetActive(false);
        actionAnimationPlaying = false;
        actionAnimationRoutine = null;
    }

    private void StopFarmActionAnimation()
    {
        if (actionAnimationRoutine != null)
        {
            StopCoroutine(actionAnimationRoutine);
            actionAnimationRoutine = null;
        }

        actionAnimationPlaying = false;

        if (actionAnimationImage != null)
        {
            actionAnimationImage.enabled = false;
        }

        if (actionPopupObject != null)
        {
            actionPopupObject.SetActive(false);
        }
    }

    private static string[] GetPlantingFramePaths()
    {
        return new[]
        {
            "Art/farm_icon/yashrine_farm_animation_frames/planting/planting_01_ready.png",
            "Art/farm_icon/yashrine_farm_animation_frames/planting/planting_02_raise_shovel.png",
            "Art/farm_icon/yashrine_farm_animation_frames/planting/planting_03_start_dig.png",
            "Art/farm_icon/yashrine_farm_animation_frames/planting/planting_04_shovel_down.png",
            "Art/farm_icon/yashrine_farm_animation_frames/planting/planting_05_turn_soil.png",
            "Art/farm_icon/yashrine_farm_animation_frames/planting/planting_06_place_seed.png",
            "Art/farm_icon/yashrine_farm_animation_frames/planting/planting_07_pat_soil.png",
            "Art/farm_icon/yashrine_farm_animation_frames/planting/planting_08_finish.png"
        };
    }

    private static string[] GetHarvestFramePaths()
    {
        return new[]
        {
            "Art/farm_icon/yashirine_harvest/harvest_daikon_01_ready.png",
            "Art/farm_icon/yashirine_harvest/harvest_daikon_02_grab_leaves.png",
            "Art/farm_icon/yashirine_harvest/harvest_daikon_03_first_pull.png",
            "Art/farm_icon/yashirine_harvest/harvest_daikon_04_hard_pull.png",
            "Art/farm_icon/yashirine_harvest/harvest_daikon_05_sweating_pull.png",
            "Art/farm_icon/yashirine_harvest/harvest_daikon_06_max_effort.png",
            "Art/farm_icon/yashirine_harvest/harvest_daikon_07_pop_out.png",
            "Art/farm_icon/yashirine_harvest/harvest_daikon_08_victory.png"
        };
    }

    private static string GetCropId(FarmCropKind cropKind)
    {
        switch (cropKind)
        {
            case FarmCropKind.Wheat:
                return "wheat";
            case FarmCropKind.CoffeeBean:
                return "coffee_bean";
            case FarmCropKind.Sugarcane:
                return "sugarcane";
            default:
                return string.Empty;
        }
    }

    private Sprite LoadSpriteFromAssetPath(
        string assetRelativePath,
        string spriteName,
        bool cleanEdgeWhiteBackground,
        bool cleanSmallDetachedArtifacts = false)
    {
        if (string.IsNullOrWhiteSpace(assetRelativePath))
        {
            return null;
        }

        string imagePath = Path.Combine(Application.dataPath, assetRelativePath);

        if (!File.Exists(imagePath))
        {
            return null;
        }

        byte[] imageBytes = File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!texture.LoadImage(imageBytes))
        {
            Destroy(texture);
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        if (cleanEdgeWhiteBackground)
        {
            RemoveNearWhiteEdgeBackground(texture);
            if (cleanSmallDetachedArtifacts)
            {
                RemoveSmallDetachedArtifacts(texture);
            }

            texture = CropToVisibleAlpha(texture);
        }

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = spriteName;
        return sprite;
    }

    private void RemoveNearWhiteEdgeBackground(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();
        bool[] visited = new bool[pixels.Length];
        Queue<int> queue = new Queue<int>();

        void TryEnqueue(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return;
            }

            int index = y * width + x;

            if (visited[index] || !IsNearWhiteBackground(pixels[index]))
            {
                return;
            }

            visited[index] = true;
            queue.Enqueue(index);
        }

        for (int x = 0; x < width; x++)
        {
            TryEnqueue(x, 0);
            TryEnqueue(x, height - 1);
        }

        for (int y = 0; y < height; y++)
        {
            TryEnqueue(0, y);
            TryEnqueue(width - 1, y);
        }

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            int x = index % width;
            int y = index / width;
            pixels[index].a = 0;

            TryEnqueue(x + 1, y);
            TryEnqueue(x - 1, y);
            TryEnqueue(x, y + 1);
            TryEnqueue(x, y - 1);
        }

        texture.SetPixels32(pixels);
        texture.Apply();
    }

    private static bool IsNearWhiteBackground(Color32 color)
    {
        if (color.a == 0)
        {
            return false;
        }

        int max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        int min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        float luminance = (0.2126f * color.r) + (0.7152f * color.g) + (0.0722f * color.b);
        return (min >= 145 && max - min <= 72) || (luminance >= 205f && max - min <= 95);
    }

    private void RemoveSmallDetachedArtifacts(Texture2D texture)
    {
        const byte VisibleAlphaThreshold = 12;
        const int SmallArtifactPixelLimit = 48;
        const int MainBoundsPadding = 24;

        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();
        bool[] visited = new bool[pixels.Length];
        List<AlphaComponent> components = new List<AlphaComponent>();
        Queue<int> queue = new Queue<int>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int startIndex = y * width + x;

                if (visited[startIndex] || pixels[startIndex].a <= VisibleAlphaThreshold)
                {
                    continue;
                }

                AlphaComponent component = new AlphaComponent(x, y);
                visited[startIndex] = true;
                queue.Enqueue(startIndex);

                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    int currentX = index % width;
                    int currentY = index / width;
                    component.AddPixel(index, currentX, currentY, pixels[index]);

                    TryQueueVisibleNeighbor(currentX + 1, currentY);
                    TryQueueVisibleNeighbor(currentX - 1, currentY);
                    TryQueueVisibleNeighbor(currentX, currentY + 1);
                    TryQueueVisibleNeighbor(currentX, currentY - 1);
                }

                components.Add(component);
            }
        }

        if (components.Count <= 1)
        {
            return;
        }

        AlphaComponent mainComponent = components[0];
        for (int i = 1; i < components.Count; i++)
        {
            if (components[i].Pixels.Count > mainComponent.Pixels.Count)
            {
                mainComponent = components[i];
            }
        }

        bool removedAny = false;
        for (int i = 0; i < components.Count; i++)
        {
            AlphaComponent component = components[i];

            if (component == mainComponent ||
                component.Pixels.Count > SmallArtifactPixelLimit ||
                component.IsBlueAccent ||
                component.IsInsidePaddedBounds(mainComponent, MainBoundsPadding))
            {
                continue;
            }

            for (int pixelIndex = 0; pixelIndex < component.Pixels.Count; pixelIndex++)
            {
                Color32 clear = pixels[component.Pixels[pixelIndex]];
                clear.a = 0;
                pixels[component.Pixels[pixelIndex]] = clear;
            }

            removedAny = true;
        }

        if (removedAny)
        {
            texture.SetPixels32(pixels);
            texture.Apply();
        }

        void TryQueueVisibleNeighbor(int neighborX, int neighborY)
        {
            if (neighborX < 0 || neighborX >= width || neighborY < 0 || neighborY >= height)
            {
                return;
            }

            int neighborIndex = neighborY * width + neighborX;

            if (visited[neighborIndex] || pixels[neighborIndex].a <= VisibleAlphaThreshold)
            {
                return;
            }

            visited[neighborIndex] = true;
            queue.Enqueue(neighborIndex);
        }
    }

    private sealed class AlphaComponent
    {
        public readonly List<int> Pixels = new List<int>();
        public int MinX { get; private set; }
        public int MaxX { get; private set; }
        public int MinY { get; private set; }
        public int MaxY { get; private set; }
        public bool IsBlueAccent { get; private set; }

        private int bluePixelCount;

        public AlphaComponent(int x, int y)
        {
            MinX = x;
            MaxX = x;
            MinY = y;
            MaxY = y;
        }

        public void AddPixel(int index, int x, int y, Color32 color)
        {
            Pixels.Add(index);
            MinX = Mathf.Min(MinX, x);
            MaxX = Mathf.Max(MaxX, x);
            MinY = Mathf.Min(MinY, y);
            MaxY = Mathf.Max(MaxY, y);

            if (color.b > 95 && color.b > color.r + 25 && color.b > color.g + 10)
            {
                bluePixelCount++;
                IsBlueAccent = bluePixelCount >= Mathf.Max(1, Pixels.Count / 3);
            }
        }

        public bool IsInsidePaddedBounds(AlphaComponent other, int padding)
        {
            return MinX >= other.MinX - padding &&
                   MaxX <= other.MaxX + padding &&
                   MinY >= other.MinY - padding &&
                   MaxY <= other.MaxY + padding;
        }
    }

    private Texture2D CropToVisibleAlpha(Texture2D source)
    {
        Color32[] pixels = source.GetPixels32();
        int width = source.width;
        int height = source.height;
        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a <= 12)
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return source;
        }

        int croppedWidth = maxX - minX + 1;
        int croppedHeight = maxY - minY + 1;
        Texture2D cropped = new Texture2D(croppedWidth, croppedHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] croppedPixels = new Color32[croppedWidth * croppedHeight];

        for (int y = 0; y < croppedHeight; y++)
        {
            for (int x = 0; x < croppedWidth; x++)
            {
                croppedPixels[y * croppedWidth + x] = pixels[(minY + y) * width + minX + x];
            }
        }

        cropped.SetPixels32(croppedPixels);
        cropped.Apply();
        Destroy(source);
        return cropped;
    }

    private Sprite CreateFallbackSquareSprite()
    {
        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
        sprite.name = "RuntimeFarmFallbackSquare";
        return sprite;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }
}
