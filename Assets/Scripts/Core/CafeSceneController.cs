using System.Collections.Generic;
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
        new FurnitureUnlockData(1, "furniture_fox_icon", "狐のしるし", "Assets/Art/cafe_icon/fox_god_transparent.png"),
        new FurnitureUnlockData(1, "furniture_fox_altar_base", "狐の供台"),
        new FurnitureUnlockData(2, "furniture_small_flower_table", "小さな花卓"),
        new FurnitureUnlockData(3, "furniture_sofa_double_up", "二人掛けソファ 上", "Assets/Art/cafe_icon/sofa_up_green.png"),
        new FurnitureUnlockData(3, "furniture_sofa_double_down", "二人掛けソファ 下", "Assets/Art/cafe_icon/sofa_down_green.png"),
        new FurnitureUnlockData(3, "furniture_sofa_double_left", "二人掛けソファ 左"),
        new FurnitureUnlockData(3, "furniture_sofa_double_right", "二人掛けソファ 右"),
        new FurnitureUnlockData(4, "furniture_shrine_lamp", "神社の小灯"),
        new FurnitureUnlockData(4, "furniture_torii_small", "小さな鳥居")
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
    private Button infoCloseButton;
    private Text infoCloseButtonText;
    private GameObject furniturePreviewRoot;
    private Transform cafePlayer;
    private CafeOperationController cafeOperationController;
    private CafeOperationPanelController cafeOperationPanelController;
    private readonly Dictionary<string, Sprite> furniturePreviewSpriteCache = new Dictionary<string, Sprite>();
    private readonly HashSet<string> loggedMissingFurniturePreviewSprites = new HashSet<string>();
    private bool isReturningToHub;
    private bool isShowingCafeResult;
    private string foxAltarFeedbackMessage;

    private void Awake()
    {
        EnsureEventSystem();
        CreateCafeCanvas();
        CreateInfoPanel();
        ResolveCafeOperationController();
        EnsureFurnitureUnlocksForLevel(GetFoxAltarLevel(), false, true);
        SetupCafeInteractions();
        ResolveCafePlayer();
    }

    private void Update()
    {
        if (isReturningToHub || isShowingCafeResult)
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

        ShowCafeDayResultPanel();
    }

    private void LoadHubAfterCafeResult()
    {
        if (isReturningToHub || string.IsNullOrEmpty(returnSceneName))
        {
            return;
        }

        CafeOperationController operationController = ResolveCafeOperationController();

        if (operationController != null)
        {
            operationController.ResetCafeSessionResults();
        }

        isShowingCafeResult = false;
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

        RefreshFurniturePreview(foxAltarLevel);

        if (infoActionButtonObject != null)
        {
            bool canUpgrade = upgradeCost > 0;
            infoActionButtonObject.SetActive(canUpgrade);
            infoActionButton.interactable = canUpgrade && inventory != null;
            infoActionButtonText.text = canUpgrade ? $"強化  こころ狐 {upgradeCost}" : "最大Lv";
            ConfigureInfoActionButton(canUpgrade ? $"強化  こころ狐 {upgradeCost}" : "最大Lv", TryUpgradeFoxAltar, canUpgrade && inventory != null);
        }

        ConfigureInfoCloseButton("Close", HideInfoPanel);
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
            SetFurniturePreviewVisible(false);
            panelController.Initialize(cafeCanvasObject.transform, operationController);
            panelController.Show();
            return;
        }

        infoTitle.text = "夜神社カフェ 営業";
        infoBody.text = BuildGuestSeatSummary();
        SetInfoActionVisible(false);
        SetFurniturePreviewVisible(false);
        ConfigureInfoCloseButton("Close", HideInfoPanel);
        infoPanel.SetActive(true);
    }

    public void HideInfoPanel()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
            SetInfoActionVisible(false);
            SetFurniturePreviewVisible(false);
            foxAltarFeedbackMessage = string.Empty;
            isShowingCafeResult = false;
            ConfigureInfoCloseButton("Close", HideInfoPanel);
        }
    }

    private void ShowCafeDayResultPanel()
    {
        CafeOperationController operationController = ResolveCafeOperationController();
        CafeOperationPanelController panelController = ResolveCafeOperationPanelController();
        isShowingCafeResult = true;

        if (panelController != null)
        {
            panelController.Hide();
        }

        infoTitle.text = "今日のカフェ記録";
        infoBody.text = operationController != null
            ? operationController.BuildCafeDayResultSummary()
            : "今日も、少しだけ灯りが増えました。\n\n来訪者: 0人\n信仰値: +0\nこころ狐: +0\n好感度アップ: 0人\n解放された家具: 新しい家具はありません";

        SetInfoActionVisible(false);
        SetFurniturePreviewVisible(false);
        ConfigureInfoCloseButton("HubMapへ戻る", LoadHubAfterCafeResult);
        infoPanel.SetActive(true);
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
        panelRect.sizeDelta = new Vector2(560f, 460f);

        Image panelImage = infoPanel.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.09f, 0.1f, 0.9f);

        infoTitle = CreateText("Title", infoPanel.transform, new Vector2(0f, 190f), new Vector2(480f, 42f), 28);
        infoBody = CreateText("Body", infoPanel.transform, new Vector2(0f, 86f), new Vector2(480f, 190f), 18);

        infoActionButtonObject = CreateInfoButton(
            "UpgradeButton",
            "強化",
            new Vector2(0f, -154f),
            new Vector2(210f, 40f),
            TryUpgradeFoxAltar,
            out infoActionButton,
            out infoActionButtonText);

        CreateInfoButton(
            "CloseButton",
            "Close",
            new Vector2(0f, -208f),
            new Vector2(160f, 42f),
            HideInfoPanel,
            out infoCloseButton,
            out infoCloseButtonText);
        infoCloseButton.interactable = true;
        infoCloseButtonText.color = Color.black;

        CreateFurniturePreviewRoot();
        SetInfoActionVisible(false);
        SetFurniturePreviewVisible(false);
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

    private void CreateFurniturePreviewRoot()
    {
        furniturePreviewRoot = new GameObject("FurniturePreviewRoot");
        furniturePreviewRoot.transform.SetParent(infoPanel.transform, false);

        RectTransform previewRect = furniturePreviewRoot.AddComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = new Vector2(0f, -62f);
        previewRect.sizeDelta = new Vector2(480f, 82f);

        Image background = furniturePreviewRoot.AddComponent<Image>();
        background.color = new Color(0.12f, 0.08f, 0.06f, 0.58f);
    }

    private void RefreshFurniturePreview(int currentLevel)
    {
        if (furniturePreviewRoot == null)
        {
            return;
        }

        ClearFurniturePreview();
        SetFurniturePreviewVisible(true);

        CreateText("家具プレビュー", furniturePreviewRoot.transform, new Vector2(-178f, 25f), new Vector2(120f, 24f), 13);

        int previewIndex = 0;

        for (int i = 0; i < FoxAltarFurnitureUnlocks.Length && previewIndex < 6; i++)
        {
            FurnitureUnlockData unlock = FoxAltarFurnitureUnlocks[i];

            if (unlock.RequiredLevel > currentLevel)
            {
                continue;
            }

            CreateFurniturePreviewSlot(unlock, previewIndex);
            previewIndex++;
        }

        if (previewIndex == 0)
        {
            CreateText("家具はまだありません", furniturePreviewRoot.transform, new Vector2(54f, -4f), new Vector2(250f, 32f), 15);
        }
    }

    private void CreateFurniturePreviewSlot(FurnitureUnlockData unlock, int index)
    {
        GameObject slotObject = new GameObject($"FurniturePreview_{unlock.UnlockId}");
        slotObject.transform.SetParent(furniturePreviewRoot.transform, false);

        RectTransform slotRect = slotObject.AddComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.5f, 0.5f);
        slotRect.anchorMax = new Vector2(0.5f, 0.5f);
        slotRect.pivot = new Vector2(0.5f, 0.5f);
        slotRect.anchoredPosition = new Vector2(-90f + index * 64f, -2f);
        slotRect.sizeDelta = new Vector2(58f, 64f);

        Image slotBackground = slotObject.AddComponent<Image>();
        slotBackground.color = new Color(0.74f, 0.58f, 0.36f, 0.24f);

        Sprite previewSprite = LoadFurniturePreviewSprite(unlock.PreviewSpritePath);

        if (previewSprite != null)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(slotObject.transform, false);

            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 9f);
            iconRect.sizeDelta = new Vector2(38f, 38f);

            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = previewSprite;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
        }
        else
        {
            CreateText("IconFallback", slotObject.transform, new Vector2(0f, 9f), new Vector2(42f, 34f), 12).text = "家具";
        }

        CreateText("Label", slotObject.transform, new Vector2(0f, -23f), new Vector2(54f, 18f), 9).text = unlock.DisplayName;
    }

    private void ClearFurniturePreview()
    {
        if (furniturePreviewRoot == null)
        {
            return;
        }

        for (int i = furniturePreviewRoot.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(furniturePreviewRoot.transform.GetChild(i).gameObject);
        }
    }

    private void SetFurniturePreviewVisible(bool isVisible)
    {
        if (furniturePreviewRoot != null)
        {
            furniturePreviewRoot.SetActive(isVisible);
        }
    }

    private Sprite LoadFurniturePreviewSprite(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        if (furniturePreviewSpriteCache.TryGetValue(assetPath, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite loadedSprite = null;

#if UNITY_EDITOR
        loadedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        if (loadedSprite == null)
        {
            Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            if (texture != null)
            {
                loadedSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                loadedSprite.name = $"{texture.name}_FurniturePreviewSprite";
            }
        }
#endif

        furniturePreviewSpriteCache[assetPath] = loadedSprite;

        if (loadedSprite == null && loggedMissingFurniturePreviewSprites.Add(assetPath))
        {
            Debug.LogWarning($"Furniture preview sprite not found: {assetPath}");
        }

        return loadedSprite;
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
        string unlockedFurniture = EnsureFurnitureUnlocksForLevel(nextLevel, true);
        foxAltarFeedbackMessage = $"狐の祠が少しあたたかくなりました。\nLv.{currentLevel} -> Lv.{nextLevel}";

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

    private string EnsureFurnitureUnlocksForLevel(int level, bool recordSessionUnlocks, bool saveImmediately = false)
    {
        string unlockedSummary = string.Empty;
        bool unlockedAnyFurniture = false;

        for (int i = 0; i < FoxAltarFurnitureUnlocks.Length; i++)
        {
            FurnitureUnlockData unlock = FoxAltarFurnitureUnlocks[i];

            if (unlock.RequiredLevel > level)
            {
                continue;
            }

            string unlockKey = FurnitureUnlockKeyPrefix + unlock.UnlockId;

            if (PlayerPrefs.GetInt(unlockKey, 0) == 1)
            {
                continue;
            }

            PlayerPrefs.SetInt(unlockKey, 1);
            unlockedAnyFurniture = true;

            if (recordSessionUnlocks)
            {
                CafeOperationController operationController = ResolveCafeOperationController();

                if (operationController != null)
                {
                    operationController.RecordSessionFurnitureUnlock(unlock.UnlockId, unlock.DisplayName);
                }
            }

            if (!string.IsNullOrEmpty(unlockedSummary))
            {
                unlockedSummary += " / ";
            }

            unlockedSummary += unlock.DisplayName;
        }

        if (unlockedAnyFurniture && saveImmediately)
        {
            PlayerPrefs.Save();
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

    private void ConfigureInfoActionButton(string label, UnityEngine.Events.UnityAction action, bool isInteractable)
    {
        if (infoActionButton == null)
        {
            return;
        }

        infoActionButton.onClick.RemoveAllListeners();

        if (action != null)
        {
            infoActionButton.onClick.AddListener(action);
        }

        infoActionButton.interactable = isInteractable;

        if (infoActionButtonText != null)
        {
            infoActionButtonText.text = label;
        }
    }

    private void ConfigureInfoCloseButton(string label, UnityEngine.Events.UnityAction action)
    {
        if (infoCloseButton == null)
        {
            return;
        }

        infoCloseButton.onClick.RemoveAllListeners();

        if (action != null)
        {
            infoCloseButton.onClick.AddListener(action);
        }

        infoCloseButton.interactable = true;

        if (infoCloseButtonText != null)
        {
            infoCloseButtonText.text = label;
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
        public string PreviewSpritePath { get; }

        public FurnitureUnlockData(int requiredLevel, string unlockId, string displayName, string previewSpritePath = null)
        {
            RequiredLevel = requiredLevel;
            UnlockId = unlockId;
            DisplayName = displayName;
            PreviewSpritePath = previewSpritePath;
        }
    }
}
