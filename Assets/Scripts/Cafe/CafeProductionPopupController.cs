using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum CafeProductionMachineType
{
    CoffeeMachine,
    BakerMachine
}

public class CafeProductionPopupController : MonoBehaviour
{
    private static readonly Color PopupBorderColor = new Color(0.36f, 0.22f, 0.12f, 1f);
    private static readonly Color PopupInnerLineColor = new Color(0.94f, 0.88f, 0.76f, 1f);
    [Header("Popup Roots")]
    [SerializeField] private GameObject productionPopupRoot;
    [SerializeField] private GameObject recipePanelRoot;
    [SerializeField] private GameObject progressRoot;
    [SerializeField] private GameObject completeCheckRoot;

    [Header("Popup Images")]
    [SerializeField] private Image popupBackgroundImage;
    [SerializeField] private Image recipeBubbleImage;
    [SerializeField] private Image progressFrameImage;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private Image coffeeMachineImage;
    [SerializeField] private Image bakerMachineImage;
    [SerializeField] private Image inariCoffeeIconImage;
    [SerializeField] private Image kitsunebiLatteIconImage;
    [SerializeField] private Image yozakuraCakeIconImage;

    [Header("Buttons")]
    [SerializeField] private Button coffeeMachineButton;
    [SerializeField] private Button bakerMachineButton;
    [SerializeField] private Button inariCoffeeRecipeButton;
    [SerializeField] private Button kitsunebiLatteRecipeButton;
    [SerializeField] private Button yozakuraCakeRecipeButton;
    [SerializeField] private Button closeButton;

    [Header("Optional Text")]
    [SerializeField] private Text statusText;
    [SerializeField] private Text inariCoffeeRecipeLabel;
    [SerializeField] private Text kitsunebiLatteRecipeLabel;
    [SerializeField] private Text yozakuraCakeRecipeLabel;

    [Header("Timing")]
    [SerializeField] private float completeHoldSeconds = 1f;
    [SerializeField] private int progressSegmentCount = 4;
    [SerializeField] private bool closePopupAfterProductionComplete = true;

    [Header("Fallback Asset Paths")]
    [SerializeField] private string popupBackgroundPath = "Assets/Art/Backgrounds/cafe_front.png";
    [SerializeField] private string recipeBubblePath = "Assets/Art/cafe_icon/speak_bubble.png";
    [SerializeField] private string progressFramePath = "Assets/Art/cafe_icon/progress_bar.png";
    [SerializeField] private string coffeeMachinePath = "Assets/Art/cafe_icon/coffe_mechine_working_cutout.png";
    [SerializeField] private string bakerMachinePath = "Assets/Art/cafe_icon/baker_mechine_working_cutout.png";
    [SerializeField] private string inariCoffeeIconPath = "Assets/Art/cafe_icon/menu_icon/inari_coffe.png";
    [SerializeField] private string kitsunebiLatteIconPath = "Assets/Art/cafe_icon/menu_icon/kitsunebi_latte.png";
    [SerializeField] private string yozakuraCakeIconPath = "Assets/Art/cafe_icon/menu_icon/night_sakura_cake.png";

    private readonly List<RecipeConfig> recipes = new List<RecipeConfig>
    {
        new RecipeConfig("InariCoffee", "inari_coffee", CafeProductionMachineType.CoffeeMachine, 3f, ResourceInventory.InariCoffeeId),
        new RecipeConfig("KitsunebiLatte", "kitsunebi_latte", CafeProductionMachineType.CoffeeMachine, 4f, ResourceInventory.KitsunebiLatteId),
        new RecipeConfig("YozakuraCake", "yozakura_cake", CafeProductionMachineType.BakerMachine, 5f, ResourceInventory.YozakuraCakeId)
    };

    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private CafeOperationController operationController;
    private Coroutine productionCoroutine;
    private CafeProductionMachineType selectedMachineType = CafeProductionMachineType.CoffeeMachine;
    private Vector3 coffeeMachineBaseScale = Vector3.one;
    private Vector3 bakerMachineBaseScale = Vector3.one;
    private bool listenersWired;
    private bool fallbackUiCreated;
    private readonly List<Image> progressSegmentImages = new List<Image>();
    private static readonly Vector2 ProgressFrameSize = new Vector2(180f, 24f);
    private static readonly Vector2 ProgressFillSize = new Vector2(142f, 12f);
    private static readonly Vector2 ProgressFillOffset = new Vector2(-1.5f, -1f);
    private static readonly Vector2 CoffeeProgressPosition = new Vector2(-330f, 140f);
    private static readonly Vector2 BakerProgressPosition = new Vector2(18f, 202f);

    public bool IsProducing => productionCoroutine != null;

    public void Initialize(Transform canvasRoot, CafeOperationController controller)
    {
        operationController = controller;

        if (productionPopupRoot == null && canvasRoot != null)
        {
            CreateFallbackUi(canvasRoot);
        }

        EnsureProgressUiBindings();
        ApplyFallbackSprites();
        WireButtonListeners();
        HideAll();
    }

    private void Awake()
    {
        EnsureProgressUiBindings();
        ApplyFallbackSprites();
        WireButtonListeners();
        HideAll();
    }

    private void OnDestroy()
    {
        UnwireButtonListeners();
    }

    public void OpenPopup()
    {
        if (!ValidateBindings())
        {
            return;
        }

        EnsureProgressUiBindings();
        ApplyFallbackSprites();
        SetRootActive(productionPopupRoot, true);
        SetRootActive(recipePanelRoot, false);

        if (productionCoroutine != null)
        {
            SetRootActive(progressRoot, true);
            SetRootActive(completeCheckRoot, false);
            SetMachineButtonsInteractable(false);
            SetStatus("Producing...");
            return;
        }

        SetRootActive(progressRoot, false);
        SetRootActive(completeCheckRoot, false);
        SetProgress(0f);
        SetMachineButtonsInteractable(true);
        SetStatus("Select a machine.");
    }

    public void ClosePopup()
    {
        SetRootActive(productionPopupRoot, false);
        SetRootActive(recipePanelRoot, false);

        if (productionCoroutine == null)
        {
            SetRootActive(progressRoot, false);
            SetRootActive(completeCheckRoot, false);
            SetProgress(0f);
            SetMachineButtonsInteractable(true);
        }
    }

    public void SelectMachine(CafeProductionMachineType machineType)
    {
        if (productionCoroutine != null)
        {
            return;
        }

        selectedMachineType = machineType;
        SetRootActive(productionPopupRoot, true);
        SetRootActive(recipePanelRoot, true);
        SetRootActive(progressRoot, false);
        SetRootActive(completeCheckRoot, false);
        SetProgress(0f);
        RefreshRecipePanel();
        SetStatus(machineType == CafeProductionMachineType.CoffeeMachine
            ? "Choose a coffee recipe."
            : "Choose a baked recipe.");
    }

    public void StartRecipe(string recipeId)
    {
        if (productionCoroutine != null)
        {
            return;
        }

        RecipeConfig recipe = FindRecipe(recipeId);

        if (recipe == null)
        {
            SetStatus($"Recipe missing: {recipeId}");
            return;
        }

        if (recipe.MachineType != selectedMachineType)
        {
            SetStatus("This recipe uses another machine.");
            return;
        }

        SetRootActive(recipePanelRoot, false);

        if (!TryFindMenuIndex(recipe.MenuId, out int menuIndex))
        {
            SetStatus($"Menu missing: {recipe.DisplayName}");
            return;
        }

        CafeMenuItem menuItem = null;
        string resultMessage = "Production controller is missing.";
        bool started = operationController != null
            && operationController.TryStartProduction(menuIndex, out menuItem, out resultMessage);
        SetStatus(resultMessage);

        if (!started || menuItem == null)
        {
            SetRootActive(progressRoot, false);
            SetRootActive(completeCheckRoot, false);
            SetProgress(0f);
            SetMachineButtonsInteractable(true);
            return;
        }

        productionCoroutine = StartCoroutine(RunProduction(recipe, menuItem));
    }

    private IEnumerator RunProduction(RecipeConfig recipe, CafeMenuItem menuItem)
    {
        EnsureProgressUiBindings();
        SetMachineButtonsInteractable(false);
        PositionProgressRoot(recipe.MachineType);
        SetMachineWorkingScale(recipe.MachineType, 0f);
        SetRootActive(progressRoot, true);
        SetRootActive(completeCheckRoot, false);
        SetProgress(0f);

        float speedMultiplier = operationController != null
            ? Mathf.Max(0.01f, operationController.ProductionSpeedMultiplier)
            : 1f;
        float duration = Mathf.Max(0.1f, recipe.CraftSeconds / speedMultiplier);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetProgress(progress, elapsed);
            SetMachineWorkingScale(recipe.MachineType, elapsed);
            SetStatus($"Producing {recipe.DisplayName} {Mathf.RoundToInt(progress * 100f)}%");
            yield return null;
        }

        SetProgress(1f, duration);
        ResetMachineScale(recipe.MachineType);
        SetRootActive(completeCheckRoot, true);
        if (completeCheckRoot != null)
        {
            completeCheckRoot.transform.localScale = Vector3.one * 0.72f;
        }

        if (operationController != null)
        {
            operationController.CompleteProduction(menuItem, out string resultMessage);
            SetStatus(resultMessage);
            operationController.SetCafeFeedbackMessage("\u5B8C\u6210\u3057\u307E\u3057\u305F\u3002\u6765\u8A2A\u8005\u306E\u6CE8\u6587\u3075\u304D\u3060\u3057\u3092\u30AF\u30EA\u30C3\u30AF\u3057\u3066\u63D0\u4F9B\u3067\u304D\u307E\u3059\u3002");
        }
        else
        {
            ResourceInventory inventory = ResolveResourceInventory();

            if (inventory != null)
            {
                inventory.AddFinishedItem(recipe.OutputItemId, 1);
                SetStatus($"{recipe.DisplayName} complete.");
            }
        }

        float holdSeconds = Mathf.Max(0f, completeHoldSeconds);
        float pulseSeconds = Mathf.Min(0.35f, holdSeconds);
        if (pulseSeconds > 0f)
        {
            yield return AnimateCompleteCheck(pulseSeconds);
        }

        float remainingHoldSeconds = holdSeconds - pulseSeconds;
        if (remainingHoldSeconds > 0f)
        {
            yield return new WaitForSeconds(remainingHoldSeconds);
        }

        SetRootActive(progressRoot, false);
        SetRootActive(completeCheckRoot, false);
        if (completeCheckRoot != null)
        {
            completeCheckRoot.transform.localScale = Vector3.one;
        }
        SetProgress(0f);
        SetMachineButtonsInteractable(true);
        ResetMachineScale(recipe.MachineType);
        productionCoroutine = null;

        if (closePopupAfterProductionComplete)
        {
            SetRootActive(recipePanelRoot, false);
            SetRootActive(productionPopupRoot, false);
        }
    }

    private void CreateFallbackUi(Transform canvasRoot)
    {
        if (fallbackUiCreated)
        {
            return;
        }

        fallbackUiCreated = true;
        productionPopupRoot = CreateRectObject("ProductionPopupRoot", canvasRoot, Vector2.zero, new Vector2(980f, 560f));
        Image frameImage = productionPopupRoot.AddComponent<Image>();
        frameImage.color = new Color(0f, 0f, 0f, 0f);

        GameObject backgroundObject = CreateRectObject("CafeFrontBackground", productionPopupRoot.transform, Vector2.zero, new Vector2(958f, 538f));
        popupBackgroundImage = backgroundObject.AddComponent<Image>();
        popupBackgroundImage.color = Color.white;
        popupBackgroundImage.preserveAspect = true;

        CreatePopupFrame(productionPopupRoot.transform, new Vector2(980f, 560f));

        coffeeMachineButton = CreateImageButton(
            "CoffeeMachineButton",
            productionPopupRoot.transform,
            new Vector2(-330f, 42f),
            new Vector2(118f, 136f),
            out coffeeMachineImage);
        coffeeMachineBaseScale = coffeeMachineButton.transform.localScale;

        bakerMachineButton = CreateImageButton(
            "BakerMachineButton",
            productionPopupRoot.transform,
            new Vector2(18f, 92f),
            new Vector2(176f, 192f),
            out bakerMachineImage);
        bakerMachineBaseScale = bakerMachineButton.transform.localScale;

        recipePanelRoot = CreateRectObject("RecipePanelRoot", productionPopupRoot.transform, new Vector2(-330f, 168f), new Vector2(160f, 84f));
        recipeBubbleImage = recipePanelRoot.AddComponent<Image>();
        recipeBubbleImage.color = Color.white;
        recipeBubbleImage.preserveAspect = true;

        inariCoffeeRecipeButton = CreateRecipeButton("InariCoffeeRecipeButton", recipePanelRoot.transform, new Vector2(-34f, 5f), out inariCoffeeIconImage, out inariCoffeeRecipeLabel);
        kitsunebiLatteRecipeButton = CreateRecipeButton("KitsunebiLatteRecipeButton", recipePanelRoot.transform, new Vector2(34f, 5f), out kitsunebiLatteIconImage, out kitsunebiLatteRecipeLabel);
        yozakuraCakeRecipeButton = CreateRecipeButton("YozakuraCakeRecipeButton", recipePanelRoot.transform, new Vector2(0f, 8f), out yozakuraCakeIconImage, out yozakuraCakeRecipeLabel);

        progressRoot = CreateRectObject("ProgressRoot", productionPopupRoot.transform, CoffeeProgressPosition, new Vector2(190f, 48f));

        GameObject fillObject = CreateRectObject("ProgressFill", progressRoot.transform, ProgressFillOffset, ProgressFillSize);
        progressFillImage = fillObject.AddComponent<Image>();
        progressFillImage.color = new Color(1f, 0.82f, 0.28f, 0.98f);
        progressFillImage.preserveAspect = false;
        progressFillImage.raycastTarget = false;

        CreateProgressSegments(progressRoot.transform);

        GameObject frameObject = CreateRectObject("ProgressFrame", progressRoot.transform, Vector2.zero, ProgressFrameSize);
        progressFrameImage = frameObject.AddComponent<Image>();
        progressFrameImage.color = Color.white;
        progressFrameImage.preserveAspect = false;
        progressFrameImage.raycastTarget = false;

        completeCheckRoot = CreateRectObject("CompleteCheckRoot", progressRoot.transform, new Vector2(82f, 2f), new Vector2(28f, 28f));
        Text checkText = completeCheckRoot.AddComponent<Text>();
        checkText.text = "\u2713";
        checkText.alignment = TextAnchor.MiddleCenter;
        checkText.fontSize = 24;
        checkText.font = GetUiFont();
        checkText.color = new Color(0.32f, 0.88f, 0.22f, 1f);

        RefreshProgressLayerOrder();

        closeButton = CreateTextButton("CloseButton", productionPopupRoot.transform, new Vector2(390f, -232f), new Vector2(120f, 42f), "Close");
    }

    private void EnsureProgressUiBindings()
    {
        if (productionPopupRoot == null)
        {
            return;
        }

        if (progressRoot == null)
        {
            Transform existingProgressRoot = productionPopupRoot.transform.Find("ProgressRoot");
            progressRoot = existingProgressRoot != null
                ? existingProgressRoot.gameObject
                : CreateRectObject("ProgressRoot", productionPopupRoot.transform, CoffeeProgressPosition, new Vector2(190f, 48f));
        }

        if (progressRoot == null)
        {
            return;
        }

        RectTransform progressRectTransform = progressRoot.GetComponent<RectTransform>();
        if (progressRectTransform == null)
        {
            progressRectTransform = progressRoot.AddComponent<RectTransform>();
        }

        progressRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        progressRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        progressRectTransform.pivot = new Vector2(0.5f, 0.5f);
        progressRectTransform.sizeDelta = new Vector2(190f, 48f);

        EnsureProgressImage(
            ref progressFrameImage,
            "ProgressFrame",
            Vector2.zero,
            ProgressFrameSize,
            Color.white);
        EnsureProgressImage(
            ref progressFillImage,
            "ProgressFill",
            ProgressFillOffset,
            ProgressFillSize,
            new Color(1f, 0.74f, 0.24f, 0.96f));

        CreateProgressSegments(progressRoot.transform);
        EnsureCompleteCheckRoot();
        ConfigureProgressVisualGeometry();
        RefreshProgressLayerOrder();
    }

    private void EnsureProgressImage(
        ref Image targetImage,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size,
        Color defaultColor)
    {
        if (progressRoot == null)
        {
            return;
        }

        GameObject imageObject = targetImage != null ? targetImage.gameObject : null;

        if (imageObject == null)
        {
            Transform existingTransform = progressRoot.transform.Find(objectName);
            imageObject = existingTransform != null
                ? existingTransform.gameObject
                : CreateRectObject(objectName, progressRoot.transform, anchoredPosition, size);
        }

        targetImage = imageObject.GetComponent<Image>();
        if (targetImage == null)
        {
            targetImage = imageObject.AddComponent<Image>();
        }

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }

        targetImage.color = defaultColor;
        targetImage.raycastTarget = false;
    }

    private void EnsureCompleteCheckRoot()
    {
        if (progressRoot == null)
        {
            return;
        }

        if (completeCheckRoot == null)
        {
            Transform existingTransform = progressRoot.transform.Find("CompleteCheckRoot");
            completeCheckRoot = existingTransform != null
                ? existingTransform.gameObject
                : CreateRectObject("CompleteCheckRoot", progressRoot.transform, new Vector2(82f, 2f), new Vector2(28f, 28f));
        }

        RectTransform rectTransform = completeCheckRoot.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(82f, 2f);
            rectTransform.sizeDelta = new Vector2(28f, 28f);
        }

        Text checkText = completeCheckRoot.GetComponent<Text>();
        if (checkText == null)
        {
            checkText = completeCheckRoot.AddComponent<Text>();
        }

        checkText.text = "\u2713";
        checkText.alignment = TextAnchor.MiddleCenter;
        checkText.fontSize = 24;
        checkText.font = GetUiFont();
        checkText.color = new Color(0.26f, 0.86f, 0.24f, 1f);
        completeCheckRoot.transform.localScale = Vector3.one;
    }

    private void CreatePopupBorder(Transform parent, Vector2 size, float thickness, float cornerSize, Color color)
    {
        CreateBorderPart("BorderTop", parent, new Vector2(0f, (size.y - thickness) * 0.5f), new Vector2(size.x - cornerSize, thickness), color);
        CreateBorderPart("BorderBottom", parent, new Vector2(0f, -(size.y - thickness) * 0.5f), new Vector2(size.x - cornerSize, thickness), color);
        CreateBorderPart("BorderLeft", parent, new Vector2(-(size.x - thickness) * 0.5f, 0f), new Vector2(thickness, size.y - cornerSize), color);
        CreateBorderPart("BorderRight", parent, new Vector2((size.x - thickness) * 0.5f, 0f), new Vector2(thickness, size.y - cornerSize), color);

        // Corners are intentionally left open so the popup keeps a clean framed-paper look.
    }

    private void CreatePopupFrame(Transform parent, Vector2 size)
    {
        GameObject frameObject = CreateRectObject("PopupRoundedFrame", parent, Vector2.zero, size);
        Image frameImage = frameObject.AddComponent<Image>();
        frameImage.sprite = CreateRoundedFrameSprite(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));
        frameImage.color = Color.white;
        frameImage.preserveAspect = false;
        frameImage.raycastTarget = false;
    }

    private Sprite CreateRoundedFrameSprite(int width, int height)
    {
        const int outerRadius = 28;
        const int brownInset = 14;
        const int whiteInset = 18;
        const int whiteThickness = 4;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[width * height];
        Color32 transparent = new Color32(255, 255, 255, 0);
        Color32 border = ToColor32(PopupBorderColor);
        Color32 innerLine = ToColor32(PopupInnerLineColor);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool inOuter = IsInsideRoundedRect(x, y, width, height, 0, outerRadius);
                bool inBrownHole = IsInsideRoundedRect(x, y, width, height, brownInset, Mathf.Max(1, outerRadius - brownInset));
                bool inWhiteOuter = IsInsideRoundedRect(x, y, width, height, whiteInset, Mathf.Max(1, outerRadius - whiteInset));
                bool inWhiteInner = IsInsideRoundedRect(x, y, width, height, whiteInset + whiteThickness, Mathf.Max(1, outerRadius - whiteInset - whiteThickness));

                pixels[y * width + x] = transparent;

                if (inOuter && !inBrownHole)
                {
                    pixels[y * width + x] = border;
                }

                if (inWhiteOuter && !inWhiteInner)
                {
                    pixels[y * width + x] = innerLine;
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.filterMode = FilterMode.Point;
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "CafeProductionPopupRoundedFrame";
        return sprite;
    }

    private bool IsInsideRoundedRect(int x, int y, int width, int height, int inset, int radius)
    {
        int left = inset;
        int right = width - 1 - inset;
        int bottom = inset;
        int top = height - 1 - inset;

        if (x < left || x > right || y < bottom || y > top)
        {
            return false;
        }

        int innerLeft = left + radius;
        int innerRight = right - radius;
        int innerBottom = bottom + radius;
        int innerTop = top - radius;

        if ((x >= innerLeft && x <= innerRight) || (y >= innerBottom && y <= innerTop))
        {
            return true;
        }

        int centerX = x < innerLeft ? innerLeft : innerRight;
        int centerY = y < innerBottom ? innerBottom : innerTop;
        int dx = x - centerX;
        int dy = y - centerY;
        return dx * dx + dy * dy <= radius * radius;
    }

    private Color32 ToColor32(Color color)
    {
        return new Color32(
            (byte)Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(color.a) * 255f));
    }

    private void CreateBorderPart(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject borderObject = CreateRectObject(objectName, parent, anchoredPosition, size);
        Image image = borderObject.AddComponent<Image>();
        image.color = color;
    }

    private void CreateCornerPart(string objectName, Transform parent, Vector2 anchoredPosition, float size, Color color)
    {
        GameObject cornerObject = CreateRectObject(objectName, parent, anchoredPosition, new Vector2(size, size));
        Image image = cornerObject.AddComponent<Image>();
        image.sprite = CreateCircleSprite();
        image.color = color;
        image.preserveAspect = true;
    }

    private Button CreateImageButton(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, out Image iconImage)
    {
        GameObject buttonObject = CreateRectObject(objectName, parent, anchoredPosition, size);
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0f);

        Button button = buttonObject.AddComponent<Button>();

        GameObject iconObject = CreateRectObject("Icon", buttonObject.transform, Vector2.zero, size * 0.92f);
        iconImage = iconObject.AddComponent<Image>();
        iconImage.color = Color.white;
        iconImage.preserveAspect = true;
        return button;
    }

    private Button CreateRecipeButton(string objectName, Transform parent, Vector2 anchoredPosition, out Image iconImage, out Text labelText)
    {
        GameObject buttonObject = CreateRectObject(objectName, parent, anchoredPosition, new Vector2(54f, 54f));
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0f);

        Button button = buttonObject.AddComponent<Button>();

        GameObject iconObject = CreateRectObject("MenuIcon", buttonObject.transform, Vector2.zero, new Vector2(44f, 44f));
        iconImage = iconObject.AddComponent<Image>();
        iconImage.color = Color.white;
        iconImage.preserveAspect = true;

        labelText = null;
        return button;
    }

    private Button CreateTextButton(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, string label)
    {
        GameObject buttonObject = CreateRectObject(objectName, parent, anchoredPosition, size);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);

        Button button = buttonObject.AddComponent<Button>();
        CreateText("Label", buttonObject.transform, Vector2.zero, size - new Vector2(12f, 8f), label, 18, Color.white);
        return button;
    }

    private void CreateProgressSegments(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        progressSegmentImages.RemoveAll(image => image == null);

        if (progressSegmentImages.Count > 0)
        {
            return;
        }

        int segmentCount = Mathf.Max(4, progressSegmentCount);

        for (int i = 0; i < segmentCount; i++)
        {
            Transform existingSegment = parent.Find($"ProgressSegment_{i:00}");
            if (existingSegment == null)
            {
                continue;
            }

            Image existingImage = existingSegment.GetComponent<Image>();
            if (existingImage != null)
            {
                progressSegmentImages.Add(existingImage);
            }
        }

        if (progressSegmentImages.Count > 0)
        {
            RefreshProgressLayerOrder();
            return;
        }

        float totalWidth = ProgressFillSize.x;
        const float gap = 2f;
        float segmentWidth = (totalWidth - gap * (segmentCount - 1)) / segmentCount;
        float startX = ProgressFillOffset.x - totalWidth * 0.5f + segmentWidth * 0.5f;

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segmentObject = CreateRectObject(
                $"ProgressSegment_{i:00}",
                parent,
                new Vector2(startX + i * (segmentWidth + gap), ProgressFillOffset.y),
                new Vector2(segmentWidth, ProgressFillSize.y));
            Image segmentImage = segmentObject.AddComponent<Image>();
            segmentImage.color = new Color(0.96f, 0.64f, 0.18f, 0f);
            segmentImage.type = Image.Type.Filled;
            segmentImage.fillMethod = Image.FillMethod.Horizontal;
            segmentImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            segmentImage.fillAmount = 0f;
            segmentImage.raycastTarget = false;
            progressSegmentImages.Add(segmentImage);
        }

        RefreshProgressLayerOrder();
    }

    private GameObject CreateRectObject(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject rectObject = new GameObject(objectName);
        rectObject.transform.SetParent(parent, false);
        RectTransform rectTransform = rectObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
        return rectObject;
    }

    private Text CreateText(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, string text, int fontSize, Color color)
    {
        GameObject textObject = CreateRectObject(objectName, parent, anchoredPosition, size);
        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.fontSize = fontSize;
        uiText.font = GetUiFont();
        uiText.color = color;
        return uiText;
    }

    private Sprite CreateCircleSprite()
    {
        const int textureSize = 32;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[textureSize * textureSize];
        Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float radius = textureSize * 0.5f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * textureSize + x] = distance <= radius
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.filterMode = FilterMode.Point;
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
        sprite.name = "CafeProductionPopupCorner";
        return sprite;
    }

    private void ApplyFallbackSprites()
    {
        SetImageSprite(popupBackgroundImage, LoadSprite(popupBackgroundPath, "CafeProductionPopupBackground"));
        SetImageSprite(recipeBubbleImage, LoadRecipeBubbleSprite());
        SetImageSprite(progressFrameImage, LoadProgressFrameSprite());
        SetProgressFillSprite(progressFillImage, LoadProgressFillSprite());
        ConfigureProgressVisualGeometry();
        SetImageSprite(coffeeMachineImage, LoadSprite(coffeeMachinePath, "CafeCoffeeMachine"));
        SetImageSprite(bakerMachineImage, LoadSprite(bakerMachinePath, "CafeBakerMachine"));
        SetImageSprite(inariCoffeeIconImage, LoadSprite(inariCoffeeIconPath, "InariCoffeeIcon"));
        SetImageSprite(kitsunebiLatteIconImage, LoadSprite(kitsunebiLatteIconPath, "KitsunebiLatteIcon"));
        SetImageSprite(yozakuraCakeIconImage, LoadSprite(yozakuraCakeIconPath, "YozakuraCakeIcon"));
    }

    private void SetImageSprite(Image image, Sprite sprite)
    {
        if (image == null || sprite == null)
        {
            return;
        }

        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
    }

    private void SetProgressFillSprite(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        if (sprite != null)
        {
            image.sprite = sprite;
        }

        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.preserveAspect = false;
    }

    private void ConfigureProgressVisualGeometry()
    {
        if (progressRoot != null)
        {
            RectTransform rootRectTransform = progressRoot.GetComponent<RectTransform>();
            if (rootRectTransform != null)
            {
                rootRectTransform.sizeDelta = new Vector2(190f, 48f);
            }
        }

        if (progressFrameImage != null)
        {
            progressFrameImage.preserveAspect = false;
            RectTransform frameRectTransform = progressFrameImage.GetComponent<RectTransform>();
            if (frameRectTransform != null)
            {
                frameRectTransform.anchoredPosition = Vector2.zero;
                frameRectTransform.sizeDelta = ProgressFrameSize;
            }
        }

        if (progressFillImage != null)
        {
            progressFillImage.preserveAspect = false;
            RectTransform fillRectTransform = progressFillImage.GetComponent<RectTransform>();
            if (fillRectTransform != null)
            {
                fillRectTransform.anchoredPosition = ProgressFillOffset;
                fillRectTransform.sizeDelta = ProgressFillSize;
            }
        }
    }

    private Sprite LoadSprite(string assetPath, string runtimeName)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        if (spriteCache.TryGetValue(assetPath, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite loadedSprite = null;

#if UNITY_EDITOR
        loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#endif

        if (loadedSprite == null)
        {
            loadedSprite = LoadRuntimeSprite(assetPath, runtimeName);
        }

        spriteCache[assetPath] = loadedSprite;
        return loadedSprite;
    }

    private Sprite LoadProgressFrameSprite()
    {
        const string cacheKey = "Assets/Art/cafe_icon/progress_bar.png#empty_bar";

        if (spriteCache.TryGetValue(cacheKey, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite progressSprite = LoadSprite("Assets/Art/cafe_icon/progress_bar_cutout.png", "CafeProgressFrame_Cutout");

        if (progressSprite == null)
        {
            progressSprite = LoadRuntimeSpriteRegion(
                progressFramePath,
                "CafeProgressFrame_FromProgressBar",
                new Rect(83f, 831f, 1062f, 139f),
                100f,
                true);
        }

        spriteCache[cacheKey] = progressSprite;
        return progressSprite;
    }

    private Sprite LoadProgressFillSprite()
    {
        const string cacheKey = "Assets/Art/cafe_icon/progress_bar.png#green_fill_inner";

        if (spriteCache.TryGetValue(cacheKey, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite fillSprite = LoadRuntimeSpriteRegion(
            progressFramePath,
            "CafeProgressFill_FromProgressBar",
            new Rect(180f, 187f, 850f, 81f),
            100f,
            false);

        spriteCache[cacheKey] = fillSprite;
        return fillSprite;
    }

    private Sprite LoadRecipeBubbleSprite()
    {
        string cacheKey = $"{recipeBubblePath}#top_left";

        if (spriteCache.TryGetValue(cacheKey, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite bubbleSprite = LoadRuntimeSpriteRegion(
            recipeBubblePath,
            "CafeRecipeBubble_TopLeft",
            new Rect(140f, 604f, 540f, 360f),
            100f,
            false);

        if (bubbleSprite == null)
        {
            bubbleSprite = LoadSprite(recipeBubblePath, "CafeRecipeBubble_Fallback");
        }

        spriteCache[cacheKey] = bubbleSprite;
        return bubbleSprite;
    }

    private Sprite LoadRuntimeSprite(string assetPath, string runtimeName)
    {
        string absolutePath = Path.Combine(Application.dataPath, assetPath.Replace("Assets/", string.Empty));

        if (!File.Exists(absolutePath))
        {
            return null;
        }

        byte[] imageBytes = File.ReadAllBytes(absolutePath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!texture.LoadImage(imageBytes))
        {
            Destroy(texture);
            return null;
        }

        texture.filterMode = FilterMode.Point;
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        sprite.name = runtimeName;
        return sprite;
    }

    private Sprite LoadRuntimeSpriteRegion(string assetPath, string runtimeName, Rect pixelRect, float pixelsPerUnit, bool removeLightChecker)
    {
        string absolutePath = Path.Combine(Application.dataPath, assetPath.Replace("Assets/", string.Empty));

        if (!File.Exists(absolutePath))
        {
            return null;
        }

        byte[] imageBytes = File.ReadAllBytes(absolutePath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!texture.LoadImage(imageBytes))
        {
            Destroy(texture);
            return null;
        }

        Rect safeRect = new Rect(
            Mathf.Clamp(pixelRect.x, 0f, texture.width - 1f),
            Mathf.Clamp(pixelRect.y, 0f, texture.height - 1f),
            Mathf.Clamp(pixelRect.width, 1f, texture.width),
            Mathf.Clamp(pixelRect.height, 1f, texture.height));
        safeRect.width = Mathf.Min(safeRect.width, texture.width - safeRect.x);
        safeRect.height = Mathf.Min(safeRect.height, texture.height - safeRect.y);

        Texture2D croppedTexture = new Texture2D(Mathf.RoundToInt(safeRect.width), Mathf.RoundToInt(safeRect.height), TextureFormat.RGBA32, false);
        Color[] pixels = texture.GetPixels(
            Mathf.RoundToInt(safeRect.x),
            Mathf.RoundToInt(safeRect.y),
            Mathf.RoundToInt(safeRect.width),
            Mathf.RoundToInt(safeRect.height));
        croppedTexture.SetPixels(pixels);

        if (removeLightChecker)
        {
            RemoveLightCheckerPixels(croppedTexture);
        }

        croppedTexture.filterMode = FilterMode.Point;
        croppedTexture.Apply();
        Destroy(texture);

        Sprite sprite = Sprite.Create(
            croppedTexture,
            new Rect(0f, 0f, croppedTexture.width, croppedTexture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);
        sprite.name = runtimeName;
        return sprite;
    }

    private void RemoveLightCheckerPixels(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();

        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 pixel = pixels[i];
            byte max = (byte)Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
            byte min = (byte)Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));

            if (pixel.a > 0 && min > 235 && max - min < 8)
            {
                pixel.a = 0;
                pixels[i] = pixel;
            }
        }

        texture.SetPixels32(pixels);
    }

    private void RefreshRecipePanel()
    {
        bool isCoffeeMachine = selectedMachineType == CafeProductionMachineType.CoffeeMachine;
        SetRootActive(inariCoffeeRecipeButton != null ? inariCoffeeRecipeButton.gameObject : null, isCoffeeMachine);
        SetRootActive(kitsunebiLatteRecipeButton != null ? kitsunebiLatteRecipeButton.gameObject : null, isCoffeeMachine);
        SetRootActive(yozakuraCakeRecipeButton != null ? yozakuraCakeRecipeButton.gameObject : null, !isCoffeeMachine);

        if (recipePanelRoot != null)
        {
            RectTransform rectTransform = recipePanelRoot.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = isCoffeeMachine
                    ? new Vector2(-330f, 168f)
                    : new Vector2(18f, 222f);
            }
        }
    }

    private void PositionProgressRoot(CafeProductionMachineType machineType)
    {
        if (progressRoot == null)
        {
            return;
        }

        RectTransform rectTransform = progressRoot.GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = machineType == CafeProductionMachineType.CoffeeMachine
            ? CoffeeProgressPosition
            : BakerProgressPosition;

        rectTransform.sizeDelta = new Vector2(190f, 48f);
    }

    private void SetMachineWorkingScale(CafeProductionMachineType machineType, float elapsed)
    {
        Transform targetTransform = machineType == CafeProductionMachineType.CoffeeMachine
            ? coffeeMachineButton != null ? coffeeMachineButton.transform : null
            : bakerMachineButton != null ? bakerMachineButton.transform : null;

        if (targetTransform == null)
        {
            return;
        }

        Vector3 baseScale = machineType == CafeProductionMachineType.CoffeeMachine
            ? coffeeMachineBaseScale
            : bakerMachineBaseScale;
        float pulse = 1f + Mathf.Sin(elapsed * 10f) * 0.018f;
        targetTransform.localScale = baseScale * pulse;
    }

    private void ResetMachineScale(CafeProductionMachineType machineType)
    {
        if (machineType == CafeProductionMachineType.CoffeeMachine)
        {
            if (coffeeMachineButton != null)
            {
                coffeeMachineButton.transform.localScale = coffeeMachineBaseScale;
            }

            return;
        }

        if (bakerMachineButton != null)
        {
            bakerMachineButton.transform.localScale = bakerMachineBaseScale;
        }
    }

    private bool TryFindMenuIndex(string menuId, out int menuIndex)
    {
        menuIndex = -1;

        if (operationController == null)
        {
            return false;
        }

        for (int i = 0; i < operationController.MenuItems.Count; i++)
        {
            CafeMenuItem menuItem = operationController.MenuItems[i];

            if (menuItem != null && menuItem.MenuId == menuId)
            {
                menuIndex = i;
                return true;
            }
        }

        return false;
    }

    private RecipeConfig FindRecipe(string recipeId)
    {
        for (int i = 0; i < recipes.Count; i++)
        {
            if (recipes[i].RecipeId == recipeId)
            {
                return recipes[i];
            }
        }

        return null;
    }

    private void SetProgress(float progress)
    {
        SetProgress(progress, 0f);
    }

    private void SetProgress(float progress, float animationTime)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        EnsureProgressUiBindings();

        if (progressFillImage == null)
        {
            UpdateProgressSegments(clampedProgress, animationTime);
            return;
        }

        progressFillImage.type = Image.Type.Filled;
        progressFillImage.fillMethod = Image.FillMethod.Horizontal;
        progressFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFillImage.fillAmount = clampedProgress;
        progressFillImage.color = clampedProgress >= 0.999f
            ? new Color(0.55f, 1f, 0.36f, 1f)
            : Color.Lerp(new Color(1f, 0.64f, 0.18f, 0.96f), new Color(1f, 0.88f, 0.34f, 1f), 0.5f + Mathf.Sin(animationTime * 8f) * 0.5f);
        UpdateProgressSegments(clampedProgress, animationTime);
        RefreshProgressLayerOrder();
    }

    private void UpdateProgressSegments(float progress, float animationTime)
    {
        if (progressRoot != null && progressSegmentImages.Count == 0)
        {
            CreateProgressSegments(progressRoot.transform);
        }

        if (progressSegmentImages.Count == 0)
        {
            return;
        }

        float activeProgress = Mathf.Clamp01(progress) * progressSegmentImages.Count;

        for (int i = 0; i < progressSegmentImages.Count; i++)
        {
            Image segmentImage = progressSegmentImages[i];

            if (segmentImage == null)
            {
                continue;
            }

            float fill = Mathf.Clamp01(activeProgress - i);

            if (fill <= 0f)
            {
                segmentImage.color = new Color(0.96f, 0.64f, 0.18f, 0f);
                segmentImage.fillAmount = 0f;
                continue;
            }

            bool isComplete = progress >= 0.999f;
            float shimmer = fill >= 1f ? 0.95f : 0.8f + Mathf.Sin(animationTime * 10f + i) * 0.12f;
            Color workingColor = new Color(1f, 0.78f, 0.24f, Mathf.Clamp01(fill * shimmer));
            Color completeColor = new Color(0.62f, 1f, 0.42f, 1f);

            segmentImage.type = Image.Type.Filled;
            segmentImage.fillMethod = Image.FillMethod.Horizontal;
            segmentImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            segmentImage.fillAmount = fill;
            segmentImage.color = isComplete ? completeColor : workingColor;
        }
    }

    private IEnumerator AnimateCompleteCheck(float duration)
    {
        if (completeCheckRoot == null)
        {
            yield break;
        }

        Transform checkTransform = completeCheckRoot.transform;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float scale = t < 0.55f
                ? Mathf.Lerp(0.72f, 1.18f, t / 0.55f)
                : Mathf.Lerp(1.18f, 1f, (t - 0.55f) / 0.45f);
            checkTransform.localScale = Vector3.one * scale;
            yield return null;
        }

        checkTransform.localScale = Vector3.one;
    }

    private void RefreshProgressLayerOrder()
    {
        if (progressFrameImage != null)
        {
            progressFrameImage.transform.SetAsFirstSibling();
        }

        if (progressFillImage != null)
        {
            progressFillImage.transform.SetAsLastSibling();
        }

        for (int i = 0; i < progressSegmentImages.Count; i++)
        {
            Image segmentImage = progressSegmentImages[i];
            if (segmentImage != null)
            {
                segmentImage.transform.SetAsLastSibling();
            }
        }

        if (completeCheckRoot != null)
        {
            completeCheckRoot.transform.SetAsLastSibling();
        }
    }

    private void SetMachineButtonsInteractable(bool isInteractable)
    {
        if (coffeeMachineButton != null)
        {
            coffeeMachineButton.interactable = isInteractable;
        }

        if (bakerMachineButton != null)
        {
            bakerMachineButton.interactable = isInteractable;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void HideAll()
    {
        SetRootActive(productionPopupRoot, false);
        SetRootActive(recipePanelRoot, false);
        SetRootActive(progressRoot, false);
        SetRootActive(completeCheckRoot, false);
        SetProgress(0f);
        SetMachineButtonsInteractable(true);
    }

    private void SetRootActive(GameObject root, bool isActive)
    {
        if (root != null)
        {
            root.SetActive(isActive);
        }
    }

    private bool ValidateBindings()
    {
        EnsureProgressUiBindings();

        bool isValid = productionPopupRoot != null
            && recipePanelRoot != null
            && progressRoot != null
            && completeCheckRoot != null
            && coffeeMachineButton != null
            && bakerMachineButton != null
            && inariCoffeeRecipeButton != null
            && kitsunebiLatteRecipeButton != null
            && yozakuraCakeRecipeButton != null
            && progressFillImage != null;

        if (!isValid)
        {
            Debug.LogWarning("CafeProductionPopupController is missing UI bindings.");
        }

        return isValid;
    }

    private void WireButtonListeners()
    {
        if (listenersWired)
        {
            return;
        }

        if (coffeeMachineButton == null
            && bakerMachineButton == null
            && inariCoffeeRecipeButton == null
            && kitsunebiLatteRecipeButton == null
            && yozakuraCakeRecipeButton == null
            && closeButton == null)
        {
            return;
        }

        AddListener(coffeeMachineButton, OnCoffeeMachineButtonClicked);
        AddListener(bakerMachineButton, OnBakerMachineButtonClicked);
        AddListener(inariCoffeeRecipeButton, OnInariCoffeeRecipeButtonClicked);
        AddListener(kitsunebiLatteRecipeButton, OnKitsunebiLatteRecipeButtonClicked);
        AddListener(yozakuraCakeRecipeButton, OnYozakuraCakeRecipeButtonClicked);
        AddListener(closeButton, ClosePopup);
        listenersWired = true;
    }

    private void UnwireButtonListeners()
    {
        RemoveListener(coffeeMachineButton, OnCoffeeMachineButtonClicked);
        RemoveListener(bakerMachineButton, OnBakerMachineButtonClicked);
        RemoveListener(inariCoffeeRecipeButton, OnInariCoffeeRecipeButtonClicked);
        RemoveListener(kitsunebiLatteRecipeButton, OnKitsunebiLatteRecipeButtonClicked);
        RemoveListener(yozakuraCakeRecipeButton, OnYozakuraCakeRecipeButtonClicked);
        RemoveListener(closeButton, ClosePopup);
        listenersWired = false;
    }

    private void OnCoffeeMachineButtonClicked()
    {
        SelectMachine(CafeProductionMachineType.CoffeeMachine);
    }

    private void OnBakerMachineButtonClicked()
    {
        SelectMachine(CafeProductionMachineType.BakerMachine);
    }

    private void OnInariCoffeeRecipeButtonClicked()
    {
        StartRecipe("InariCoffee");
    }

    private void OnKitsunebiLatteRecipeButtonClicked()
    {
        StartRecipe("KitsunebiLatte");
    }

    private void OnYozakuraCakeRecipeButtonClicked()
    {
        StartRecipe("YozakuraCake");
    }

    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
        }
    }

    private Font GetUiFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Meiryo", "Arial" }, 18);
    }

    private ResourceInventory ResolveResourceInventory()
    {
        ResourceInventory inventory = ResourceInventory.Instance;
        return inventory != null ? inventory : FindAnyObjectByType<ResourceInventory>();
    }

    private class RecipeConfig
    {
        public string RecipeId { get; }
        public string MenuId { get; }
        public CafeProductionMachineType MachineType { get; }
        public float CraftSeconds { get; }
        public string OutputItemId { get; }
        public string DisplayName => RecipeId;

        public RecipeConfig(
            string recipeId,
            string menuId,
            CafeProductionMachineType machineType,
            float craftSeconds,
            string outputItemId)
        {
            RecipeId = recipeId;
            MenuId = menuId;
            MachineType = machineType;
            CraftSeconds = craftSeconds;
            OutputItemId = outputItemId;
        }
    }
}
