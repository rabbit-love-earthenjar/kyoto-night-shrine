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

    public bool IsProducing => productionCoroutine != null;

    public void Initialize(Transform canvasRoot, CafeOperationController controller)
    {
        operationController = controller;

        if (productionPopupRoot == null && canvasRoot != null)
        {
            CreateFallbackUi(canvasRoot);
        }

        ApplyFallbackSprites();
        WireButtonListeners();
        HideAll();
    }

    private void Awake()
    {
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
        SetMachineButtonsInteractable(false);
        PositionProgressRoot(recipe.MachineType);
        SetMachineWorkingScale(recipe.MachineType, 0f);
        SetRootActive(progressRoot, true);
        SetRootActive(completeCheckRoot, false);
        SetProgress(0f);

        float duration = Mathf.Max(0.1f, recipe.CraftSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetProgress(progress);
            SetMachineWorkingScale(recipe.MachineType, elapsed);
            SetStatus($"Producing {recipe.DisplayName} {Mathf.RoundToInt(progress * 100f)}%");
            yield return null;
        }

        SetProgress(1f);
        ResetMachineScale(recipe.MachineType);
        SetRootActive(completeCheckRoot, true);

        if (operationController != null)
        {
            operationController.CompleteProduction(menuItem, out string resultMessage);
            SetStatus(resultMessage);
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

        yield return new WaitForSeconds(Mathf.Max(0f, completeHoldSeconds));

        SetRootActive(progressRoot, false);
        SetRootActive(completeCheckRoot, false);
        SetProgress(0f);
        SetMachineButtonsInteractable(true);
        ResetMachineScale(recipe.MachineType);
        productionCoroutine = null;
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

        progressRoot = CreateRectObject("ProgressRoot", productionPopupRoot.transform, new Vector2(-330f, 150f), new Vector2(180f, 46f));

        GameObject fillObject = CreateRectObject("ProgressFill", progressRoot.transform, new Vector2(-8f, 1f), new Vector2(132f, 11f));
        progressFillImage = fillObject.AddComponent<Image>();
        progressFillImage.color = new Color(0.72f, 0.95f, 0.38f, 0.95f);

        GameObject frameObject = CreateRectObject("ProgressFrame", progressRoot.transform, Vector2.zero, new Vector2(180f, 24f));
        progressFrameImage = frameObject.AddComponent<Image>();
        progressFrameImage.color = Color.white;
        progressFrameImage.preserveAspect = true;

        completeCheckRoot = CreateRectObject("CompleteCheckRoot", progressRoot.transform, new Vector2(82f, 2f), new Vector2(28f, 28f));
        Text checkText = completeCheckRoot.AddComponent<Text>();
        checkText.text = "✓";
        checkText.alignment = TextAnchor.MiddleCenter;
        checkText.fontSize = 24;
        checkText.font = GetUiFont();
        checkText.color = new Color(0.32f, 0.88f, 0.22f, 1f);

        closeButton = CreateTextButton("CloseButton", productionPopupRoot.transform, new Vector2(390f, -232f), new Vector2(120f, 42f), "Close");
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

        Sprite progressSprite = LoadRuntimeSpriteRegion(
            progressFramePath,
            "CafeProgressFrame_FromProgressBar",
            new Rect(83f, 831f, 1062f, 139f),
            100f,
            true);

        if (progressSprite == null)
        {
            progressSprite = LoadSprite("Assets/Art/cafe_icon/progress_bar_cutout.png", "CafeProgressFrame_CutoutFallback");
        }

        spriteCache[cacheKey] = progressSprite;
        return progressSprite;
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
                    : new Vector2(18f, 232f);
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
            ? new Vector2(-330f, 140f)
            : new Vector2(18f, 202f);
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
        if (progressFillImage == null)
        {
            return;
        }

        progressFillImage.type = Image.Type.Filled;
        progressFillImage.fillMethod = Image.FillMethod.Horizontal;
        progressFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFillImage.fillAmount = Mathf.Clamp01(progress);
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
