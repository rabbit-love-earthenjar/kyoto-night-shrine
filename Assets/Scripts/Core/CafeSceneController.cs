using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CafeSceneController : MonoBehaviour
{
    private const string FoxAltarLevelKey = "CafeFoxAltarLevel";
    private const string FurnitureUnlockKeyPrefix = "CafeFurnitureUnlocked_";
    private const int MaxFoxAltarLevel = 4;
    private static readonly FurnitureUnlockData[] FoxAltarFurnitureUnlocks =
    {
        new FurnitureUnlockData(2, "furniture_small_flower_table", "小さな花卓"),
        new FurnitureUnlockData(3, "furniture_soft_sofa", "やわらかなソファ"),
        new FurnitureUnlockData(4, "furniture_shrine_lamp", "神社の小灯")
    };

    [SerializeField] private string returnSceneName = "HubMap_Day";
    [SerializeField] private string foxAltarObjectName = "fox_god_transparent_0";
    [SerializeField] private string counterObjectName = "CafeCounter";
    [SerializeField] private Vector2 exitPosition = new Vector2(0f, -3.75f);
    [SerializeField] private float exitHalfWidth = 1.15f;
    [SerializeField] private string cafePlayerObjectName = "CafePlayer";

    private GameObject cafeCanvasObject;
    private GameObject infoPanel;
    private Text infoTitle;
    private Text infoBody;
    private GameObject infoActionButtonObject;
    private Button infoActionButton;
    private Text infoActionButtonText;
    private Transform cafePlayer;
    private CafeOperationController cafeOperationController;
    private CafeOperationPanelController cafeOperationPanelController;
    private bool isReturningToHub;
    private string foxAltarFeedbackMessage;

    private void Awake()
    {
        EnsureEventSystem();
        CreateCafeCanvas();
        CreateInfoPanel();
        ResolveCafeOperationController();
        SetupCafeInteractions();
        ResolveCafePlayer();
    }

    private void Update()
    {
        if (isReturningToHub)
        {
            return;
        }

        ResolveCafePlayer();

        if (cafePlayer == null)
        {
            return;
        }

        Vector3 playerPosition = cafePlayer.position;
        bool insideDoorWidth = Mathf.Abs(playerPosition.x - exitPosition.x) <= exitHalfWidth;

        if (insideDoorWidth && playerPosition.y <= exitPosition.y)
        {
            ReturnToHub();
        }
    }

    public void ReturnToHub()
    {
        if (isReturningToHub || string.IsNullOrEmpty(returnSceneName))
        {
            return;
        }

        isReturningToHub = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(returnSceneName);
    }

    public void ShowFoxAltarPanel()
    {
        ResourceInventory inventory = ResolveResourceInventory();
        int faithPoints = inventory != null ? inventory.FaithPoints : 0;
        int heartFox = inventory != null ? inventory.HeartFoxCount : 0;
        int foxAltarLevel = GetFoxAltarLevel();
        int upgradeCost = GetFoxAltarUpgradeCost(foxAltarLevel);
        string feedback = string.IsNullOrEmpty(foxAltarFeedbackMessage)
            ? string.Empty
            : $"\n\n{foxAltarFeedbackMessage}";

        infoTitle.text = "狐狸供台";
        infoBody.text =
            $"Lv.{foxAltarLevel}\n" +
            $"神社の状態: 準備中\n" +
            $"信仰値: {faithPoints}\n" +
            $"こころ狐: {heartFox}\n" +
            BuildFoxAltarUpgradeText(foxAltarLevel, upgradeCost) +
            feedback;

        if (infoActionButtonObject != null)
        {
            bool canUpgrade = upgradeCost > 0;
            infoActionButtonObject.SetActive(canUpgrade);
            infoActionButton.interactable = canUpgrade && inventory != null;
            infoActionButtonText.text = canUpgrade ? $"強化  こころ狐 {upgradeCost}" : "最大Lv";
        }

        infoPanel.SetActive(true);
    }

    public void ShowReceptionPanel()
    {
        CafeOperationController operationController = ResolveCafeOperationController();
        CafeOperationPanelController panelController = ResolveCafeOperationPanelController();

        if (operationController != null && panelController != null)
        {
            infoPanel.SetActive(false);
            SetInfoActionVisible(false);
            panelController.Initialize(cafeCanvasObject.transform, operationController);
            panelController.Show();
            return;
        }

        infoTitle.text = "夜神社カフェ 営業";
        infoBody.text = BuildGuestSeatSummary();
        SetInfoActionVisible(false);
        infoPanel.SetActive(true);
    }

    public void HideInfoPanel()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
            SetInfoActionVisible(false);
            foxAltarFeedbackMessage = string.Empty;
        }
    }

    private void CreateCafeCanvas()
    {
        cafeCanvasObject = new GameObject("CafeCanvas");
        Canvas canvas = cafeCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = cafeCanvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        cafeCanvasObject.AddComponent<GraphicRaycaster>();
    }

    private void CreateInfoPanel()
    {
        infoPanel = new GameObject("CafeInfoPanel");
        infoPanel.transform.SetParent(cafeCanvasObject.transform, false);

        RectTransform panelRect = infoPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500f, 390f);

        Image panelImage = infoPanel.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.09f, 0.1f, 0.9f);

        infoTitle = CreateText("Title", infoPanel.transform, new Vector2(0f, 156f), new Vector2(430f, 42f), 28);
        infoBody = CreateText("Body", infoPanel.transform, new Vector2(0f, 46f), new Vector2(430f, 220f), 19);

        infoActionButtonObject = CreateInfoButton(
            "UpgradeButton",
            "強化",
            new Vector2(0f, -112f),
            new Vector2(210f, 40f),
            TryUpgradeFoxAltar,
            out infoActionButton,
            out infoActionButtonText);

        Button closeButton;
        Text closeButtonText;
        CreateInfoButton(
            "CloseButton",
            "Close",
            new Vector2(0f, -166f),
            new Vector2(160f, 42f),
            HideInfoPanel,
            out closeButton,
            out closeButtonText);
        closeButton.interactable = true;
        closeButtonText.color = Color.black;

        SetInfoActionVisible(false);
        infoPanel.SetActive(false);
    }

    private GameObject CreateInfoButton(
        string objectName,
        string label,
        Vector2 position,
        Vector2 size,
        UnityEngine.Events.UnityAction action,
        out Button button,
        out Text buttonText)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(infoPanel.transform, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = size;

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.86f, 0.82f, 0.72f, 1f);

        button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(action);

        buttonText = CreateText(label, buttonObject.transform, Vector2.zero, size - new Vector2(10f, 6f), 18);
        buttonText.color = Color.black;

        return buttonObject;
    }

    private void SetupCafeInteractions()
    {
        SetupInteraction(GameObject.Find(counterObjectName), CafeInteractionType.FrontCounter);
        SetupInteraction(GameObject.Find(foxAltarObjectName), CafeInteractionType.FoxAltar);
    }

    private void SetupInteraction(GameObject target, CafeInteractionType interactionType)
    {
        if (target == null)
        {
            return;
        }

        if (target.GetComponent<Collider2D>() == null)
        {
            target.AddComponent<BoxCollider2D>();
        }

        CafeInteractable interactable = target.GetComponent<CafeInteractable>();

        if (interactable == null)
        {
            interactable = target.AddComponent<CafeInteractable>();
        }

        interactable.Configure(this, interactionType);
    }

    private string BuildGuestSeatSummary()
    {
        CafeOperationController operationController = ResolveCafeOperationController();

        if (operationController != null)
        {
            return operationController.BuildGuestSeatSummary();
        }

        return "今は来訪者がいません。";
    }

    private void TryUpgradeFoxAltar()
    {
        ResourceInventory inventory = ResolveResourceInventory();
        int currentLevel = GetFoxAltarLevel();
        int upgradeCost = GetFoxAltarUpgradeCost(currentLevel);

        if (inventory == null || upgradeCost <= 0)
        {
            ShowFoxAltarPanel();
            return;
        }

        if (!inventory.SpendHeartFox(upgradeCost))
        {
            foxAltarFeedbackMessage = "こころ狐が足りません。";
            ShowFoxAltarPanel();
            return;
        }

        int nextLevel = Mathf.Min(currentLevel + 1, MaxFoxAltarLevel);
        PlayerPrefs.SetInt(FoxAltarLevelKey, nextLevel);
        string unlockedFurniture = UnlockFurnitureForLevel(nextLevel);
        foxAltarFeedbackMessage = "狐の祠が少しあたたかくなりました。";

        if (!string.IsNullOrEmpty(unlockedFurniture))
        {
            foxAltarFeedbackMessage += $"\n新しい家具が使えるようになりました。\n{unlockedFurniture}";
        }

        PlayerPrefs.Save();
        ShowFoxAltarPanel();
    }

    private int GetFoxAltarLevel()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(FoxAltarLevelKey, 1), 1, MaxFoxAltarLevel);
    }

    private int GetFoxAltarUpgradeCost(int currentLevel)
    {
        switch (currentLevel)
        {
            case 1:
                return 3;
            case 2:
                return 5;
            case 3:
                return 8;
            default:
                return 0;
        }
    }

    private string BuildFoxAltarUpgradeText(int currentLevel, int upgradeCost)
    {
        if (upgradeCost <= 0)
        {
            return $"解放済み家具: {BuildUnlockedFurnitureSummary(currentLevel)}\nこれ以上の強化は後日追加予定";
        }

        string unlockPreview = BuildFurnitureUnlockPreview(currentLevel + 1);
        string unlockedSummary = BuildUnlockedFurnitureSummary(currentLevel);

        return $"次の強化: こころ狐 {upgradeCost}\n次の家具解放: {unlockPreview}\n解放済み家具: {unlockedSummary}";
    }

    private string BuildFurnitureUnlockPreview(int targetLevel)
    {
        string summary = string.Empty;

        for (int i = 0; i < FoxAltarFurnitureUnlocks.Length; i++)
        {
            FurnitureUnlockData unlock = FoxAltarFurnitureUnlocks[i];

            if (unlock.RequiredLevel != targetLevel)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(summary))
            {
                summary += " / ";
            }

            summary += unlock.DisplayName;
        }

        return string.IsNullOrEmpty(summary) ? "後日追加予定" : summary;
    }

    private string BuildUnlockedFurnitureSummary(int currentLevel)
    {
        string summary = string.Empty;

        for (int i = 0; i < FoxAltarFurnitureUnlocks.Length; i++)
        {
            FurnitureUnlockData unlock = FoxAltarFurnitureUnlocks[i];

            if (unlock.RequiredLevel > currentLevel)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(summary))
            {
                summary += " / ";
            }

            summary += $"{unlock.DisplayName} ({unlock.UnlockId})";
        }

        return string.IsNullOrEmpty(summary) ? "なし" : summary;
    }

    private string UnlockFurnitureForLevel(int level)
    {
        string unlockedSummary = string.Empty;

        for (int i = 0; i < FoxAltarFurnitureUnlocks.Length; i++)
        {
            FurnitureUnlockData unlock = FoxAltarFurnitureUnlocks[i];

            if (unlock.RequiredLevel != level)
            {
                continue;
            }

            PlayerPrefs.SetInt(FurnitureUnlockKeyPrefix + unlock.UnlockId, 1);

            if (!string.IsNullOrEmpty(unlockedSummary))
            {
                unlockedSummary += " / ";
            }

            unlockedSummary += unlock.DisplayName;
        }

        return unlockedSummary;
    }

    private void SetInfoActionVisible(bool isVisible)
    {
        if (infoActionButtonObject != null)
        {
            infoActionButtonObject.SetActive(isVisible);
        }
    }

    private ResourceInventory ResolveResourceInventory()
    {
        ResourceInventory inventory = ResourceInventory.Instance;

        if (inventory == null)
        {
            inventory = FindAnyObjectByType<ResourceInventory>();
        }

        return inventory;
    }

    private CafeOperationController ResolveCafeOperationController()
    {
        if (cafeOperationController != null)
        {
            return cafeOperationController;
        }

        cafeOperationController = GetComponent<CafeOperationController>();

        if (cafeOperationController == null)
        {
            cafeOperationController = gameObject.AddComponent<CafeOperationController>();
        }

        return cafeOperationController;
    }

    private CafeOperationPanelController ResolveCafeOperationPanelController()
    {
        if (cafeOperationPanelController != null)
        {
            return cafeOperationPanelController;
        }

        cafeOperationPanelController = GetComponent<CafeOperationPanelController>();

        if (cafeOperationPanelController == null)
        {
            cafeOperationPanelController = gameObject.AddComponent<CafeOperationPanelController>();
        }

        return cafeOperationPanelController;
    }

    private void ResolveCafePlayer()
    {
        if (cafePlayer != null)
        {
            return;
        }

        GameObject playerObject = GameObject.Find(cafePlayerObjectName);
        cafePlayer = playerObject != null ? playerObject.transform : null;
    }

    private Text CreateText(string text, Transform parent, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject textObject = new GameObject(text);
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = position;
        textRect.sizeDelta = size;

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

    private class FurnitureUnlockData
    {
        public int RequiredLevel { get; }
        public string UnlockId { get; }
        public string DisplayName { get; }

        public FurnitureUnlockData(int requiredLevel, string unlockId, string displayName)
        {
            RequiredLevel = requiredLevel;
            UnlockId = unlockId;
            DisplayName = displayName;
        }
    }
}
