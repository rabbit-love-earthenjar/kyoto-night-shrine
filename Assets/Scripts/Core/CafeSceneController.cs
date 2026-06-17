using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CafeSceneController : MonoBehaviour
{
    public const string FoxAltarLevelKey = "CafeFoxAltarLevel";
    private const string FurnitureUnlockKeyPrefix = "CafeFurnitureUnlocked_";
    private const string MenuBoardObjectName = "MenuBoard";
    private const string OpenBusinessLabel = "\u958B\u696D";
    private const string BusinessOpenedLabel = "\u55B6\u696D\u4E2D";
    private const string MessageBoardLabel = "\u7559\u8A00";
    private const string BoardCloseLabel = "Close";
    private const string BusinessOpenedMessage = "\u591C\u795E\u793E\u30AB\u30D5\u30A7\u3092\u958B\u3051\u307E\u3057\u305F\u3002";
    private const string AlreadyBusinessOpenedMessage = "\u3059\u3067\u306B\u958B\u696D\u4E2D\u3067\u3059\u3002";
    private const string VisitorMessageTitle = "\u6765\u8A2A\u8005\u306E\u7559\u8A00";
    private const string EmptyVisitorMessage = "\u307E\u3060\u7559\u8A00\u306F\u3042\u308A\u307E\u305B\u3093\u3002";
    public const int MaxFoxAltarLevel = 4;
    private static readonly FurnitureUnlockData[] FoxAltarFurnitureUnlocks =
    {
        new FurnitureUnlockData(1, "furniture_fox_icon", "狐のしるし", "Assets/Art/cafe_icon/fox_god_transparent.png"),
        new FurnitureUnlockData(1, "furniture_fox_altar_base", "狐の供台"),
        new FurnitureUnlockData(2, "furniture_small_flower_table", "小さな花卓", "Assets/Art/cafe_icon/cafe_icons_cutouts/cafe_icon_20.png"),
        new FurnitureUnlockData(3, "furniture_sofa_double_up", "二人掛けソファ 上", "Assets/Art/cafe_icon/sofa_up_green.png"),
        new FurnitureUnlockData(4, "furniture_shrine_lamp", "神社の小灯"),
        new FurnitureUnlockData(4, "furniture_torii_small", "小さな鳥居")
    };
    private static readonly FixedFurnitureDisplayData[] FixedFurnitureDisplays =
    {
        new FixedFurnitureDisplayData(
            1,
            "counter_decoration",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_bottle_shelf.png",
            new Vector3(-0.35f, 2.72f, 0f),
            new Vector3(0.2f, 0.2f, 1f),
            5),
        new FixedFurnitureDisplayData(
            1,
            "coffee_table_basic",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_low_table_tea.png",
            new Vector3(-4.78f, -1.83f, 0f),
            new Vector3(0.36f, 0.36f, 1f),
            2),
        new FixedFurnitureDisplayData(
            2,
            "sofa_front",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_sofa_front_a.png",
            new Vector3(-4.78f, -2.36f, 0f),
            new Vector3(0.5f, 0.5f, 1f),
            2),
        new FixedFurnitureDisplayData(
            2,
            "sofa_back",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_sofa_back.png",
            new Vector3(-4.78f, -1.26f, 0f),
            new Vector3(0.5f, 0.5f, 1f),
            2),
        new FixedFurnitureDisplayData(
            2,
            "sofa_left",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_sofa_side_left.png",
            new Vector3(-5.42f, -1.8f, 0f),
            new Vector3(0.5f, 0.5f, 1f),
            2),
        new FixedFurnitureDisplayData(
            2,
            "sofa_right",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_sofa_side_right.png",
            new Vector3(-4.14f, -1.8f, 0f),
            new Vector3(0.5f, 0.5f, 1f),
            2),
        new FixedFurnitureDisplayData(
            3,
            "furniture_sofa_double_up",
            "Assets/Art/cafe_icon/sofa_up_green.png",
            new Vector3(-4.9f, -1.72f, 0f),
            new Vector3(0.78f, 0.78f, 1f),
            2),
        new FixedFurnitureDisplayData(
            4,
            "furniture_shrine_lamp",
            null,
            new Vector3(-5.35f, -2.75f, 0f),
            new Vector3(0.28f, 0.56f, 1f),
            2),
        new FixedFurnitureDisplayData(
            4,
            "furniture_torii_small",
            null,
            new Vector3(5.35f, -2.75f, 0f),
            new Vector3(0.62f, 0.44f, 1f),
            2)
    };
    private static readonly FurnitureSeatAnchorData[] FurnitureSeatAnchors =
    {
        new FurnitureSeatAnchorData("furniture_sofa_double_up", "GuestSeat_05", new Vector3(-4.9f, -1.42f, 0f))
    };
    private static readonly FurnitureDefinition[] CafeFurnitureDefinitions =
    {
        new FurnitureDefinition(
            "fox_shrine_small",
            "\u72D0\u306E\u5C0F\u3055\u306A\u795E\u68DA",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_fox_altar_corner.png",
            "altar_corner",
            0,
            true,
            1,
            "\u6700\u521D\u304B\u3089\u4F7F\u3048\u308B\u72D0\u306E\u795E\u68DA\u30A2\u30A4\u30B3\u30F3\u3002"),
        new FurnitureDefinition(
            "counter_decoration",
            "\u30AB\u30A6\u30F3\u30BF\u30FC\u98FE\u308A",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_bottle_shelf.png",
            "counter_top",
            0,
            true,
            1,
            "\u30AB\u30A6\u30F3\u30BF\u30FC\u306B\u6696\u304B\u3055\u3092\u8DB3\u3059\u5C0F\u7269\u3002"),
        new FurnitureDefinition(
            "coffee_table_basic",
            "\u5C0F\u3055\u306A\u8336\u5353",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_low_table_tea.png",
            "left_lounge",
            4,
            false,
            1,
            "\u6765\u8A2A\u8005\u304C\u843D\u3061\u7740\u3051\u308B\u5C0F\u3055\u306A\u30C6\u30FC\u30D6\u30EB\u3002"),
        new FurnitureDefinition(
            "sofa_front",
            "\u30BD\u30D5\u30A1\u524D",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_sofa_front_a.png",
            "left_lounge",
            6,
            false,
            2,
            "\u5DE6\u306E\u4F11\u61A9\u30B9\u30DA\u30FC\u30B9\u7528\u306E\u30BD\u30D5\u30A1\u3002"),
        new FurnitureDefinition(
            "sofa_back",
            "\u30BD\u30D5\u30A1\u5965",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_sofa_back.png",
            "left_lounge",
            6,
            false,
            2,
            "\u5965\u5411\u304D\u306E\u30BD\u30D5\u30A1\u3002"),
        new FurnitureDefinition(
            "sofa_left",
            "\u30BD\u30D5\u30A1\u5DE6",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_sofa_side_left.png",
            "left_lounge",
            6,
            false,
            2,
            "\u5DE6\u5411\u304D\u306E\u30BD\u30D5\u30A1\u3002"),
        new FurnitureDefinition(
            "sofa_right",
            "\u30BD\u30D5\u30A1\u53F3",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_sofa_side_right.png",
            "left_lounge",
            6,
            false,
            2,
            "\u53F3\u5411\u304D\u306E\u30BD\u30D5\u30A1\u3002")
    };
    private static readonly CounterMachineDisplayData[] CounterMachineDisplays =
    {
        new CounterMachineDisplayData(
            "CafeCounter_CoffeeMachine",
            "Assets/Art/cafe_icon/coffe_mechine_cutout.png",
            new Vector3(-1.75f, 2.63f, 0f),
            new Vector3(0.145f, 0.145f, 1f),
            4),
        new CounterMachineDisplayData(
            "CafeCounter_BakerMachine",
            "Assets/Art/cafe_icon/baker_mechine_cutout.png",
            new Vector3(1.18f, 2.63f, 0f),
            new Vector3(0.132f, 0.132f, 1f),
            4)
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
    private GameObject infoSecondaryActionButtonObject;
    private Button infoSecondaryActionButton;
    private Text infoSecondaryActionButtonText;
    private Button infoCloseButton;
    private Text infoCloseButtonText;
    private GameObject furniturePreviewRoot;
    private Transform cafePlayer;
    private CafeOperationController cafeOperationController;
    [SerializeField] private CafeOperationPanelController cafeOperationPanelController;
    [SerializeField] private CafeProductionPopupController cafeProductionPopupController;
    private readonly Dictionary<string, Sprite> furniturePreviewSpriteCache = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, GameObject> fixedFurnitureObjects = new Dictionary<string, GameObject>();
    private readonly HashSet<string> loggedMissingFurniturePreviewSprites = new HashSet<string>();
    private GameObject fixedFurnitureRoot;
    private GameObject furnitureSeatAnchorRoot;
    private GameObject counterMachineRoot;
    private GameObject doorMessageBoardTextObject;
    private TextMesh doorMessageBoardText;
    private GameObject menuBoardActionPanel;
    private Button openBusinessButton;
    private Text openBusinessButtonText;
    private Button messageBoardButton;
    private Text messageBoardButtonText;
    private Button menuBoardCloseButton;
    private Text menuBoardCloseButtonText;
    private bool isReturningToHub;
    private bool isShowingCafeResult;
    private string foxAltarFeedbackMessage;
    private string furnitureFeedbackMessage;

    private void Awake()
    {
        EnsureEventSystem();
        CreateCafeCanvas();
        CreateInfoPanel();
        ResolveCafeOperationController();
        if (cafeOperationController != null)
        {
            cafeOperationController.StateChanged += RefreshDoorMessageBoardText;
            cafeOperationController.StateChanged += RefreshMenuBoardActionState;
        }
        EnsureDefaultFurnitureUnlocks();
        EnsureFurnitureUnlocksForLevel(GetFoxAltarLevel(), false, true);
        RefreshFixedFurnitureDisplays(false);
        RefreshFurnitureSeatAnchors();
        HideCounterMachineDisplays();
        EnsureDoorMessageBoardText();
        SetupCafeInteractions();
        ResolveCafePlayer();
    }

    private void OnDestroy()
    {
        if (cafeOperationController != null)
        {
            cafeOperationController.StateChanged -= RefreshDoorMessageBoardText;
            cafeOperationController.StateChanged -= RefreshMenuBoardActionState;
        }
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
            BuildProductionUpgradeText(foxAltarLevel) + "\n" +
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

        SetInfoSecondaryActionVisible(true);
        ConfigureInfoSecondaryActionButton("\u5BB6\u5177", ShowFurnitureUnlockPanel, true);
        ConfigureInfoCloseButton("Close", HideInfoPanel);
        infoPanel.SetActive(true);
    }

    public void ShowReceptionPanel()
    {
        CafeOperationController operationController = ResolveCafeOperationController();
        CafeProductionPopupController productionPopupController = ResolveCafeProductionPopupController(true);

        if (operationController != null && productionPopupController != null)
        {
            infoPanel.SetActive(false);
            SetInfoActionVisible(false);
            SetFurniturePreviewVisible(false);
            productionPopupController.Initialize(cafeCanvasObject.transform, operationController);
            productionPopupController.OpenPopup();
            return;
        }

        CafeOperationPanelController panelController = ResolveCafeOperationPanelController(true);

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

    public void ShowMenuBoardPanel()
    {
        CreateMenuBoardActionPanel();
        PositionMenuBoardActionPanel();
        RefreshMenuBoardActionState();

        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
            SetInfoActionVisible(false);
            SetFurniturePreviewVisible(false);
        }

        menuBoardActionPanel.SetActive(true);
    }

    private void OpenCafeFromMenuBoard()
    {
        CafeOperationController operationController = ResolveCafeOperationController();

        if (operationController == null)
        {
            return;
        }

        bool opened = operationController.TryOpenForBusiness();
        operationController.SetCafeFeedbackMessage(opened ? BusinessOpenedMessage : AlreadyBusinessOpenedMessage);
        RefreshDoorMessageBoardText();
        RefreshMenuBoardActionState();
        HideMenuBoardActionPanel();

        infoTitle.text = OpenBusinessLabel;
        infoBody.text = opened ? BusinessOpenedMessage : AlreadyBusinessOpenedMessage;
        SetInfoActionVisible(false);
        SetFurniturePreviewVisible(false);
        ConfigureInfoCloseButton("Close", HideInfoPanel);
        infoPanel.SetActive(true);
    }

    private void ShowMenuBoardMessages()
    {
        CafeOperationController operationController = ResolveCafeOperationController();
        string summary = operationController != null ? operationController.BuildMessageBoardSummary() : string.Empty;

        HideMenuBoardActionPanel();
        infoTitle.text = VisitorMessageTitle;
        infoBody.text = string.IsNullOrWhiteSpace(summary) ? EmptyVisitorMessage : summary;
        SetInfoActionVisible(false);
        SetFurniturePreviewVisible(false);
        ConfigureInfoCloseButton("Close", HideInfoPanel);
        infoPanel.SetActive(true);
    }

    private void HideMenuBoardActionPanel()
    {
        if (menuBoardActionPanel != null)
        {
            menuBoardActionPanel.SetActive(false);
        }
    }

    public void HideInfoPanel()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
            SetInfoActionVisible(false);
            SetFurniturePreviewVisible(false);
            foxAltarFeedbackMessage = string.Empty;
            furnitureFeedbackMessage = string.Empty;
            isShowingCafeResult = false;
            ConfigureInfoCloseButton("Close", HideInfoPanel);
        }
    }

    private void ShowCafeDayResultPanel()
    {
        CafeOperationController operationController = ResolveCafeOperationController();
        CafeProductionPopupController productionPopupController = ResolveCafeProductionPopupController(false);
        CafeOperationPanelController panelController = ResolveCafeOperationPanelController(false);
        isShowingCafeResult = true;

        if (productionPopupController != null)
        {
            productionPopupController.ClosePopup();
        }

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
        panelRect.sizeDelta = new Vector2(580f, 500f);

        Image panelImage = infoPanel.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.09f, 0.1f, 0.9f);

        infoTitle = CreateText("Title", infoPanel.transform, new Vector2(0f, 214f), new Vector2(500f, 42f), 28);
        infoBody = CreateText("Body", infoPanel.transform, new Vector2(0f, 98f), new Vector2(500f, 224f), 17);

        infoActionButtonObject = CreateInfoButton(
            "UpgradeButton",
            "強化",
            new Vector2(-112f, -174f),
            new Vector2(196f, 40f),
            TryUpgradeFoxAltar,
            out infoActionButton,
            out infoActionButtonText);

        infoSecondaryActionButtonObject = CreateInfoButton(
            "FurnitureButton",
            "\u5BB6\u5177",
            new Vector2(112f, -174f),
            new Vector2(196f, 40f),
            ShowFurnitureUnlockPanel,
            out infoSecondaryActionButton,
            out infoSecondaryActionButtonText);

        CreateInfoButton(
            "CloseButton",
            "Close",
            new Vector2(0f, -228f),
            new Vector2(160f, 42f),
            HideInfoPanel,
            out infoCloseButton,
            out infoCloseButtonText);
        infoCloseButton.interactable = true;
        infoCloseButtonText.color = Color.black;

        CreateFurniturePreviewRoot();
        SetInfoActionVisible(false);
        SetInfoSecondaryActionVisible(false);
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

    private void CreateMenuBoardActionPanel()
    {
        if (menuBoardActionPanel != null)
        {
            return;
        }

        menuBoardActionPanel = new GameObject("MenuBoardActionPanel");
        menuBoardActionPanel.transform.SetParent(cafeCanvasObject.transform, false);

        RectTransform panelRect = menuBoardActionPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(150f, 126f);

        Image panelImage = menuBoardActionPanel.AddComponent<Image>();
        panelImage.color = new Color(0.18f, 0.09f, 0.035f, 0.86f);

        Outline panelOutline = menuBoardActionPanel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.88f, 0.78f, 0.58f, 0.95f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        CreateCanvasButton(
            "OpenBusinessButton",
            menuBoardActionPanel.transform,
            OpenBusinessLabel,
            new Vector2(0f, 40f),
            new Vector2(112f, 30f),
            OpenCafeFromMenuBoard,
            out openBusinessButton,
            out openBusinessButtonText);

        CreateCanvasButton(
            "MessageBoardButton",
            menuBoardActionPanel.transform,
            MessageBoardLabel,
            new Vector2(0f, 4f),
            new Vector2(112f, 30f),
            ShowMenuBoardMessages,
            out messageBoardButton,
            out messageBoardButtonText);

        CreateCanvasButton(
            "MenuBoardCloseButton",
            menuBoardActionPanel.transform,
            BoardCloseLabel,
            new Vector2(0f, -38f),
            new Vector2(90f, 28f),
            HideMenuBoardActionPanel,
            out menuBoardCloseButton,
            out menuBoardCloseButtonText);

        menuBoardActionPanel.SetActive(false);
    }

    private GameObject CreateCanvasButton(
        string objectName,
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size,
        UnityEngine.Events.UnityAction action,
        out Button button,
        out Text buttonText)
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
        buttonImage.color = new Color(0.84f, 0.72f, 0.52f, 0.96f);

        button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(action);

        buttonText = CreateText(label, buttonObject.transform, Vector2.zero, size - new Vector2(8f, 4f), 16);
        buttonText.color = new Color(0.12f, 0.06f, 0.025f, 1f);

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
        previewRect.anchoredPosition = new Vector2(0f, -78f);
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

    private void RefreshFixedFurnitureDisplays(bool animateNewFurniture)
    {
        EnsureFixedFurnitureRoot();
        RemoveObsoleteFixedFurnitureObject("furniture_small_flower_table");
        RemoveObsoleteFixedFurnitureObject("furniture_sofa_double_down");
        RemoveObsoleteFixedFurnitureObject("furniture_sofa_double_left");
        RemoveObsoleteFixedFurnitureObject("furniture_sofa_double_right");
        RemoveObsoleteSceneObject("UnlockedFurniture_furniture_small_flower_table");
        RemoveObsoleteSceneObject("UnlockedFurniture_furniture_sofa_double_down");
        RemoveObsoleteSceneObject("UnlockedFurniture_furniture_sofa_double_left");
        RemoveObsoleteSceneObject("UnlockedFurniture_furniture_sofa_double_right");
        int currentLevel = GetFoxAltarLevel();

        for (int i = 0; i < FixedFurnitureDisplays.Length; i++)
        {
            FixedFurnitureDisplayData displayData = FixedFurnitureDisplays[i];
            bool isUnlocked = displayData.RequiredLevel <= currentLevel && IsFurnitureUnlocked(displayData.UnlockId);

            if (!isUnlocked)
            {
                RemoveFixedFurnitureObject(displayData.UnlockId);
                continue;
            }

            if (fixedFurnitureObjects.ContainsKey(displayData.UnlockId))
            {
                continue;
            }

            CreateFixedFurnitureObject(displayData, animateNewFurniture);
        }
    }

    private void EnsureFixedFurnitureRoot()
    {
        if (fixedFurnitureRoot != null)
        {
            return;
        }

        fixedFurnitureRoot = GameObject.Find("UnlockedFurniture");

        if (fixedFurnitureRoot == null)
        {
            fixedFurnitureRoot = new GameObject("UnlockedFurniture");
        }

        fixedFurnitureRoot.transform.position = Vector3.zero;
        fixedFurnitureRoot.transform.rotation = Quaternion.identity;
        fixedFurnitureRoot.transform.localScale = Vector3.one;
    }

    private void CreateFixedFurnitureObject(FixedFurnitureDisplayData displayData, bool animateNewFurniture)
    {
        Sprite displaySprite = LoadFurniturePreviewSprite(displayData.SpritePath);

        if (displaySprite == null)
        {
            return;
        }

        GameObject furnitureObject = new GameObject($"UnlockedFurniture_{displayData.UnlockId}");
        furnitureObject.transform.SetParent(fixedFurnitureRoot.transform, false);
        furnitureObject.transform.position = displayData.WorldPosition;
        furnitureObject.transform.localScale = animateNewFurniture ? Vector3.zero : displayData.WorldScale;

        SpriteRenderer spriteRenderer = furnitureObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = displaySprite;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = displayData.SortingOrder;

        fixedFurnitureObjects[displayData.UnlockId] = furnitureObject;

        if (animateNewFurniture)
        {
            StartCoroutine(AnimateFixedFurnitureAppear(furnitureObject.transform, spriteRenderer, displayData.WorldPosition, displayData.WorldScale));
        }
    }

    private void RemoveFixedFurnitureObject(string unlockId)
    {
        if (!fixedFurnitureObjects.TryGetValue(unlockId, out GameObject furnitureObject))
        {
            return;
        }

        fixedFurnitureObjects.Remove(unlockId);

        if (furnitureObject != null)
        {
            Destroy(furnitureObject);
        }
    }

    private void RemoveObsoleteFixedFurnitureObject(string unlockId)
    {
        RemoveFixedFurnitureObject(unlockId);

        if (fixedFurnitureRoot == null)
        {
            return;
        }

        Transform obsoleteTransform = fixedFurnitureRoot.transform.Find($"UnlockedFurniture_{unlockId}");

        if (obsoleteTransform != null)
        {
            Destroy(obsoleteTransform.gameObject);
        }
    }

    private void RemoveObsoleteSceneObject(string objectName)
    {
        GameObject obsoleteObject = GameObject.Find(objectName);

        if (obsoleteObject != null)
        {
            Destroy(obsoleteObject);
        }
    }

    private IEnumerator AnimateFixedFurnitureAppear(Transform furnitureTransform, SpriteRenderer spriteRenderer, Vector3 targetPosition, Vector3 targetScale)
    {
        const float animationSeconds = 0.55f;
        float elapsed = 0f;
        Vector3 startPosition = targetPosition + new Vector3(0f, 0.35f, 0f);
        Color targetColor = spriteRenderer.color;
        Color startColor = targetColor;
        startColor.a = 0f;

        furnitureTransform.position = startPosition;
        furnitureTransform.localScale = Vector3.zero;
        spriteRenderer.color = startColor;

        while (elapsed < animationSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animationSeconds));
            furnitureTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            furnitureTransform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            spriteRenderer.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        furnitureTransform.position = targetPosition;
        furnitureTransform.localScale = targetScale;
        spriteRenderer.color = targetColor;
    }

    private void RefreshFurnitureSeatAnchors()
    {
        EnsureFurnitureSeatAnchorRoot();
        RemoveObsoleteFurnitureSeatAnchor("GuestSeat_06");
        RemoveObsoleteFurnitureSeatAnchor("GuestSeat_07");
        RemoveObsoleteFurnitureSeatAnchor("GuestSeat_08");
        RemoveObsoleteSceneObject("GuestSeat_06");
        RemoveObsoleteSceneObject("GuestSeat_07");
        RemoveObsoleteSceneObject("GuestSeat_08");
        int currentLevel = GetFoxAltarLevel();

        for (int i = 0; i < FurnitureSeatAnchors.Length; i++)
        {
            FurnitureSeatAnchorData anchorData = FurnitureSeatAnchors[i];
            bool isUnlocked = GetFurnitureRequiredLevel(anchorData.UnlockId) <= currentLevel && IsFurnitureUnlocked(anchorData.UnlockId);
            Transform anchor = furnitureSeatAnchorRoot.transform.Find(anchorData.SeatName);

            if (anchor == null)
            {
                GameObject anchorObject = new GameObject(anchorData.SeatName);
                anchorObject.transform.SetParent(furnitureSeatAnchorRoot.transform, false);
                anchorObject.transform.position = anchorData.WorldPosition;
                anchor = anchorObject.transform;
            }

            anchor.position = anchorData.WorldPosition;
            anchor.gameObject.SetActive(isUnlocked);
        }
    }

    private void RemoveObsoleteFurnitureSeatAnchor(string seatName)
    {
        if (furnitureSeatAnchorRoot == null)
        {
            return;
        }

        Transform obsoleteAnchor = furnitureSeatAnchorRoot.transform.Find(seatName);

        if (obsoleteAnchor != null)
        {
            Destroy(obsoleteAnchor.gameObject);
        }
    }

    private void EnsureFurnitureSeatAnchorRoot()
    {
        if (furnitureSeatAnchorRoot != null)
        {
            return;
        }

        furnitureSeatAnchorRoot = GameObject.Find("FurnitureSeatAnchors");

        if (furnitureSeatAnchorRoot == null)
        {
            furnitureSeatAnchorRoot = new GameObject("FurnitureSeatAnchors");
        }

        furnitureSeatAnchorRoot.transform.position = Vector3.zero;
        furnitureSeatAnchorRoot.transform.rotation = Quaternion.identity;
        furnitureSeatAnchorRoot.transform.localScale = Vector3.one;
    }

    private void EnsureCounterMachineDisplays()
    {
        EnsureCounterMachineRoot();

        for (int i = 0; i < CounterMachineDisplays.Length; i++)
        {
            CounterMachineDisplayData displayData = CounterMachineDisplays[i];
            Transform existingTransform = counterMachineRoot.transform.Find(displayData.ObjectName);
            GameObject machineObject = existingTransform != null
                ? existingTransform.gameObject
                : new GameObject(displayData.ObjectName);

            machineObject.transform.SetParent(counterMachineRoot.transform, false);
            machineObject.transform.position = displayData.WorldPosition;
            machineObject.transform.localScale = displayData.WorldScale;

            SpriteRenderer spriteRenderer = machineObject.GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer = machineObject.AddComponent<SpriteRenderer>();
            }

            Sprite displaySprite = LoadFurniturePreviewSprite(displayData.SpritePath);
            spriteRenderer.sprite = displaySprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = displayData.SortingOrder;
            machineObject.SetActive(displaySprite != null);
        }
    }

    private void HideCounterMachineDisplays()
    {
        EnsureCounterMachineRoot();

        if (counterMachineRoot != null)
        {
            counterMachineRoot.SetActive(false);
        }
    }

    private void EnsureCounterMachineRoot()
    {
        if (counterMachineRoot != null)
        {
            return;
        }

        counterMachineRoot = GameObject.Find("CafeCounterMachines");

        if (counterMachineRoot == null)
        {
            counterMachineRoot = new GameObject("CafeCounterMachines");
        }

        counterMachineRoot.transform.position = Vector3.zero;
        counterMachineRoot.transform.rotation = Quaternion.identity;
        counterMachineRoot.transform.localScale = Vector3.one;
    }

    private void EnsureDoorMessageBoardText()
    {
        if (doorMessageBoardTextObject == null)
        {
            doorMessageBoardTextObject = GameObject.Find("MenuBoardVisitorMessages");

            if (doorMessageBoardTextObject == null)
            {
                doorMessageBoardTextObject = new GameObject("MenuBoardVisitorMessages");
            }
        }

        Transform menuBoard = GameObject.Find(MenuBoardObjectName)?.transform;
        doorMessageBoardTextObject.transform.position = menuBoard != null
            ? menuBoard.position + new Vector3(0f, 0.14f, 0f)
            : new Vector3(1.46f, -2.67f, 0f);
        doorMessageBoardTextObject.transform.rotation = Quaternion.identity;
        doorMessageBoardTextObject.transform.localScale = Vector3.one;

        if (doorMessageBoardText == null)
        {
            doorMessageBoardText = doorMessageBoardTextObject.GetComponent<TextMesh>();

            if (doorMessageBoardText == null)
            {
                doorMessageBoardText = doorMessageBoardTextObject.AddComponent<TextMesh>();
            }
        }

        doorMessageBoardText.anchor = TextAnchor.MiddleCenter;
        doorMessageBoardText.alignment = TextAlignment.Center;
        doorMessageBoardText.font = GetUiFont();
        doorMessageBoardText.fontSize = 32;
        doorMessageBoardText.characterSize = 0.026f;
        doorMessageBoardText.color = new Color(0.16f, 0.08f, 0.035f, 1f);

        MeshRenderer textRenderer = doorMessageBoardTextObject.GetComponent<MeshRenderer>();

        if (textRenderer != null)
        {
            textRenderer.sharedMaterial = doorMessageBoardText.font.material;
            textRenderer.sortingOrder = 8;
        }

        RefreshDoorMessageBoardText();
    }

    private void PositionMenuBoardActionPanel()
    {
        if (menuBoardActionPanel == null)
        {
            return;
        }

        RectTransform panelRect = menuBoardActionPanel.GetComponent<RectTransform>();

        if (panelRect == null)
        {
            return;
        }

        Transform menuBoard = GameObject.Find(MenuBoardObjectName)?.transform;
        Vector3 worldPosition = menuBoard != null
            ? menuBoard.position + new Vector3(0.32f, 0.38f, 0f)
            : new Vector3(1.76f, -2.23f, 0f);
        panelRect.anchoredPosition = WorldToCafeCanvasPosition(worldPosition);
    }

    private Vector2 WorldToCafeCanvasPosition(Vector3 worldPosition)
    {
        RectTransform canvasRect = cafeCanvasObject.GetComponent<RectTransform>();

        if (canvasRect == null)
        {
            return Vector2.zero;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint);
        return localPoint;
    }

    private void RefreshDoorMessageBoardText()
    {
        if (doorMessageBoardText == null)
        {
            return;
        }

        CafeOperationController operationController = ResolveCafeOperationController();
        string summary = operationController != null
            ? operationController.BuildMessageBoardSummary()
            : string.Empty;

        if (string.IsNullOrEmpty(summary))
        {
            doorMessageBoardText.text = "来訪者の留言";
            return;
        }

        string[] lines = summary.Split('\n');
        string boardText = "来訪者の留言";
        int addedLines = 0;

        for (int i = 0; i < lines.Length && addedLines < 2; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            boardText += "\n" + ShortenBoardLine(lines[i].Trim(), 13);
            addedLines++;
        }

        doorMessageBoardText.text = boardText;
    }

    private void RefreshMenuBoardActionState()
    {
        if (openBusinessButton == null || openBusinessButtonText == null)
        {
            return;
        }

        CafeOperationController operationController = ResolveCafeOperationController();
        bool isOpen = operationController != null && operationController.IsOpenForBusiness;
        openBusinessButton.interactable = !isOpen;
        openBusinessButtonText.text = isOpen ? BusinessOpenedLabel : OpenBusinessLabel;

        if (messageBoardButton != null)
        {
            messageBoardButton.interactable = operationController != null;
        }

        if (messageBoardButtonText != null)
        {
            messageBoardButtonText.text = MessageBoardLabel;
        }
    }

    private string ShortenBoardLine(string line, int maxCharacters)
    {
        if (string.IsNullOrEmpty(line) || line.Length <= maxCharacters)
        {
            return line;
        }

        return line.Substring(0, Mathf.Max(1, maxCharacters - 1)) + "…";
    }

    private bool IsFurnitureUnlocked(string unlockId)
    {
        return !string.IsNullOrEmpty(unlockId) && PlayerPrefs.GetInt(FurnitureUnlockKeyPrefix + unlockId, 0) == 1;
    }

    private void EnsureDefaultFurnitureUnlocks()
    {
        bool changed = false;

        for (int i = 0; i < CafeFurnitureDefinitions.Length; i++)
        {
            FurnitureDefinition definition = CafeFurnitureDefinitions[i];

            if (!definition.IsDefaultUnlocked)
            {
                continue;
            }

            string key = FurnitureUnlockKeyPrefix + definition.FurnitureId;

            if (PlayerPrefs.GetInt(key, 0) == 1)
            {
                continue;
            }

            PlayerPrefs.SetInt(key, 1);
            changed = true;
        }

        if (changed)
        {
            PlayerPrefs.Save();
        }
    }

    private int GetFurnitureRequiredLevel(string unlockId)
    {
        for (int i = 0; i < FoxAltarFurnitureUnlocks.Length; i++)
        {
            FurnitureUnlockData unlockData = FoxAltarFurnitureUnlocks[i];

            if (unlockData.UnlockId == unlockId)
            {
                return unlockData.RequiredLevel;
            }
        }

        return MaxFoxAltarLevel + 1;
    }

    private void SetupCafeInteractions()
    {
        SetupInteraction(GameObject.Find(counterObjectName), CafeInteractionType.FrontCounter);
        SetupInteraction(GameObject.Find(foxAltarObjectName), CafeInteractionType.FoxAltar);
        SetupInteraction(GameObject.Find(MenuBoardObjectName), CafeInteractionType.MenuBoard);
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

    private void ShowFurnitureUnlockPanel()
    {
        ResourceInventory inventory = ResolveResourceInventory();
        FurnitureDefinition nextFurniture = GetNextLockedFurnitureDefinition();
        string feedback = string.IsNullOrEmpty(furnitureFeedbackMessage)
            ? string.Empty
            : $"\n\n{furnitureFeedbackMessage}";

        infoTitle.text = "\u5BB6\u5177";
        infoBody.text = BuildFurnitureUnlockPanelText(inventory) + feedback;
        SetFurniturePreviewVisible(false);

        bool canUnlock = nextFurniture != null
            && inventory != null
            && inventory.FaithPoints >= nextFurniture.UnlockFaithCost;
        string actionLabel = nextFurniture != null
            ? $"\u89E3\u653E {nextFurniture.DisplayName} {nextFurniture.UnlockFaithCost}\u4FE1\u4EF0\u5024"
            : "\u89E3\u653E\u53EF\u80FD\u306A\u5BB6\u5177\u306A\u3057";

        SetInfoActionVisible(nextFurniture != null);
        ConfigureInfoActionButton(actionLabel, TryUnlockNextFurniture, canUnlock);
        SetInfoSecondaryActionVisible(true);
        ConfigureInfoSecondaryActionButton("\u623B\u308B", ShowFoxAltarPanel, true);
        ConfigureInfoCloseButton("Close", HideInfoPanel);
        infoPanel.SetActive(true);
    }

    private string BuildFurnitureUnlockPanelText(ResourceInventory inventory)
    {
        int faithPoints = inventory != null ? inventory.FaithPoints : 0;
        string body = $"\u4FE1\u4EF0\u5024: {faithPoints}\n";

        for (int i = 0; i < CafeFurnitureDefinitions.Length; i++)
        {
            FurnitureDefinition definition = CafeFurnitureDefinitions[i];
            string state = IsFurnitureUnlocked(definition.FurnitureId)
                ? "\u89E3\u653E\u6E08\u307F"
                : "\u672A\u89E3\u653E";
            string levelText = definition.RequiredFoxAltarLevel > 1
                ? $" / \u4F9B\u53F0Lv.{definition.RequiredFoxAltarLevel}"
                : string.Empty;

            body += $"\n{definition.DisplayName}: {state} / {definition.UnlockFaithCost}\u4FE1\u4EF0\u5024{levelText}";
        }

        return body;
    }

    private void TryUnlockNextFurniture()
    {
        FurnitureDefinition nextFurniture = GetNextLockedFurnitureDefinition();

        if (nextFurniture == null)
        {
            furnitureFeedbackMessage = "\u4ECA\u89E3\u653E\u3067\u304D\u308B\u5BB6\u5177\u306F\u3042\u308A\u307E\u305B\u3093\u3002";
            ShowFurnitureUnlockPanel();
            return;
        }

        TryUnlockFurniture(nextFurniture.FurnitureId);
    }

    private void TryUnlockFurniture(string furnitureId)
    {
        FurnitureDefinition definition = GetFurnitureDefinition(furnitureId);

        if (definition == null)
        {
            furnitureFeedbackMessage = "\u5BB6\u5177\u30C7\u30FC\u30BF\u304C\u898B\u3064\u304B\u308A\u307E\u305B\u3093\u3002";
            ShowFurnitureUnlockPanel();
            return;
        }

        if (IsFurnitureUnlocked(definition.FurnitureId))
        {
            furnitureFeedbackMessage = "\u3059\u3067\u306B\u89E3\u653E\u6E08\u307F\u3067\u3059\u3002";
            ShowFurnitureUnlockPanel();
            return;
        }

        if (GetFoxAltarLevel() < definition.RequiredFoxAltarLevel)
        {
            furnitureFeedbackMessage = "\u72D0\u306E\u4F9B\u53F0\u30EC\u30D9\u30EB\u304C\u8DB3\u308A\u307E\u305B\u3093\u3002";
            ShowFurnitureUnlockPanel();
            return;
        }

        ResourceInventory inventory = ResolveResourceInventory();

        if (inventory == null || !inventory.SpendFaithPoints(definition.UnlockFaithCost))
        {
            furnitureFeedbackMessage = "\u4FE1\u4EF0\u5024\u304C\u8DB3\u308A\u307E\u305B\u3093\u3002";
            ShowFurnitureUnlockPanel();
            return;
        }

        PlayerPrefs.SetInt(FurnitureUnlockKeyPrefix + definition.FurnitureId, 1);
        PlayerPrefs.Save();
        furnitureFeedbackMessage = $"{definition.DisplayName}\u3092\u89E3\u653E\u3057\u307E\u3057\u305F\u3002";
        RefreshFixedFurnitureDisplays(true);
        RefreshFurnitureSeatAnchors();
        ShowFurnitureUnlockPanel();
    }

    private FurnitureDefinition GetNextLockedFurnitureDefinition()
    {
        for (int i = 0; i < CafeFurnitureDefinitions.Length; i++)
        {
            FurnitureDefinition definition = CafeFurnitureDefinitions[i];

            if (definition.IsDefaultUnlocked || IsFurnitureUnlocked(definition.FurnitureId))
            {
                continue;
            }

            if (GetFoxAltarLevel() < definition.RequiredFoxAltarLevel)
            {
                continue;
            }

            return definition;
        }

        return null;
    }

    private FurnitureDefinition GetFurnitureDefinition(string furnitureId)
    {
        if (string.IsNullOrEmpty(furnitureId))
        {
            return null;
        }

        for (int i = 0; i < CafeFurnitureDefinitions.Length; i++)
        {
            if (CafeFurnitureDefinitions[i].FurnitureId == furnitureId)
            {
                return CafeFurnitureDefinitions[i];
            }
        }

        return null;
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
        RefreshFixedFurnitureDisplays(true);
        RefreshFurnitureSeatAnchors();
        ShowFoxAltarPanel();
    }

    private int GetFoxAltarLevel()
    {
        return GetStoredFoxAltarLevel();
    }

    public static int GetStoredFoxAltarLevel()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(FoxAltarLevelKey, 1), 1, MaxFoxAltarLevel);
    }

    public static float GetProductionSpeedMultiplier(int foxAltarLevel)
    {
        switch (Mathf.Clamp(foxAltarLevel, 1, MaxFoxAltarLevel))
        {
            case 2:
                return 1.15f;
            case 3:
                return 1.3f;
            case 4:
                return 1.5f;
            default:
                return 1f;
        }
    }

    public static int GetProductionOutputAmount(int foxAltarLevel)
    {
        return Mathf.Clamp(foxAltarLevel, 1, MaxFoxAltarLevel) >= 4 ? 2 : 1;
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

    private string BuildProductionUpgradeText(int currentLevel)
    {
        float speedMultiplier = GetProductionSpeedMultiplier(currentLevel);
        int outputAmount = GetProductionOutputAmount(currentLevel);
        string nextEffect = currentLevel < MaxFoxAltarLevel
            ? BuildNextProductionUpgradePreview(currentLevel + 1)
            : "次の制作強化: 後日追加予定";

        return $"制作強化: 速度 x{speedMultiplier:0.00} / 完成品 x{outputAmount}\n{nextEffect}";
    }

    private string BuildNextProductionUpgradePreview(int targetLevel)
    {
        float speedMultiplier = GetProductionSpeedMultiplier(targetLevel);
        int outputAmount = GetProductionOutputAmount(targetLevel);
        return $"次の制作強化: 速度 x{speedMultiplier:0.00} / 完成品 x{outputAmount}";
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

        if (!isVisible)
        {
            SetInfoSecondaryActionVisible(false);
        }
    }

    private void SetInfoSecondaryActionVisible(bool isVisible)
    {
        if (infoSecondaryActionButtonObject != null)
        {
            infoSecondaryActionButtonObject.SetActive(isVisible);
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

    private void ConfigureInfoSecondaryActionButton(string label, UnityEngine.Events.UnityAction action, bool isInteractable)
    {
        if (infoSecondaryActionButton == null)
        {
            return;
        }

        infoSecondaryActionButton.onClick.RemoveAllListeners();

        if (action != null)
        {
            infoSecondaryActionButton.onClick.AddListener(action);
        }

        infoSecondaryActionButton.interactable = isInteractable;

        if (infoSecondaryActionButtonText != null)
        {
            infoSecondaryActionButtonText.text = label;
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

    private CafeProductionPopupController ResolveCafeProductionPopupController(bool createIfMissing)
    {
        if (cafeProductionPopupController != null)
        {
            return cafeProductionPopupController;
        }

        cafeProductionPopupController = GetComponent<CafeProductionPopupController>();

        if (cafeProductionPopupController == null && createIfMissing)
        {
            cafeProductionPopupController = gameObject.AddComponent<CafeProductionPopupController>();
        }

        return cafeProductionPopupController;
    }

    private CafeOperationPanelController ResolveCafeOperationPanelController(bool warnIfMissing)
    {
        if (cafeOperationPanelController != null)
        {
            return cafeOperationPanelController;
        }

        cafeOperationPanelController = GetComponent<CafeOperationPanelController>();

        if (cafeOperationPanelController == null && warnIfMissing)
        {
            Debug.LogWarning("CafeOperationPanelController is not assigned. Bind the production popup controller in the Inspector before using the front counter production UI.");
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

    private class FurnitureDefinition
    {
        public string FurnitureId { get; }
        public string DisplayName { get; }
        public string SpritePath { get; }
        public string FixedSlotId { get; }
        public int UnlockFaithCost { get; }
        public bool IsDefaultUnlocked { get; }
        public int RequiredFoxAltarLevel { get; }
        public string Description { get; }

        public FurnitureDefinition(
            string furnitureId,
            string displayName,
            string spritePath,
            string fixedSlotId,
            int unlockFaithCost,
            bool isDefaultUnlocked,
            int requiredFoxAltarLevel,
            string description)
        {
            FurnitureId = furnitureId;
            DisplayName = displayName;
            SpritePath = spritePath;
            FixedSlotId = fixedSlotId;
            UnlockFaithCost = Mathf.Max(0, unlockFaithCost);
            IsDefaultUnlocked = isDefaultUnlocked;
            RequiredFoxAltarLevel = Mathf.Max(1, requiredFoxAltarLevel);
            Description = description;
        }
    }

    private class FixedFurnitureDisplayData
    {
        public int RequiredLevel { get; }
        public string UnlockId { get; }
        public string SpritePath { get; }
        public Vector3 WorldPosition { get; }
        public Vector3 WorldScale { get; }
        public int SortingOrder { get; }

        public FixedFurnitureDisplayData(
            int requiredLevel,
            string unlockId,
            string spritePath,
            Vector3 worldPosition,
            Vector3 worldScale,
            int sortingOrder)
        {
            RequiredLevel = requiredLevel;
            UnlockId = unlockId;
            SpritePath = spritePath;
            WorldPosition = worldPosition;
            WorldScale = worldScale;
            SortingOrder = sortingOrder;
        }
    }

    private class FurnitureSeatAnchorData
    {
        public string UnlockId { get; }
        public string SeatName { get; }
        public Vector3 WorldPosition { get; }

        public FurnitureSeatAnchorData(string unlockId, string seatName, Vector3 worldPosition)
        {
            UnlockId = unlockId;
            SeatName = seatName;
            WorldPosition = worldPosition;
        }
    }

    private class CounterMachineDisplayData
    {
        public string ObjectName { get; }
        public string SpritePath { get; }
        public Vector3 WorldPosition { get; }
        public Vector3 WorldScale { get; }
        public int SortingOrder { get; }

        public CounterMachineDisplayData(
            string objectName,
            string spritePath,
            Vector3 worldPosition,
            Vector3 worldScale,
            int sortingOrder)
        {
            ObjectName = objectName;
            SpritePath = spritePath;
            WorldPosition = worldPosition;
            WorldScale = worldScale;
            SortingOrder = sortingOrder;
        }
    }
}
