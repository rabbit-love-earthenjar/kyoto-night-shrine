using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HubMapController : MonoBehaviour
{
    [SerializeField] private string warehouseTitle = "資材小屋";
    [SerializeField] private string shrineTitle = "荒れた小神社";
    [SerializeField] private string shrineStatus = "まだ修復されていません";
    [SerializeField] private string shrineRepairedStatus = "修復が完了しました";
    [SerializeField] private int shrineRepairFaithCost = 10;
    [SerializeField] private string cafeSceneName = "CafeInterior_Temporary";
    [SerializeField] private Transform uiRoot;
    [SerializeField] private SpriteRenderer shrineIconRenderer;
    [SerializeField] private Sprite repairedShrineSprite;
    [SerializeField] private Vector3 repairedShrineScale = new Vector3(0.16f, 0.16f, 1f);

    private GameObject panelCanvasObject;
    private GameObject panelObject;
    private GameObject repairButtonObject;
    private Button repairButton;
    private Image repairButtonImage;
    private Text titleText;
    private Text bodyText;
    private Text repairButtonText;
    private Text closeButtonText;
    private ResourceInventory resourceInventory;
    private Sprite originalShrineSprite;
    private Vector3 originalShrineScale;
    private bool shrineRepaired;
    private bool shrineVisualCached;
    private static bool shrineRepairedInSession;

    private void Awake()
    {
        shrineRepaired = shrineRepairedInSession;
        ApplyShrineVisualState();
        EnsureEventSystem();
    }

    public void ShowWarehousePanel()
    {
        ResourceInventory inventory = ResolveResourceInventory();
        int faithPoints = inventory != null ? inventory.FaithPoints : 0;
        int basicMaterialCount = inventory != null ? inventory.GetMaterialCount(ResourceInventory.BasicYokaiMaterialId) : 0;

        EnsurePanel();
        titleText.text = warehouseTitle;
        bodyText.text = $"FaithPoints: {faithPoints}\n{ResourceInventory.BasicYokaiMaterialId}: {basicMaterialCount}";
        repairButtonObject.SetActive(false);
        closeButtonText.text = "Close";
        panelObject.SetActive(true);
    }

    public void ShowShrinePanel()
    {
        EnsurePanel();
        titleText.text = shrineTitle;
        repairButtonObject.SetActive(true);
        closeButtonText.text = "Close";
        RefreshShrinePanel();
        panelObject.SetActive(true);
    }

    public void HidePanel()
    {
        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }
    }

    public void TryRepairShrine()
    {
        if (shrineRepaired)
        {
            EnterCafe();
            return;
        }

        ResourceInventory inventory = ResolveResourceInventory();

        if (inventory == null || !inventory.SpendFaithPoints(shrineRepairFaithCost))
        {
            RefreshShrinePanel();
            return;
        }

        shrineRepaired = true;
        shrineRepairedInSession = true;
        ApplyShrineVisualState();
        RefreshShrinePanel();
    }

    public void EnterCafe()
    {
        if (string.IsNullOrEmpty(cafeSceneName))
        {
            return;
        }

        SceneManager.LoadScene(cafeSceneName);
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

    private void EnsurePanel()
    {
        if (panelObject != null)
        {
            return;
        }

        panelCanvasObject = new GameObject("HubMapPanelCanvas");
        ResolveUiRoot();

        if (uiRoot != null)
        {
            panelCanvasObject.transform.SetParent(uiRoot, false);
        }

        Canvas canvas = panelCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        CanvasScaler scaler = panelCanvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        panelCanvasObject.AddComponent<GraphicRaycaster>();

        panelObject = new GameObject("HubMapInfoPanel");
        panelObject.transform.SetParent(panelCanvasObject.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(380f, 270f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.09f, 0.1f, 0.88f);

        titleText = CreateText("Title", panelObject.transform, new Vector2(0f, 88f), new Vector2(320f, 42f), 28, TextAnchor.MiddleCenter);
        bodyText = CreateText("Body", panelObject.transform, new Vector2(0f, 32f), new Vector2(320f, 82f), 21, TextAnchor.MiddleCenter);
        CreateRepairButton(panelObject.transform);
        CreateCloseButton(panelObject.transform);

        panelObject.SetActive(false);
    }

    private void CreateCloseButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("HubMapPanelButton");
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -98f);
        buttonRect.sizeDelta = new Vector2(170f, 42f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.86f, 0.82f, 0.72f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(HidePanel);

        closeButtonText = CreateText("Close", buttonObject.transform, Vector2.zero, new Vector2(160f, 36f), 20, TextAnchor.MiddleCenter);
        closeButtonText.color = Color.black;
    }

    private void CreateRepairButton(Transform parent)
    {
        repairButtonObject = new GameObject("RepairButton");
        repairButtonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = repairButtonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -48f);
        buttonRect.sizeDelta = new Vector2(190f, 38f);

        repairButtonImage = repairButtonObject.AddComponent<Image>();
        repairButtonImage.color = new Color(0.35f, 0.35f, 0.35f, 0.9f);

        repairButton = repairButtonObject.AddComponent<Button>();
        repairButton.onClick.AddListener(TryRepairShrine);

        repairButtonText = CreateText("修復する", repairButtonObject.transform, Vector2.zero, new Vector2(180f, 34f), 18, TextAnchor.MiddleCenter);
        repairButtonText.color = Color.black;
        repairButtonObject.SetActive(false);
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

        Text uiText = textObject.AddComponent<Text>();
        uiText.text = objectName;
        uiText.alignment = alignment;
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

    private void ResolveUiRoot()
    {
        if (uiRoot != null)
        {
            return;
        }

        Transform foundRoot = transform.Find("UI");

        if (foundRoot != null)
        {
            uiRoot = foundRoot;
        }
    }

    private void RefreshShrinePanel()
    {
        ResourceInventory inventory = ResolveResourceInventory();
        int faithPoints = inventory != null ? inventory.FaithPoints : 0;
        bool canRepair = !shrineRepaired && faithPoints >= shrineRepairFaithCost;

        if (shrineRepaired)
        {
            bodyText.text = $"{shrineRepairedStatus}\n小さな神社に灯りが戻りました";
            SetRepairButtonState(true, "カフェに入る", new Color(0.86f, 0.82f, 0.72f, 1f), Color.black);
            return;
        }

        bodyText.text = $"{shrineStatus}\n必要な信仰値: {shrineRepairFaithCost}\n現在の信仰値: {faithPoints}";

        if (canRepair)
        {
            SetRepairButtonState(true, "修復する", new Color(0.86f, 0.82f, 0.72f, 1f), Color.black);
        }
        else
        {
            SetRepairButtonState(false, "信仰値不足", new Color(0.35f, 0.35f, 0.35f, 0.9f), new Color(0.82f, 0.82f, 0.82f, 1f));
        }
    }

    private void SetRepairButtonState(bool interactable, string label, Color backgroundColor, Color textColor)
    {
        if (repairButton != null)
        {
            repairButton.interactable = interactable;
        }

        if (repairButtonImage != null)
        {
            repairButtonImage.color = backgroundColor;
        }

        if (repairButtonText != null)
        {
            repairButtonText.text = label;
            repairButtonText.color = textColor;
        }
    }

    private void ApplyShrineVisualState()
    {
        ResolveShrineIconRenderer();

        if (shrineIconRenderer == null)
        {
            return;
        }

        if (shrineRepaired)
        {
            if (repairedShrineSprite != null)
            {
                shrineIconRenderer.sprite = repairedShrineSprite;
                shrineIconRenderer.transform.localScale = repairedShrineScale;
                shrineIconRenderer.color = Color.white;
                return;
            }

            shrineIconRenderer.color = new Color(1f, 0.95f, 0.78f, 1f);
            return;
        }

        if (originalShrineSprite != null)
        {
            shrineIconRenderer.sprite = originalShrineSprite;
            shrineIconRenderer.transform.localScale = originalShrineScale;
        }

        shrineIconRenderer.color = Color.white;
    }

    private void ResolveShrineIconRenderer()
    {
        if (shrineIconRenderer == null)
        {
            Transform shrineTransform = transform.Find("Buildings/RuinedShrineIcon");
            shrineIconRenderer = shrineTransform != null ? shrineTransform.GetComponent<SpriteRenderer>() : null;
        }

        if (shrineIconRenderer != null && !shrineVisualCached)
        {
            originalShrineSprite = shrineIconRenderer.sprite;
            originalShrineScale = shrineIconRenderer.transform.localScale;
            shrineVisualCached = true;
        }
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
}
