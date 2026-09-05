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
        new FurnitureUnlockData(2, "furniture_small_flower_table", "小さな花卓", "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_flower_chest.png"),
        new FurnitureUnlockData(3, "furniture_sofa_double_up", "二人掛けソファ 上", "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_sofa_table_set.png"),
        new FurnitureUnlockData(4, "furniture_shrine_lamp", "神社の小灯", "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_tall_lantern.png"),
        new FurnitureUnlockData(4, "furniture_torii_small", "小さな鳥居", "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_long_shrine_runner.png")
    };
    private static readonly FixedFurnitureDisplayData[] FixedFurnitureDisplays =
    {
        new FixedFurnitureDisplayData(
            1,
            "counter_decoration",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_bottle_shelf.png",
            new Vector3(1.72f, 2.58f, 0f),
            new Vector3(0.22f, 0.22f, 1f),
            5),
        new FixedFurnitureDisplayData(
            1,
            "wall_menu_board",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_menu_board.png",
            new Vector3(-3.28f, 2.64f, 0f),
            new Vector3(0.22f, 0.22f, 1f),
            5),
        new FixedFurnitureDisplayData(
            1,
            "potted_plant",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_potted_plant.png",
            new Vector3(-5.72f, 0.85f, 0f),
            new Vector3(0.24f, 0.24f, 1f),
            2),
        new FixedFurnitureDisplayData(
            1,
            "coffee_table_basic",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_low_table_tea.png",
            new Vector3(-4.62f, -0.62f, 0f),
            new Vector3(0.4f, 0.4f, 1f),
            2),
        new FixedFurnitureDisplayData(
            2,
            "dessert_counter",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_dessert_counter.png",
            new Vector3(0.58f, 2.42f, 0f),
            new Vector3(0.2f, 0.2f, 1f),
            5),
        new FixedFurnitureDisplayData(
            2,
            "storage_shelf",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_storage_shelf.png",
            new Vector3(3.46f, 2.62f, 0f),
            new Vector3(0.22f, 0.22f, 1f),
            5),
        new FixedFurnitureDisplayData(
            2,
            "furniture_small_flower_table",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_flower_chest.png",
            new Vector3(5.38f, 0.72f, 0f),
            new Vector3(0.26f, 0.26f, 1f),
            2),
        new FixedFurnitureDisplayData(
            2,
            "table_two_cushions",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_table_two_cushions.png",
            new Vector3(4.38f, -0.48f, 0f),
            new Vector3(0.46f, 0.46f, 1f),
            2),
        new FixedFurnitureDisplayData(
            2,
            "tatami_table_set",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_tatami_table_set.png",
            new Vector3(-4.76f, -2.22f, 0f),
            new Vector3(0.42f, 0.42f, 1f),
            2),
        new FixedFurnitureDisplayData(
            2,
            "sofa_front",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_sofa_front_a.png",
            new Vector3(-4.62f, -0.04f, 0f),
            new Vector3(0.48f, 0.48f, 1f),
            2),
        new FixedFurnitureDisplayData(
            2,
            "sofa_back",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_sofa_back.png",
            new Vector3(-4.62f, -1.22f, 0f),
            new Vector3(0.48f, 0.48f, 1f),
            2),
        new FixedFurnitureDisplayData(
            2,
            "sofa_left",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_sofa_side_left.png",
            new Vector3(-5.35f, -0.62f, 0f),
            new Vector3(0.48f, 0.48f, 1f),
            2),
        new FixedFurnitureDisplayData(
            2,
            "sofa_right",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_sofa_side_right.png",
            new Vector3(-3.9f, -0.62f, 0f),
            new Vector3(0.48f, 0.48f, 1f),
            2),
        new FixedFurnitureDisplayData(
            3,
            "furniture_sofa_double_up",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_sofa_table_set.png",
            new Vector3(4.26f, -2.02f, 0f),
            new Vector3(0.58f, 0.58f, 1f),
            2),
        new FixedFurnitureDisplayData(
            4,
            "furniture_shrine_lamp",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_tall_lantern.png",
            new Vector3(-5.78f, -2.64f, 0f),
            new Vector3(0.22f, 0.22f, 1f),
            2),
        new FixedFurnitureDisplayData(
            4,
            "furniture_torii_small",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_long_shrine_runner.png",
            new Vector3(0f, -2.84f, 0f),
            new Vector3(0.44f, 0.44f, 1f),
            1)
    };
    private static readonly FurnitureSeatAnchorData[] FurnitureSeatAnchors =
    {
        new FurnitureSeatAnchorData("furniture_sofa_double_up", "GuestSeat_05", new Vector3(4.26f, -1.62f, 0f))
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
            "\u53F3\u5411\u304D\u306E\u30BD\u30D5\u30A1\u3002"),
        new FurnitureDefinition(
            "wall_menu_board",
            "\u30E1\u30CB\u30E5\u30FC\u9ED2\u677F",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_menu_board.png",
            "back_wall",
            4,
            false,
            1,
            "\u58C1\u306B\u5C0F\u3055\u306A\u55B6\u696D\u306E\u6C17\u914D\u3092\u8DB3\u3059\u9ED2\u677F\u3002"),
        new FurnitureDefinition(
            "potted_plant",
            "\u89B3\u8449\u690D\u7269",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_potted_plant.png",
            "left_corner",
            3,
            false,
            1,
            "\u6765\u8A2A\u8005\u306E\u547C\u5438\u304C\u5C11\u3057\u697D\u306B\u306A\u308B\u7DD1\u3002"),
        new FurnitureDefinition(
            "dessert_counter",
            "\u83D3\u5B50\u30B1\u30FC\u30B9",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_dessert_counter.png",
            "counter_top",
            6,
            false,
            2,
            "\u591C\u685C\u30B1\u30FC\u30AD\u3092\u4E26\u3079\u308B\u5C0F\u3055\u306A\u30B1\u30FC\u30B9\u3002"),
        new FurnitureDefinition(
            "storage_shelf",
            "\u6750\u6599\u68DA",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_storage_shelf.png",
            "back_wall",
            7,
            false,
            2,
            "\u30AB\u30D5\u30A7\u306E\u6750\u6599\u3092\u6574\u3048\u308B\u68DA\u3002"),
        new FurnitureDefinition(
            "table_two_cushions",
            "\u4E8C\u4EBA\u7528\u5EA7\u5353",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_table_two_cushions.png",
            "right_lounge",
            7,
            false,
            2,
            "\u4E8C\u4EBA\u306E\u6765\u8A2A\u8005\u304C\u9759\u304B\u306B\u8A71\u305B\u308B\u5EA7\u5353\u3002"),
        new FurnitureDefinition(
            "tatami_table_set",
            "\u7573\u306E\u5C0F\u5E2D",
            "Assets/Art/cafe_icon/cafe_icons_cutouts/furniture_tatami_table_set.png",
            "left_lounge",
            8,
            false,
            2,
            "\u5C11\u3057\u9577\u304F\u4F11\u3081\u308B\u7573\u306E\u5C0F\u5E2D\u3002")
    };
    private static readonly CounterMachineDisplayData[] CounterMachineDisplays =
    {
        new CounterMachineDisplayData(
            "CafeCounter_CoffeeMachine",
            "Assets/Art/cafe_icon/coffe_mechine_cutout.png",
            new Vector3(-1.58f, 2.63f, 0f),
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
    [SerializeField] private string frontCounterInteractionObjectName = "FrontCounterMachineInteraction";
    [SerializeField] private Vector2 frontCounterInteractionLocalPosition = new Vector2(-1.08f, 0.35f);
    [SerializeField] private Vector2 frontCounterInteractionSize = new Vector2(0.82f, 1.08f);
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
    private GameObject furnitureCatalogRoot;
    private Transform cafePlayer;
    private CafeOperationController cafeOperationController;
    [SerializeField] private CafeOperationPanelController cafeOperationPanelController;
    [SerializeField] private CafeProductionPopupController cafeProductionPopupController;
    private readonly Dictionary<string, Sprite> furniturePreviewSpriteCache = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, GameObject> fixedFurnitureObjects = new Dictionary<string, GameObject>();
    private readonly HashSet<string> loggedMissingFurniturePreviewSprites = new HashSet<string>();
    private GameObject fixedFurnitureRoot;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private Coroutine debugFurnitureDropCoroutine;
#endif
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
        ConfigureInfoBodyDefault();
        SetFurnitureCatalogVisible(false);
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
            SetFurnitureCatalogVisible(false);
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
            SetFurnitureCatalogVisible(false);
            panelController.Initialize(cafeCanvasObject.transform, operationController);
            panelController.Show();
            return;
        }

        infoTitle.text = "夜神社カフェ 営業";
        ConfigureInfoBodyDefault();
        infoBody.text = BuildGuestSeatSummary();
        SetInfoActionVisible(false);
        SetFurniturePreviewVisible(false);
        SetFurnitureCatalogVisible(false);
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
            SetFurnitureCatalogVisible(false);
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

        ConfigureInfoBodyDefault();
        infoTitle.text = OpenBusinessLabel;
        infoBody.text = opened ? BusinessOpenedMessage : AlreadyBusinessOpenedMessage;
        SetInfoActionVisible(false);
        SetFurniturePreviewVisible(false);
        SetFurnitureCatalogVisible(false);
        ConfigureInfoCloseButton("Close", HideInfoPanel);
        infoPanel.SetActive(true);
    }

    private void ShowMenuBoardMessages()
    {
        CafeOperationController operationController = ResolveCafeOperationController();
        string summary = operationController != null ? operationController.BuildMessageBoardSummary() : string.Empty;

        HideMenuBoardActionPanel();
        ConfigureInfoBodyDefault();
        infoTitle.text = VisitorMessageTitle;
        infoBody.text = string.IsNullOrWhiteSpace(summary) ? EmptyVisitorMessage : summary;
        SetInfoActionVisible(false);
        SetFurniturePreviewVisible(false);
        SetFurnitureCatalogVisible(false);
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
            SetFurnitureCatalogVisible(false);
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

        ConfigureInfoBodyDefault();
        infoTitle.text = "今日のカフェ記録";
        infoBody.text = operationController != null
            ? operationController.BuildCafeDayResultSummary()
            : "今日も、少しだけ灯りが増えました。\n\n来訪者: 0人\n信仰値: +0\nこころ狐: +0\n好感度アップ: 0人\n解放された家具: 新しい家具はありません";

        SetInfoActionVisible(false);
        SetFurniturePreviewVisible(false);
        SetFurnitureCatalogVisible(false);
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
            "閉じる",
            new Vector2(0f, -228f),
            new Vector2(160f, 42f),
            HideInfoPanel,
            out infoCloseButton,
            out infoCloseButtonText);
        infoCloseButton.interactable = true;
        infoCloseButtonText.color = new Color32(0x3A, 0x24, 0x1C, 0xFF);

        CreateFurniturePreviewRoot();
        CreateFurnitureCatalogRoot();
        SetInfoActionVisible(false);
        SetInfoSecondaryActionVisible(false);
        SetFurniturePreviewVisible(false);
        SetFurnitureCatalogVisible(false);
        infoPanel.SetActive(false);
    }

    private void ConfigureInfoBodyDefault()
    {
        ConfigureInfoBodyLayout(
            new Vector2(0f, 98f),
            new Vector2(500f, 224f),
            17,
            TextAnchor.MiddleCenter);
    }

    private void ConfigureInfoBodyForFurnitureCatalog()
    {
        ConfigureInfoBodyLayout(
            new Vector2(0f, 166f),
            new Vector2(500f, 42f),
            16,
            TextAnchor.MiddleCenter);
    }

    private void ConfigureInfoBodyLayout(Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
    {
        if (infoBody == null)
        {
            return;
        }

        RectTransform bodyRect = infoBody.rectTransform;
        bodyRect.anchoredPosition = position;
        bodyRect.sizeDelta = size;
        infoBody.fontSize = fontSize;
        infoBody.alignment = alignment;
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
        buttonText.color = new Color32(0x3A, 0x24, 0x1C, 0xFF);

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

    private void CreateFurnitureCatalogRoot()
    {
        furnitureCatalogRoot = new GameObject("FurnitureCatalogRoot");
        furnitureCatalogRoot.transform.SetParent(infoPanel.transform, false);

        RectTransform catalogRect = furnitureCatalogRoot.AddComponent<RectTransform>();
        catalogRect.anchorMin = new Vector2(0.5f, 0.5f);
        catalogRect.anchorMax = new Vector2(0.5f, 0.5f);
        catalogRect.pivot = new Vector2(0.5f, 0.5f);
        catalogRect.anchoredPosition = new Vector2(0f, -34f);
        catalogRect.sizeDelta = new Vector2(540f, 306f);

        Image catalogBackground = furnitureCatalogRoot.AddComponent<Image>();
        catalogBackground.color = new Color(0.13f, 0.08f, 0.045f, 0.78f);

        Outline catalogOutline = furnitureCatalogRoot.AddComponent<Outline>();
        catalogOutline.effectColor = new Color(0.77f, 0.58f, 0.35f, 0.95f);
        catalogOutline.effectDistance = new Vector2(2f, -2f);
    }

    private void RefreshFurnitureCatalog(ResourceInventory inventory)
    {
        if (furnitureCatalogRoot == null)
        {
            return;
        }

        ClearFurnitureCatalog();

        for (int i = 0; i < CafeFurnitureDefinitions.Length; i++)
        {
            CreateFurnitureCatalogCard(CafeFurnitureDefinitions[i], i, inventory);
        }
    }

    private void CreateFurnitureCatalogCard(FurnitureDefinition definition, int index, ResourceInventory inventory)
    {
        if (definition == null || furnitureCatalogRoot == null)
        {
            return;
        }

        const int columns = 3;
        Vector2 cardSize = new Vector2(168f, 58f);
        int row = index / columns;
        int column = index % columns;
        Vector2 cardPosition = new Vector2(-176f + column * 176f, 116f - row * 64f);
        bool isUnlocked = IsFurnitureUnlocked(definition.FurnitureId);
        bool levelReady = GetFoxAltarLevel() >= definition.RequiredFoxAltarLevel;
        bool canAfford = inventory != null && inventory.FaithPoints >= definition.UnlockFaithCost;
        bool canUnlock = !definition.IsDefaultUnlocked && !isUnlocked && levelReady && canAfford;

        GameObject cardObject = new GameObject($"FurnitureCard_{definition.FurnitureId}");
        cardObject.transform.SetParent(furnitureCatalogRoot.transform, false);

        RectTransform cardRect = cardObject.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = cardPosition;
        cardRect.sizeDelta = cardSize;

        Image cardImage = cardObject.AddComponent<Image>();
        cardImage.color = isUnlocked
            ? new Color(0.47f, 0.31f, 0.16f, 0.92f)
            : new Color(0.26f, 0.18f, 0.12f, 0.9f);

        Outline cardOutline = cardObject.AddComponent<Outline>();
        cardOutline.effectColor = isUnlocked
            ? new Color(0.95f, 0.74f, 0.42f, 0.9f)
            : new Color(0.58f, 0.42f, 0.28f, 0.86f);
        cardOutline.effectDistance = new Vector2(1f, -1f);

        CreateFurnitureCatalogIcon(definition, cardObject.transform);

        Text nameText = CreateText("FurnitureName", cardObject.transform, new Vector2(10f, 15f), new Vector2(78f, 20f), 12);
        nameText.text = definition.DisplayName;
        nameText.alignment = TextAnchor.MiddleLeft;

        Text stateText = CreateText("FurnitureState", cardObject.transform, new Vector2(10f, -9f), new Vector2(78f, 32f), 10);
        stateText.text = BuildFurnitureCatalogStateText(definition, isUnlocked, levelReady);
        stateText.alignment = TextAnchor.MiddleLeft;
        stateText.color = isUnlocked
            ? new Color(1f, 0.91f, 0.62f, 1f)
            : new Color(0.85f, 0.78f, 0.68f, 1f);

        CreateFurnitureCatalogButton(definition, cardObject.transform, canUnlock, isUnlocked, levelReady, canAfford);
    }

    private void CreateFurnitureCatalogIcon(FurnitureDefinition definition, Transform parent)
    {
        GameObject iconObject = new GameObject("FurnitureIcon");
        iconObject.transform.SetParent(parent, false);

        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-62f, 2f);
        iconRect.sizeDelta = new Vector2(42f, 42f);

        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.sprite = LoadFurniturePreviewSprite(definition.SpritePath);
        iconImage.preserveAspect = true;
        iconImage.color = iconImage.sprite != null ? Color.white : new Color(0.38f, 0.28f, 0.2f, 0.8f);

        if (iconImage.sprite == null)
        {
            Text fallbackText = CreateText("FurnitureIconFallback", iconObject.transform, Vector2.zero, new Vector2(48f, 36f), 11);
            fallbackText.text = "\u5BB6\u5177";
            fallbackText.color = new Color(0.9f, 0.82f, 0.68f, 1f);
        }
    }

    private void CreateFurnitureCatalogButton(
        FurnitureDefinition definition,
        Transform parent,
        bool canUnlock,
        bool isUnlocked,
        bool levelReady,
        bool canAfford)
    {
        GameObject buttonObject = new GameObject("FurnitureUnlockButton");
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(54f, -14f);
        buttonRect.sizeDelta = new Vector2(56f, 26f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = canUnlock
            ? new Color(0.83f, 0.62f, 0.32f, 1f)
            : new Color(0.42f, 0.34f, 0.27f, 0.92f);

        Button button = buttonObject.AddComponent<Button>();
        button.interactable = canUnlock;

        if (canUnlock)
        {
            string furnitureId = definition.FurnitureId;
            button.onClick.AddListener(() => TryUnlockFurniture(furnitureId));
        }

        Text buttonText = CreateText("Label", buttonObject.transform, Vector2.zero, new Vector2(52f, 22f), 11);
        buttonText.text = BuildFurnitureCatalogButtonLabel(definition, isUnlocked, levelReady, canAfford);
        buttonText.color = canUnlock ? Color.black : new Color(0.78f, 0.72f, 0.66f, 1f);
    }

    private string BuildFurnitureCatalogStateText(FurnitureDefinition definition, bool isUnlocked, bool levelReady)
    {
        if (isUnlocked)
        {
            return "\u89E3\u653E\u6E08\u307F\n" + definition.FixedSlotId;
        }

        if (!levelReady)
        {
            return $"\u4F9B\u53F0Lv.{definition.RequiredFoxAltarLevel}\n{definition.FixedSlotId}";
        }

        return $"{definition.UnlockFaithCost}\u4FE1\u4EF0\u5024\n{definition.FixedSlotId}";
    }

    private string BuildFurnitureCatalogButtonLabel(
        FurnitureDefinition definition,
        bool isUnlocked,
        bool levelReady,
        bool canAfford)
    {
        if (isUnlocked || definition.IsDefaultUnlocked)
        {
            return "\u89E3\u653E\u6E08";
        }

        if (!levelReady)
        {
            return $"Lv.{definition.RequiredFoxAltarLevel}";
        }

        return canAfford ? "\u89E3\u653E" : "\u4E0D\u8DB3";
    }

    private void ClearFurnitureCatalog()
    {
        if (furnitureCatalogRoot == null)
        {
            return;
        }

        for (int i = furnitureCatalogRoot.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(furnitureCatalogRoot.transform.GetChild(i).gameObject);
        }
    }

    private void SetFurnitureCatalogVisible(bool isVisible)
    {
        if (furnitureCatalogRoot != null)
        {
            furnitureCatalogRoot.SetActive(isVisible);
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
        const float animationSeconds = 0.72f;
        float elapsed = 0f;
        Vector3 startPosition = targetPosition + new Vector3(0f, 0.85f, 0f);
        Vector3 overshootPosition = targetPosition + new Vector3(0f, -0.08f, 0f);
        Color targetColor = spriteRenderer.color;
        Color startColor = targetColor;
        startColor.a = 0f;

        furnitureTransform.position = startPosition;
        furnitureTransform.localScale = targetScale * 0.65f;
        spriteRenderer.color = startColor;

        while (elapsed < animationSeconds)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / animationSeconds);
            float dropT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.72f));
            float settleT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((progress - 0.72f) / 0.28f));
            Vector3 dropPosition = Vector3.Lerp(startPosition, overshootPosition, dropT);
            float scaleMultiplier = progress < 0.72f
                ? Mathf.Lerp(0.65f, 1.08f, dropT)
                : Mathf.Lerp(1.08f, 1f, settleT);

            furnitureTransform.position = Vector3.Lerp(dropPosition, targetPosition, settleT);
            furnitureTransform.localScale = targetScale * scaleMultiplier;
            spriteRenderer.color = Color.Lerp(startColor, targetColor, Mathf.SmoothStep(0f, 1f, progress));
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
        SetupFrontCounterMachineInteraction(GameObject.Find(counterObjectName));
        SetupInteraction(GameObject.Find(foxAltarObjectName), CafeInteractionType.FoxAltar);
        SetupInteraction(GameObject.Find(MenuBoardObjectName), CafeInteractionType.MenuBoard);
    }

    private void SetupFrontCounterMachineInteraction(GameObject counterObject)
    {
        if (counterObject == null)
        {
            return;
        }

        // Keep the counter's wide collider for player blocking, but bind clicks only to the black machine.
        CafeInteractable legacyCounterInteraction = counterObject.GetComponent<CafeInteractable>();
        if (legacyCounterInteraction != null)
        {
            Destroy(legacyCounterInteraction);
        }

        Transform existingTarget = counterObject.transform.Find(frontCounterInteractionObjectName);
        GameObject interactionTarget = existingTarget != null
            ? existingTarget.gameObject
            : new GameObject(frontCounterInteractionObjectName);

        interactionTarget.transform.SetParent(counterObject.transform, false);
        interactionTarget.transform.localPosition = new Vector3(
            frontCounterInteractionLocalPosition.x,
            frontCounterInteractionLocalPosition.y,
            -0.05f);
        interactionTarget.transform.localRotation = Quaternion.identity;
        interactionTarget.transform.localScale = Vector3.one;

        BoxCollider2D interactionCollider = interactionTarget.GetComponent<BoxCollider2D>();
        if (interactionCollider == null)
        {
            interactionCollider = interactionTarget.AddComponent<BoxCollider2D>();
        }

        interactionCollider.isTrigger = true;
        interactionCollider.offset = Vector2.zero;
        interactionCollider.size = frontCounterInteractionSize;

        SetupInteraction(interactionTarget, CafeInteractionType.FrontCounter);
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
        string feedback = string.IsNullOrEmpty(furnitureFeedbackMessage)
            ? string.Empty
            : $"\n\n{furnitureFeedbackMessage}";

        ConfigureInfoBodyForFurnitureCatalog();
        infoTitle.text = "\u5BB6\u5177\u89E3\u653E";
        infoBody.text = BuildFurnitureUnlockPanelHeader(inventory) + feedback;
        SetFurniturePreviewVisible(false);
        RefreshFurnitureCatalog(inventory);
        SetFurnitureCatalogVisible(true);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        SetInfoActionVisible(true);
        ConfigureInfoActionButton("Debug 全解放", DebugUnlockAllCafeFurniture, true);
#else
        SetInfoActionVisible(false);
#endif
        SetInfoSecondaryActionVisible(true);
        ConfigureInfoSecondaryActionButton("\u623B\u308B", ShowFoxAltarPanel, true);
        ConfigureInfoCloseButton("Close", HideInfoPanel);
        BringInfoButtonsToFront();
        infoPanel.SetActive(true);
    }

    private string BuildFurnitureUnlockPanelHeader(ResourceInventory inventory)
    {
        int faithPoints = inventory != null ? inventory.FaithPoints : 0;
        return $"\u4F9B\u53F0Lv.{GetFoxAltarLevel()}  /  \u4FE1\u4EF0\u5024: {faithPoints}";
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void DebugUnlockAllCafeFurniture()
    {
        PlayerPrefs.SetInt(FoxAltarLevelKey, MaxFoxAltarLevel);

        for (int i = 0; i < FoxAltarFurnitureUnlocks.Length; i++)
        {
            PlayerPrefs.SetInt(FurnitureUnlockKeyPrefix + FoxAltarFurnitureUnlocks[i].UnlockId, 1);
        }

        for (int i = 0; i < CafeFurnitureDefinitions.Length; i++)
        {
            PlayerPrefs.SetInt(FurnitureUnlockKeyPrefix + CafeFurnitureDefinitions[i].FurnitureId, 1);
        }

        PlayerPrefs.Save();
        furnitureFeedbackMessage = "Debug: 家具をすべて解放しました。";

        if (debugFurnitureDropCoroutine != null)
        {
            StopCoroutine(debugFurnitureDropCoroutine);
        }

        debugFurnitureDropCoroutine = StartCoroutine(RebuildFixedFurnitureDisplaysSequential());
        RefreshFurnitureSeatAnchors();
        ShowFurnitureUnlockPanel();
    }

    private IEnumerator RebuildFixedFurnitureDisplaysSequential()
    {
        for (int i = 0; i < FixedFurnitureDisplays.Length; i++)
        {
            RemoveFixedFurnitureObject(FixedFurnitureDisplays[i].UnlockId);
        }

        EnsureFixedFurnitureRoot();
        int currentLevel = GetFoxAltarLevel();

        for (int i = 0; i < FixedFurnitureDisplays.Length; i++)
        {
            FixedFurnitureDisplayData displayData = FixedFurnitureDisplays[i];
            bool isUnlocked = displayData.RequiredLevel <= currentLevel && IsFurnitureUnlocked(displayData.UnlockId);

            if (!isUnlocked)
            {
                continue;
            }

            CreateFixedFurnitureObject(displayData, true);
            yield return new WaitForSeconds(0.12f);
        }

        debugFurnitureDropCoroutine = null;
    }
#endif

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

    private void BringInfoButtonsToFront()
    {
        if (infoActionButtonObject != null)
        {
            infoActionButtonObject.transform.SetAsLastSibling();
        }

        if (infoSecondaryActionButtonObject != null)
        {
            infoSecondaryActionButtonObject.transform.SetAsLastSibling();
        }

        if (infoCloseButton != null)
        {
            infoCloseButton.transform.SetAsLastSibling();
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
        NightShrineTextStyle.Apply(uiText, NightShrineTextRole.Body);

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
