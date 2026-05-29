using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private PlayerController player;
    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private ResourceInventory resourceInventory;
    [SerializeField] private Transform startPoint;
    [SerializeField] private float fallRetryY = -5.5f;
    [SerializeField] private string stageClearTitle = "Stage Clear!";
    [SerializeField] private string stageClearMessage = "You reached the shrine gate.";
    [SerializeField] private string stageClearContinueText = "Continue";
    [SerializeField] private string continueSceneName = "HubMap_Day";
    [SerializeField] private Sprite faithPointIcon;
    [SerializeField] private Sprite starSealIcon;
    [SerializeField] private string starSealLabel = "Star Seals";
    [SerializeField] private int starSealTargetCount = 3;

    private GameObject retryPanel;
    private GameObject stageClearPanel;
    private Text faithPointText;
    private Text starSealText;
    private int starSealCount;
    private bool retryVisible;
    private bool stageClearVisible;

    private void Awake()
    {
        Instance = this;

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
        }

        if (playerBody == null && player != null)
        {
            playerBody = player.GetComponent<Rigidbody2D>();
        }

        if (playerHealth == null && player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }

        ResolveResourceInventory();

        EnsureRetryUi();
        EnsureStageClearUi();
        EnsureFaithPointUi();
        EnsureStarSealUi();
        HideRetryUi();
        HideStageClearUi();
        playerHealth?.RefreshHealthUi();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (retryVisible || stageClearVisible || player == null)
        {
            return;
        }

        if (player.transform.position.y <= fallRetryY)
        {
            PlayerFell();
        }
    }

    public void PlayerFell()
    {
        if (retryVisible || stageClearVisible)
        {
            return;
        }

        retryVisible = true;
        EnsureRetryUi();
        GameAudio.EnterRetryAudioState();
        GameAudio.PlayRetryFall();

        if (player != null)
        {
            player.SetControlEnabled(false);
        }

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.simulated = false;
        }

        if (retryPanel != null)
        {
            retryPanel.SetActive(true);
            retryPanel.transform.SetAsLastSibling();
        }
    }

    public void ShowStageClear()
    {
        if (stageClearVisible || retryVisible)
        {
            return;
        }

        stageClearVisible = true;
        EnsureStageClearUi();
        GameAudio.ExitRetryAudioState();
        GameAudio.PlayStageClear();

        if (player != null)
        {
            player.SetControlEnabled(false);
        }

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.simulated = false;
        }

        if (stageClearPanel != null)
        {
            stageClearPanel.SetActive(true);
            stageClearPanel.transform.SetAsLastSibling();
        }
    }

    public void Retry()
    {
        HideRetryUi();
        HideStageClearUi();
        ResetPlayer();
        GameAudio.ExitRetryAudioState();
    }

    public void ContinueAfterStageClear()
    {
        if (string.IsNullOrEmpty(continueSceneName))
        {
            return;
        }

        SceneManager.LoadScene(continueSceneName);
    }

    public void AddFaithPoints(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        ResourceInventory inventory = ResolveResourceInventory();

        if (inventory != null)
        {
            inventory.AddFaithPoints(amount);
        }

        EnsureFaithPointUi();
        UpdateFaithPointUi();
    }

    public bool SpendFaithPoints(int amount)
    {
        ResourceInventory inventory = ResolveResourceInventory();

        if (inventory == null)
        {
            return false;
        }

        bool spent = inventory.SpendFaithPoints(amount);
        UpdateFaithPointUi();
        return spent;
    }

    public int GetFaithPointCount()
    {
        ResourceInventory inventory = ResolveResourceInventory();
        return inventory != null ? inventory.FaithPoints : 0;
    }

    public void AddStarSeals(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        starSealCount += amount;
        EnsureStarSealUi();
        UpdateStarSealUi();
    }

    private void ResetPlayer()
    {
        if (player == null || startPoint == null)
        {
            return;
        }

        player.transform.position = startPoint.position;

        if (playerBody != null)
        {
            playerBody.simulated = true;
            playerBody.linearVelocity = Vector2.zero;
        }

        player.ResetMotion();

        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }

        player.SetControlEnabled(true);
    }

    private void HideRetryUi()
    {
        retryVisible = false;

        if (retryPanel != null)
        {
            retryPanel.SetActive(false);
        }
    }

    private void HideStageClearUi()
    {
        stageClearVisible = false;

        if (stageClearPanel != null)
        {
            stageClearPanel.SetActive(false);
        }
    }

    private void CreateRetryUi()
    {
        if (retryPanel != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("RetryCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasObject.AddComponent<GraphicRaycaster>();

        retryPanel = new GameObject("RetryPanel");
        retryPanel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = retryPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(260f, 150f);

        Image panelImage = retryPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        CreateText("Retry?", retryPanel.transform, new Vector2(0f, 35f), 30);
        CreateRetryButton(retryPanel.transform);
    }

    private void CreateRetryButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("RetryButton");
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -30f);
        buttonRect.sizeDelta = new Vector2(140f, 44f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(Retry);

        Text buttonText = CreateText("Retry", buttonObject.transform, Vector2.zero, 22);
        buttonText.color = Color.black;
    }

    private void CreateStageClearUi()
    {
        if (stageClearPanel != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("StageClearCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasObject.AddComponent<GraphicRaycaster>();

        stageClearPanel = new GameObject("StageClearPanel");
        stageClearPanel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = stageClearPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(340f, 190f);

        Image panelImage = stageClearPanel.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.08f, 0.12f, 0.78f);

        CreateText(stageClearTitle, stageClearPanel.transform, new Vector2(0f, 48f), 30);
        CreateText(stageClearMessage, stageClearPanel.transform, new Vector2(0f, 4f), 18);
        CreateContinueButton(stageClearPanel.transform);
    }

    private void CreateContinueButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("ContinueButton");
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -58f);
        buttonRect.sizeDelta = new Vector2(150f, 42f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.86f, 0.82f, 0.72f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(ContinueAfterStageClear);

        Text buttonText = CreateText(stageClearContinueText, buttonObject.transform, Vector2.zero, 20);
        buttonText.color = Color.black;
    }

    private void CreateFaithPointUi()
    {
        if (faithPointText != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("FaithPointCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 89;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        GameObject textObject = new GameObject("FaithPointText");
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = faithPointIcon != null ? new Vector2(66f, -88f) : new Vector2(26f, -88f);
        textRect.sizeDelta = new Vector2(100f, 34f);

        if (faithPointIcon != null)
        {
            CreateFaithPointIcon(canvasObject.transform);
        }

        faithPointText = textObject.AddComponent<Text>();
        faithPointText.alignment = TextAnchor.MiddleLeft;
        faithPointText.fontSize = 24;
        faithPointText.color = new Color(1f, 0.86f, 0.36f, 1f);
        faithPointText.font = GetUiFont();
        faithPointText.raycastTarget = false;

        UpdateFaithPointUi();
    }

    private void CreateStarSealUi()
    {
        if (starSealText != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("StarSealCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 88;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        GameObject textObject = new GameObject("StarSealText");
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = starSealIcon != null ? new Vector2(66f, -126f) : new Vector2(26f, -126f);
        textRect.sizeDelta = new Vector2(100f, 34f);

        if (starSealIcon != null)
        {
            CreateStarSealIcon(canvasObject.transform);
        }

        starSealText = textObject.AddComponent<Text>();
        starSealText.alignment = TextAnchor.MiddleLeft;
        starSealText.fontSize = 24;
        starSealText.color = new Color(0.6f, 0.95f, 1f, 1f);
        starSealText.font = GetUiFont();
        starSealText.raycastTarget = false;

        UpdateStarSealUi();
    }

    private void CreateFaithPointIcon(Transform parent)
    {
        GameObject iconObject = new GameObject("FaithPointIcon");
        iconObject.transform.SetParent(parent, false);

        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.anchoredPosition = new Vector2(24f, -82f);
        iconRect.sizeDelta = new Vector2(34f, 34f);

        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.sprite = faithPointIcon;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
    }

    private void CreateStarSealIcon(Transform parent)
    {
        GameObject iconObject = new GameObject("StarSealIcon");
        iconObject.transform.SetParent(parent, false);

        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.anchoredPosition = new Vector2(24f, -120f);
        iconRect.sizeDelta = new Vector2(32f, 32f);

        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.sprite = starSealIcon;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
    }

    private void UpdateFaithPointUi()
    {
        if (faithPointText != null)
        {
            faithPointText.text = GetFaithPointCount().ToString();
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

    private void UpdateStarSealUi()
    {
        if (starSealText != null)
        {
            if (starSealTargetCount > 0)
            {
                starSealText.text = $"{starSealCount}/{starSealTargetCount}";
            }
            else
            {
                starSealText.text = starSealCount.ToString();
            }
        }
    }

    private Text CreateText(string text, Transform parent, Vector2 position, int size)
    {
        GameObject textObject = new GameObject(text);
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = position;
        textRect.sizeDelta = new Vector2(220f, 44f);

        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.fontSize = size;
        uiText.color = Color.white;
        uiText.font = GetUiFont();

        return uiText;
    }

    private Font GetUiFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont("Arial", 18);
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

    private void EnsureRetryUi()
    {
        EnsureEventSystem();
        CreateRetryUi();
    }

    private void EnsureStageClearUi()
    {
        EnsureEventSystem();
        CreateStageClearUi();
    }

    private void EnsureFaithPointUi()
    {
        CreateFaithPointUi();
    }

    private void EnsureStarSealUi()
    {
        CreateStarSealUi();
    }
}
