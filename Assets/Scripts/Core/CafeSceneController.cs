using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CafeSceneController : MonoBehaviour
{
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
    private Transform cafePlayer;
    private CafeOperationController cafeOperationController;
    private CafeOperationPanelController cafeOperationPanelController;
    private bool isReturningToHub;

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

        infoTitle.text = "狐狸供台";
        infoBody.text = $"Lv.1\n神社の状態: 準備中\n信仰値: {faithPoints}\n強化機能は後日追加予定";
        infoPanel.SetActive(true);
    }

    public void ShowReceptionPanel()
    {
        CafeOperationController operationController = ResolveCafeOperationController();
        CafeOperationPanelController panelController = ResolveCafeOperationPanelController();

        if (operationController != null && panelController != null)
        {
            infoPanel.SetActive(false);
            panelController.Initialize(cafeCanvasObject.transform, operationController);
            panelController.Show();
            return;
        }

        infoTitle.text = "夜神社カフェ 営業";
        infoBody.text = BuildGuestSeatSummary();
        infoPanel.SetActive(true);
    }

    public void HideInfoPanel()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
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
        panelRect.sizeDelta = new Vector2(450f, 310f);

        Image panelImage = infoPanel.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.09f, 0.1f, 0.9f);

        infoTitle = CreateText("Title", infoPanel.transform, new Vector2(0f, 116f), new Vector2(390f, 42f), 28);
        infoBody = CreateText("Body", infoPanel.transform, new Vector2(0f, 28f), new Vector2(390f, 160f), 20);

        GameObject buttonObject = new GameObject("CloseButton");
        buttonObject.transform.SetParent(infoPanel.transform, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -118f);
        buttonRect.sizeDelta = new Vector2(160f, 42f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.86f, 0.82f, 0.72f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(HideInfoPanel);

        Text buttonText = CreateText("Close", buttonObject.transform, Vector2.zero, new Vector2(150f, 36f), 18);
        buttonText.color = Color.black;

        infoPanel.SetActive(false);
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
        string[] labels =
        {
            "参拝客",
            "旅人",
            "小さな妖怪",
            "不思議な常連"
        };

        string summary = "柜台前の4席\n";

        for (int i = 0; i < labels.Length; i++)
        {
            string seatName = $"GuestSeat_{i + 1:00}";
            string seatState = GameObject.Find(seatName) != null ? labels[i] : $"{labels[i]} (座席未接続)";
            summary += $"{seatName}: {seatState}";

            if (i < labels.Length - 1)
            {
                summary += "\n";
            }
        }

        return summary;
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
}
