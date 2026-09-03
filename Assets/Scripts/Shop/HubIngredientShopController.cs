using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class HubIngredientShopController : MonoBehaviour
{
    private const string ShopMarkerName = "IngredientShop_\u4ed5\u5165\u308c\u5546\u5e97";
    private const string ShopDisplayName = "\u4ed5\u5165\u308c\u5546\u5e97";

    [Header("World marker")]
    [SerializeField] private Sprite shopMarkerSprite;
    [SerializeField] private Vector2 shopPosition = new Vector2(3.25f, 1.45f);
    [SerializeField] private Vector3 shopScale = new Vector3(0.22f, 0.22f, 1f);
    [SerializeField] private Vector2 shopColliderSize = new Vector2(2.15f, 2.15f);
    [SerializeField] private Vector2 shopLabelOffset = new Vector2(0f, -1.4f);

    [Header("Shop panel art")]
    [SerializeField] private Sprite merchantPortraitSprite;
    [SerializeField] private Sprite shopInteriorBackgroundSprite;
    [SerializeField] private string shopInteriorBackgroundPath = "Art/Backgrounds/store.png";

    [Header("Merchant intro animation")]
    [SerializeField] private Vector2 merchantPortraitPosition = new Vector2(-470f, -172f);
    [SerializeField] private Vector2 merchantPortraitSize = new Vector2(320f, 370f);
    [SerializeField] private Vector2 merchantPopStartOffset = new Vector2(0f, -104f);
    [SerializeField] private float merchantPopDuration = 0.46f;
    [SerializeField] private float merchantPopStartScale = 0.72f;
    [SerializeField] private float merchantPopOvershootScale = 1.12f;

    [Header("Shop audio")]
    [SerializeField] private AudioClip shopBgmClip;
    [SerializeField, Range(0f, 1f)] private float shopBgmVolume = 0.22f;

    [Header("Shop panel icons")]
    [SerializeField] private Sprite coffeeBeanIcon;
    [SerializeField] private Sprite milkIcon;
    [SerializeField] private Sprite sugarIcon;
    [SerializeField] private Sprite flourIcon;

    private readonly IngredientShopItem[] items = new IngredientShopItem[4];

    private GameObject panelObject;
    private Text faithPointText;
    private Text stockText;
    private Text statusText;
    private Text[] buttonTexts;
    private AudioSource shopBgmSource;
    private RectTransform merchantPortraitRect;
    private Coroutine merchantIntroRoutine;
    private Sprite loadedShopInteriorBackgroundSprite;
    private ResourceInventory resourceInventory;

    private void Awake()
    {
        InitializeItems();
        ResolveResourceInventory().EnsureCafeStarterIngredients();
        CreateShopMarker();
    }

    private void OnDestroy()
    {
        StopShopBgm();
        GameAudio.ResumeBgmFromOverlay();
    }

    public void ShowPanel()
    {
        EnsureEventSystem();
        EnsurePanel();
        RefreshPanel();
        panelObject.SetActive(true);
        GameAudio.PauseBgmForOverlay();
        PlayShopBgm();
        PlayMerchantIntroAnimation();
    }

    public void HidePanel()
    {
        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }

        StopShopBgm();
        GameAudio.ResumeBgmFromOverlay();
    }

    public bool TryCloseOverlay()
    {
        if (panelObject == null || !panelObject.activeSelf)
        {
            return false;
        }

        HidePanel();
        return true;
    }

    private void InitializeItems()
    {
        items[0] = CreateIngredientShopItem(ResourceInventory.CoffeeBeanId, "\u30b3\u30fc\u30d2\u30fc\u8c46", coffeeBeanIcon);
        items[1] = CreateIngredientShopItem(ResourceInventory.MilkId, "\u30df\u30eb\u30af", milkIcon);
        items[2] = CreateIngredientShopItem(ResourceInventory.SugarId, "\u7802\u7cd6", sugarIcon);
        items[3] = CreateIngredientShopItem(ResourceInventory.FlourId, "\u5c0f\u9ea6\u7c89", flourIcon);
    }

    private static IngredientShopItem CreateIngredientShopItem(string ingredientId, string displayName, Sprite icon)
    {
        return new IngredientShopItem(
            ingredientId,
            displayName,
            CafeEconomyFormula.GetIngredientUnitPrice(ingredientId),
            icon);
    }

    private void CreateShopMarker()
    {
        if (GameObject.Find(ShopMarkerName) != null)
        {
            return;
        }

        GameObject shopObject = new GameObject(ShopMarkerName);
        Transform buildingsRoot = transform.Find("Buildings");

        if (buildingsRoot != null)
        {
            shopObject.transform.SetParent(buildingsRoot, false);
        }

        shopObject.transform.position = shopPosition;

        GameObject visualObject = new GameObject("IngredientShopVisual");
        visualObject.transform.SetParent(shopObject.transform, false);
        visualObject.transform.localScale = shopScale;

        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = shopMarkerSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 2;

        BoxCollider2D collider = shopObject.AddComponent<BoxCollider2D>();
        collider.size = shopColliderSize;

        HubMapInteractable interactable = shopObject.AddComponent<HubMapInteractable>();
        interactable.Configure(FindAnyObjectByType<HubMapController>(), HubInteractionType.IngredientShop);

        GameObject labelObject = new GameObject("IngredientShopLabel");
        labelObject.transform.SetParent(shopObject.transform, false);
        labelObject.transform.localPosition = shopLabelOffset;

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = ShopDisplayName;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 42;
        label.characterSize = 0.06f;
        label.color = new Color(1f, 0.94f, 0.78f, 1f);
    }

    private void EnsurePanel()
    {
        if (panelObject != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("HubIngredientShopCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 130;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasObject.AddComponent<GraphicRaycaster>();

        panelObject = new GameObject("HubIngredientShopPanel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1280f, 720f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.sprite = ResolveShopInteriorBackgroundSprite();
        panelImage.preserveAspect = false;
        panelImage.color = panelImage.sprite != null
            ? Color.white
            : new Color(0.12f, 0.075f, 0.045f, 1f);

        CreateDecorPanel("ShopInteriorShade", panelObject.transform, Vector2.zero, new Vector2(1280f, 720f), new Color(0.02f, 0.012f, 0.008f, 0.18f));
        CreateDecorPanel("ShopHeaderPlaque", panelObject.transform, new Vector2(0f, 250f), new Vector2(720f, 158f), new Color(0.11f, 0.06f, 0.028f, 0.9f));
        if (merchantPortraitSprite != null)
        {
            merchantPortraitRect = CreateIcon("RabbitMerchantIcon", merchantPortraitSprite, panelObject.transform, merchantPortraitPosition, merchantPortraitSize);
        }

        CreateDecorPanel("ShopTitlePlaque", panelObject.transform, new Vector2(0f, 286f), new Vector2(430f, 66f), new Color(0.2f, 0.1f, 0.04f, 0.92f));
        CreateText(ShopDisplayName, panelObject.transform, new Vector2(0f, 294f), new Vector2(390f, 38f), 32);
        faithPointText = CreateText(string.Empty, panelObject.transform, new Vector2(0f, 252f), new Vector2(390f, 30f), 20);

        CreateDecorPanel("ShopStockPlaque", panelObject.transform, new Vector2(0f, 190f), new Vector2(650f, 70f), new Color(0.09f, 0.055f, 0.03f, 0.86f));
        stockText = CreateText(string.Empty, panelObject.transform, new Vector2(0f, 190f), new Vector2(600f, 54f), 17);

        CreateDecorPanel("ShopItemBoard", panelObject.transform, new Vector2(0f, -22f), new Vector2(700f, 316f), new Color(0.11f, 0.055f, 0.025f, 0.76f));

        buttonTexts = new Text[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            int itemIndex = i;
            IngredientShopItem item = items[i];
            Button button = CreateButton(
                $"IngredientButton_{i + 1:00}",
                panelObject.transform,
                new Vector2(0f, 74f - i * 62f),
                new Vector2(560f, 50f),
                () => PurchaseIngredient(itemIndex));

            if (item.Icon != null)
            {
                CreateIcon("IngredientIcon", item.Icon, button.transform, new Vector2(-236f, 0f), new Vector2(40f, 40f));
            }

            buttonTexts[i] = CreateText(string.Empty, button.transform, new Vector2(34f, 0f), new Vector2(480f, 38f), 18);
        }

        statusText = CreateText("\u5fc5\u8981\u306a\u98df\u6750\u3092\u4ed5\u5165\u308c\u3066\u304f\u3060\u3055\u3044\u3002", panelObject.transform, new Vector2(0f, -210f), new Vector2(620f, 30f), 17);
        CreateButtonWithLabel("CloseButton", "Close", panelObject.transform, new Vector2(0f, -268f), new Vector2(160f, 42f), HidePanel);
        panelObject.SetActive(false);
    }

    private void PurchaseIngredient(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= items.Length)
        {
            return;
        }

        IngredientShopItem item = items[itemIndex];
        ResourceInventory inventory = ResolveResourceInventory();

        if (!inventory.SpendFaithPoints(item.Cost))
        {
            statusText.text = $"\u4fe1\u4ef0\u5024\u304c\u8db3\u308a\u307e\u305b\u3093\u3002{item.DisplayName} \u306f {item.Cost} \u5fc5\u8981\u3067\u3059\u3002";
            RefreshPanel();
            return;
        }

        inventory.AddIngredient(item.IngredientId, 1);
        statusText.text = $"{item.DisplayName} \u3092 1 \u500b\u8cfc\u5165\u3057\u307e\u3057\u305f\u3002";
        RefreshPanel();
    }

    private void RefreshPanel()
    {
        ResourceInventory inventory = ResolveResourceInventory();

        faithPointText.text = $"\u4fe1\u4ef0\u5024: {inventory.FaithPoints}";
        stockText.text =
            $"\u6240\u6301  \u30b3\u30fc\u30d2\u30fc\u8c46 {inventory.GetIngredientCount(ResourceInventory.CoffeeBeanId)}   \u30df\u30eb\u30af {inventory.GetIngredientCount(ResourceInventory.MilkId)}\n"
            + $"\u7802\u7cd6 {inventory.GetIngredientCount(ResourceInventory.SugarId)}   \u5c0f\u9ea6\u7c89 {inventory.GetIngredientCount(ResourceInventory.FlourId)}";

        for (int i = 0; i < items.Length; i++)
        {
            IngredientShopItem item = items[i];
            buttonTexts[i].text =
                $"\u8cfc\u5165  {item.DisplayName} +1    \u6240\u6301 {inventory.GetIngredientCount(item.IngredientId)}    {item.Cost} \u4fe1\u4ef0\u5024";
        }
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

    private void PlayShopBgm()
    {
        if (shopBgmClip == null)
        {
            return;
        }

        if (shopBgmSource == null)
        {
            shopBgmSource = GetComponent<AudioSource>();

            if (shopBgmSource == null)
            {
                shopBgmSource = gameObject.AddComponent<AudioSource>();
            }

            shopBgmSource.playOnAwake = false;
            shopBgmSource.loop = true;
            shopBgmSource.spatialBlend = 0f;
        }

        shopBgmSource.clip = shopBgmClip;
        shopBgmSource.volume = shopBgmVolume * GameSettings.BgmVolume;

        if (!shopBgmSource.isPlaying)
        {
            shopBgmSource.Play();
        }
    }

    private void StopShopBgm()
    {
        if (shopBgmSource != null && shopBgmSource.isPlaying)
        {
            shopBgmSource.Stop();
        }
    }

    private Sprite ResolveShopInteriorBackgroundSprite()
    {
        if (loadedShopInteriorBackgroundSprite != null)
        {
            return loadedShopInteriorBackgroundSprite;
        }

        if (!string.IsNullOrWhiteSpace(shopInteriorBackgroundPath))
        {
            string imagePath = Path.Combine(Application.dataPath, shopInteriorBackgroundPath);

            if (File.Exists(imagePath))
            {
                byte[] imageBytes = File.ReadAllBytes(imagePath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                if (texture.LoadImage(imageBytes))
                {
                    texture.filterMode = FilterMode.Point;
                    loadedShopInteriorBackgroundSprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                    return loadedShopInteriorBackgroundSprite;
                }
            }
        }

        return shopInteriorBackgroundSprite;
    }

    private void PlayMerchantIntroAnimation()
    {
        if (merchantPortraitRect == null)
        {
            return;
        }

        if (merchantIntroRoutine != null)
        {
            StopCoroutine(merchantIntroRoutine);
        }

        merchantIntroRoutine = StartCoroutine(AnimateMerchantIntro());
    }

    private IEnumerator AnimateMerchantIntro()
    {
        Vector2 startPosition = merchantPortraitPosition + merchantPopStartOffset;
        Vector2 overshootPosition = merchantPortraitPosition + new Vector2(0f, 12f);
        float safeDuration = Mathf.Max(0.05f, merchantPopDuration);
        float riseDuration = safeDuration * 0.68f;
        float settleDuration = safeDuration - riseDuration;

        merchantPortraitRect.anchoredPosition = startPosition;
        merchantPortraitRect.localScale = Vector3.one * Mathf.Max(0.01f, merchantPopStartScale);

        float elapsed = 0f;

        while (elapsed < riseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            merchantPortraitRect.anchoredPosition = Vector2.Lerp(startPosition, overshootPosition, eased);
            merchantPortraitRect.localScale = Vector3.one * Mathf.Lerp(merchantPopStartScale, merchantPopOvershootScale, eased);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < settleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / settleDuration);
            float eased = t * t * (3f - 2f * t);

            merchantPortraitRect.anchoredPosition = Vector2.Lerp(overshootPosition, merchantPortraitPosition, eased);
            merchantPortraitRect.localScale = Vector3.one * Mathf.Lerp(merchantPopOvershootScale, 1f, eased);
            yield return null;
        }

        merchantPortraitRect.anchoredPosition = merchantPortraitPosition;
        merchantPortraitRect.localScale = Vector3.one;
        merchantIntroRoutine = null;
    }

    private Image CreateDecorPanel(string objectName, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(objectName);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Button CreateButton(string objectName, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.28f, 0.145f, 0.065f, 0.96f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.16f, 1.08f, 0.92f, 1f);
        colors.pressedColor = new Color(0.78f, 0.62f, 0.45f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.45f, 0.36f, 0.28f, 0.7f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.onClick.AddListener(action);
        return button;
    }

    private void CreateButtonWithLabel(
        string objectName,
        string label,
        Transform parent,
        Vector2 position,
        Vector2 size,
        UnityEngine.Events.UnityAction action)
    {
        Button button = CreateButton(objectName, parent, position, size, action);
        CreateText(label, button.transform, Vector2.zero, size - new Vector2(10f, 8f), 18);
    }

    private RectTransform CreateIcon(string objectName, Sprite sprite, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject iconObject = new GameObject(objectName);
        iconObject.transform.SetParent(parent, false);

        RectTransform rect = iconObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = iconObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return rect;
    }

    private Text CreateText(string text, Transform parent, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.fontSize = fontSize;
        uiText.color = Color.white;
        uiText.font = GetUiFont();
        return uiText;
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

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private class IngredientShopItem
    {
        public string IngredientId { get; }
        public string DisplayName { get; }
        public int Cost { get; }
        public Sprite Icon { get; }

        public IngredientShopItem(string ingredientId, string displayName, int cost, Sprite icon)
        {
            IngredientId = ingredientId;
            DisplayName = displayName;
            Cost = cost;
            Icon = icon;
        }
    }
}
