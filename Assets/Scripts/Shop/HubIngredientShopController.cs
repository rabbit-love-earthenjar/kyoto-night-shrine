using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HubIngredientShopController : MonoBehaviour
{
    [Header("World marker")]
    [SerializeField] private Sprite shopMarkerSprite;
    [SerializeField] private Vector2 shopPosition = new Vector2(3.25f, 1.45f);
    [SerializeField] private Vector3 shopScale = new Vector3(0.22f, 0.22f, 1f);
    [SerializeField] private Vector2 shopColliderSize = new Vector2(2.15f, 2.15f);
    [SerializeField] private Vector2 shopLabelOffset = new Vector2(0f, -1.4f);

    [Header("Shop panel icons")]
    [SerializeField] private Sprite merchantPortraitSprite;
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
    private ResourceInventory resourceInventory;

    private void Awake()
    {
        InitializeItems();
        ResolveResourceInventory().EnsureCafeStarterIngredients();
        CreateShopMarker();
    }

    public void ShowPanel()
    {
        EnsureEventSystem();
        EnsurePanel();
        RefreshPanel();
        panelObject.SetActive(true);
    }

    public void HidePanel()
    {
        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }
    }

    private void InitializeItems()
    {
        items[0] = new IngredientShopItem(ResourceInventory.CoffeeBeanId, "コーヒー豆", 1, coffeeBeanIcon);
        items[1] = new IngredientShopItem(ResourceInventory.MilkId, "ミルク", 1, milkIcon);
        items[2] = new IngredientShopItem(ResourceInventory.SugarId, "砂糖", 1, sugarIcon);
        items[3] = new IngredientShopItem(ResourceInventory.FlourId, "小麦粉", 2, flourIcon);
    }

    private void CreateShopMarker()
    {
        if (GameObject.Find("IngredientShop_仕入れ商店") != null)
        {
            return;
        }

        GameObject shopObject = new GameObject("IngredientShop_仕入れ商店");
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
        label.text = "仕入れ商店";
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
        panelRect.sizeDelta = new Vector2(650f, 470f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.055f, 0.05f, 0.96f);

        CreateText("仕入れ商店", panelObject.transform, new Vector2(0f, 190f), new Vector2(500f, 36f), 26);
        faithPointText = CreateText(string.Empty, panelObject.transform, new Vector2(0f, 150f), new Vector2(560f, 30f), 19);
        stockText = CreateText(string.Empty, panelObject.transform, new Vector2(0f, 112f), new Vector2(590f, 44f), 16);

        if (merchantPortraitSprite != null)
        {
            CreateIcon("RabbitMerchantIcon", merchantPortraitSprite, panelObject.transform, new Vector2(-275f, 166f), new Vector2(72f, 88f));
        }

        buttonTexts = new Text[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            int itemIndex = i;
            IngredientShopItem item = items[i];
            Button button = CreateButton(
                $"IngredientButton_{i + 1:00}",
                panelObject.transform,
                new Vector2(0f, 54f - i * 58f),
                new Vector2(430f, 44f),
                () => PurchaseIngredient(itemIndex));

            if (item.Icon != null)
            {
                CreateIcon("IngredientIcon", item.Icon, button.transform, new Vector2(-182f, 0f), new Vector2(38f, 38f));
            }

            buttonTexts[i] = CreateText(string.Empty, button.transform, new Vector2(20f, 0f), new Vector2(370f, 36f), 18);
        }

        statusText = CreateText("必要な食材を仕入れてください。", panelObject.transform, new Vector2(0f, -146f), new Vector2(560f, 30f), 17);
        CreateButtonWithLabel("CloseButton", "Close", panelObject.transform, new Vector2(0f, -190f), new Vector2(150f, 40f), HidePanel);
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
            statusText.text = $"信仰値が足りません。{item.DisplayName} は {item.Cost} 必要です。";
            RefreshPanel();
            return;
        }

        inventory.AddIngredient(item.IngredientId, 1);
        statusText.text = $"{item.DisplayName} を 1 個購入しました。";
        RefreshPanel();
    }

    private void RefreshPanel()
    {
        ResourceInventory inventory = ResolveResourceInventory();
        faithPointText.text = $"信仰値: {inventory.FaithPoints}";
        stockText.text =
            $"在庫  コーヒー豆: {inventory.GetIngredientCount(ResourceInventory.CoffeeBeanId)}  /  "
            + $"ミルク: {inventory.GetIngredientCount(ResourceInventory.MilkId)}  /  "
            + $"砂糖: {inventory.GetIngredientCount(ResourceInventory.SugarId)}  /  "
            + $"小麦粉: {inventory.GetIngredientCount(ResourceInventory.FlourId)}";

        for (int i = 0; i < items.Length; i++)
        {
            IngredientShopItem item = items[i];
            buttonTexts[i].text =
                $"購入  {item.DisplayName} +1  /  在庫 {inventory.GetIngredientCount(item.IngredientId)}  /  {item.Cost} 信仰値";
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
        image.color = new Color(0.23f, 0.2f, 0.18f, 1f);

        Button button = buttonObject.AddComponent<Button>();
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

    private void CreateIcon(string objectName, Sprite sprite, Transform parent, Vector2 position, Vector2 size)
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
