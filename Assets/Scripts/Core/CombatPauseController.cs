using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CombatPauseController : MonoBehaviour
{
    private const string StartSceneName = "StartScene";
    private const string HubSceneName = "HubMap_Day";
    private const string ResultSceneName = "Result";

    private static CombatPauseController instance;

    [SerializeField] private string returnSceneName = HubSceneName;
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private GameUiTheme uiTheme;

    private Canvas menuCanvas;
    private GameObject pauseOverlay;
    private GameObject mainMenuRoot;
    private GameObject settingsRoot;
    private Button resumeButton;
    private Button closeButton;
    private Button settingsBackButton;
    private Text returnButtonLabel;
    private Text bgmValueLabel;
    private Text sfxValueLabel;
    private Text fullscreenValueLabel;
    private Slider bgmSlider;
    private Slider sfxSlider;
    private EventSystem globalEventSystem;
    private Font uiFont;
    private bool isPaused;
    private bool pauseAllowed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject systemObject = new GameObject("GlobalGameUiSystem");
        systemObject.AddComponent<CombatPauseController>();
        DontDestroyOnLoad(systemObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Time.timeScale = 1f;
        uiTheme = uiTheme != null ? uiTheme : GameUiTheme.LoadDefault();
        EnsureEventSystem();
        CreateGlobalMenuUi();
        SceneManager.activeSceneChanged += HandleSceneChanged;
        ConfigureForScene(SceneManager.GetActiveScene());
        SetPaused(false);
    }

    private void Update()
    {
        if (!pauseAllowed || !Input.GetKeyDown(pauseKey))
        {
            return;
        }

        if (isPaused && settingsRoot != null && settingsRoot.activeSelf)
        {
            ShowMainMenu();
            return;
        }

        if (!isPaused && TryCloseSceneOverlay())
        {
            return;
        }

        if (!isPaused && GameManager.Instance != null && GameManager.Instance.IsBlockingUiVisible)
        {
            return;
        }

        SetPaused(!isPaused);
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        SceneManager.activeSceneChanged -= HandleSceneChanged;
        GameSettings.Changed -= RefreshSettingsValues;
        Time.timeScale = 1f;
        instance = null;
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void ReturnToMap()
    {
        Time.timeScale = 1f;
        GameAudio.ExitRetryAudioState();

        if (!string.IsNullOrWhiteSpace(returnSceneName))
        {
            SceneManager.LoadScene(returnSceneName);
        }
    }

    private void HandleSceneChanged(Scene previousScene, Scene nextScene)
    {
        EnsureEventSystem();
        SetPaused(false);
        ConfigureForScene(nextScene);
    }

    private bool TryCloseSceneOverlay()
    {
        HubFarmPanelController farmPanel = FindAnyObjectByType<HubFarmPanelController>();
        if (farmPanel != null && farmPanel.TryCloseOverlay())
        {
            return true;
        }

        HubIngredientShopController shopPanel = FindAnyObjectByType<HubIngredientShopController>();
        if (shopPanel != null && shopPanel.TryCloseOverlay())
        {
            return true;
        }

        HubMapController hubMap = FindAnyObjectByType<HubMapController>();
        return hubMap != null && hubMap.TryCloseOverlay();
    }

    private void ConfigureForScene(Scene scene)
    {
        string sceneName = scene.IsValid() ? scene.name : string.Empty;
        pauseAllowed = !string.IsNullOrEmpty(sceneName)
            && sceneName != StartSceneName
            && sceneName != ResultSceneName;

        bool isHub = sceneName == HubSceneName;
        returnSceneName = isHub ? StartSceneName : HubSceneName;

        if (returnButtonLabel != null)
        {
            returnButtonLabel.text = isHub ? "タイトルへ戻る" : "マップへ戻る";
        }

        if (menuCanvas != null)
        {
            menuCanvas.enabled = pauseAllowed;
        }
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused && pauseAllowed;
        Time.timeScale = isPaused ? 0f : 1f;

        if (pauseOverlay != null)
        {
            pauseOverlay.SetActive(isPaused);
        }

        if (!isPaused)
        {
            return;
        }

        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        if (mainMenuRoot != null)
        {
            mainMenuRoot.SetActive(true);
        }

        if (settingsRoot != null)
        {
            settingsRoot.SetActive(false);
        }

        SelectButton(resumeButton);
    }

    private void ShowSettings()
    {
        RefreshSettingsValues();
        mainMenuRoot.SetActive(false);
        settingsRoot.SetActive(true);
        SelectButton(settingsBackButton);
    }

    private void CreateGlobalMenuUi()
    {
        GameObject canvasObject = new GameObject("GlobalGameMenuCanvas");
        canvasObject.transform.SetParent(transform, false);

        menuCanvas = canvasObject.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 300;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        pauseOverlay = CreateUiObject("GameMenuOverlay", canvasObject.transform);
        StretchToParent(pauseOverlay.GetComponent<RectTransform>());
        Image veil = pauseOverlay.AddComponent<Image>();
        veil.color = new Color(0.018f, 0.012f, 0.04f, 0.5f);

        GameObject panel = CreateUiObject("GameMenuPanel", pauseOverlay.transform);
        SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(364f, 660f));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.sprite = uiTheme != null ? uiTheme.LargePanel : null;
        panelImage.color = panelImage.sprite != null
            ? new Color(1.04f, 1.02f, 0.98f, 0.88f)
            : new Color(0.12f, 0.08f, 0.16f, 0.88f);
        panelImage.preserveAspect = false;

        CreateHeader(panel.transform, "メニュー");
        CreateCloseButton(panel.transform);
        CreateMainMenu(panel.transform);
        CreateSettingsMenu(panel.transform);
        GameSettings.Changed += RefreshSettingsValues;
    }

    private void CreateHeader(Transform parent, string title)
    {
        GameObject header = CreateUiObject("MenuHeader", parent);
        SetRect(header.GetComponent<RectTransform>(), new Vector2(0f, 246f), new Vector2(300f, 152f));
        Image headerImage = header.AddComponent<Image>();
        headerImage.sprite = uiTheme != null ? uiTheme.HeaderFrame : null;
        headerImage.color = headerImage.sprite != null
            ? new Color(1.03f, 1.01f, 0.96f, 0.9f)
            : new Color(0.25f, 0.12f, 0.3f, 0.9f);
        headerImage.preserveAspect = true;
        CreateText(title, header.transform, new Vector2(0f, 7f), new Vector2(214f, 42f), 25, new Color(0.24f, 0.095f, 0.055f, 0.94f));
    }

    private void CreateCloseButton(Transform parent)
    {
        GameObject buttonObject = CreateUiObject("CloseButton", parent);
        SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(140f, 292f), new Vector2(58f, 58f));

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.58f, 0.31f, 0.12f, 0.96f);

        closeButton = buttonObject.AddComponent<Button>();
        closeButton.targetGraphic = image;
        closeButton.onClick.AddListener(Resume);

        GameObject inner = CreateUiObject("CloseButtonInner", buttonObject.transform);
        SetRect(inner.GetComponent<RectTransform>(), Vector2.zero, new Vector2(48f, 48f));
        Image innerImage = inner.AddComponent<Image>();
        innerImage.color = new Color(0.15f, 0.07f, 0.19f, 0.96f);
        innerImage.raycastTarget = false;

        Text icon = CreateText("X", inner.transform, Vector2.zero, new Vector2(38f, 38f), 27, new Color(1f, 0.9f, 0.56f, 1f));
        icon.fontStyle = FontStyle.Bold;
        icon.raycastTarget = false;
        Outline iconOutline = icon.gameObject.AddComponent<Outline>();
        iconOutline.effectColor = new Color(0.08f, 0.025f, 0.08f, 0.9f);
        iconOutline.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private void CreateMainMenu(Transform parent)
    {
        mainMenuRoot = CreateUiObject("MainMenu", parent);
        SetRect(mainMenuRoot.GetComponent<RectTransform>(), new Vector2(0f, -30f), new Vector2(310f, 408f));

        resumeButton = CreateMenuButton("ResumeButton", "ゲームを続ける", mainMenuRoot.transform, 98f, false, Resume);
        CreateMenuButton("SettingsButton", "設定", mainMenuRoot.transform, 0f, false, ShowSettings);
        Button returnButton = CreateMenuButton("ReturnButton", string.Empty, mainMenuRoot.transform, -98f, true, ReturnToMap);
        returnButtonLabel = returnButton.GetComponentInChildren<Text>();
    }

    private void CreateSettingsMenu(Transform parent)
    {
        settingsRoot = CreateUiObject("SettingsMenu", parent);
        SetRect(settingsRoot.GetComponent<RectTransform>(), new Vector2(0f, -26f), new Vector2(320f, 430f));

        bgmSlider = CreateVolumeSlider("BgmVolume", "BGM", settingsRoot.transform, 112f, GameSettings.BgmVolume, out bgmValueLabel);
        bgmSlider.onValueChanged.AddListener(GameSettings.SetBgmVolume);

        sfxSlider = CreateVolumeSlider("SfxVolume", "効果音", settingsRoot.transform, 22f, GameSettings.SfxVolume, out sfxValueLabel);
        sfxSlider.onValueChanged.AddListener(GameSettings.SetSfxVolume);

        CreateText("画面モード", settingsRoot.transform, new Vector2(-82f, -66f), new Vector2(120f, 32f), 17, new Color(0.26f, 0.12f, 0.1f, 1f));
        Button fullscreenButton = CreateMenuButton("FullscreenButton", string.Empty, settingsRoot.transform, -82f, false, ToggleFullscreen);
        RectTransform fullscreenRect = fullscreenButton.transform as RectTransform;
        fullscreenRect.anchoredPosition = new Vector2(75f, -66f);
        fullscreenRect.sizeDelta = new Vector2(128f, 54f);
        fullscreenValueLabel = fullscreenButton.GetComponentInChildren<Text>();
        fullscreenValueLabel.rectTransform.sizeDelta = new Vector2(110f, 32f);
        fullscreenValueLabel.fontSize = 14;

        settingsBackButton = CreateMenuButton("SettingsBackButton", "戻る", settingsRoot.transform, -164f, true, ShowMainMenu);
        RefreshSettingsValues();
        settingsRoot.SetActive(false);
    }

    private Slider CreateVolumeSlider(
        string objectName,
        string label,
        Transform parent,
        float y,
        float value,
        out Text valueLabel)
    {
        GameObject root = CreateUiObject(objectName, parent);
        SetRect(root.GetComponent<RectTransform>(), new Vector2(0f, y), new Vector2(286f, 76f));
        CreateText(label, root.transform, new Vector2(-92f, 18f), new Vector2(86f, 28f), 17, new Color(0.26f, 0.12f, 0.1f, 1f));
        valueLabel = CreateText("100%", root.transform, new Vector2(104f, 18f), new Vector2(62f, 28f), 15, new Color(0.26f, 0.12f, 0.1f, 1f));

        GameObject sliderObject = CreateUiObject("Slider", root.transform);
        SetRect(sliderObject.GetComponent<RectTransform>(), new Vector2(18f, -17f), new Vector2(220f, 30f));
        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;

        GameObject background = CreateUiObject("Background", sliderObject.transform);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(0f, 8f);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.24f, 0.11f, 0.23f, 0.7f);

        GameObject fillArea = CreateUiObject("Fill Area", sliderObject.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRect.offsetMin = new Vector2(5f, -4f);
        fillAreaRect.offsetMax = new Vector2(-5f, 4f);
        GameObject fill = CreateUiObject("Fill", fillArea.transform);
        StretchToParent(fill.GetComponent<RectTransform>());
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.78f, 0.36f, 0.72f, 1f);

        GameObject handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform);
        StretchToParent(handleArea.GetComponent<RectTransform>());
        GameObject handle = CreateUiObject("Handle", handleArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(22f, 22f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(1f, 0.78f, 0.28f, 1f);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private Button CreateMenuButton(
        string objectName,
        string label,
        Transform parent,
        float y,
        bool red,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0f, y), new Vector2(226f, 82f));

        GameObject artworkObject = CreateUiObject("ButtonArtwork", buttonObject.transform);
        StretchToParent(artworkObject.GetComponent<RectTransform>());

        Sprite normalSprite = red
            ? uiTheme != null ? uiTheme.RedButtonNormal : null
            : uiTheme != null ? uiTheme.PurpleButtonNormal : null;
        Sprite selectedSprite = red
            ? uiTheme != null ? uiTheme.RedButtonSelected : null
            : uiTheme != null ? uiTheme.PurpleButtonSelected : null;

        Image image = artworkObject.AddComponent<Image>();
        image.sprite = normalSprite;
        image.color = normalSprite != null
            ? new Color(1.03f, 1.01f, 0.97f, 0.78f)
            : new Color(0.36f, 0.18f, 0.42f, 0.78f);
        image.preserveAspect = false;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        if (normalSprite != null && selectedSprite != null)
        {
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState state = button.spriteState;
            state.highlightedSprite = selectedSprite;
            state.selectedSprite = selectedSprite;
            state.pressedSprite = selectedSprite;
            button.spriteState = state;
        }
        button.onClick.AddListener(onClick);

        Text text = CreateText(label, buttonObject.transform, new Vector2(0f, 5f), new Vector2(194f, 46f), 19, new Color(1f, 0.93f, 0.72f, 0.96f));
        text.fontStyle = FontStyle.Bold;
        text.raycastTarget = false;
        Outline textOutline = text.gameObject.AddComponent<Outline>();
        textOutline.effectColor = new Color(0.12f, 0.04f, 0.12f, 0.92f);
        textOutline.effectDistance = new Vector2(1.25f, -1.25f);

        UiSelectionScale selectionScale = buttonObject.AddComponent<UiSelectionScale>();
        selectionScale.Configure(1.08f, image, 0.78f, 0.98f, artworkObject.transform, text.rectTransform);
        selectionScale.SetSelected(false);
        return button;
    }

    private void ToggleFullscreen()
    {
        GameSettings.SetFullscreen(!GameSettings.IsFullscreen);
    }

    private void RefreshSettingsValues()
    {
        float bgm = GameSettings.BgmVolume;
        float sfx = GameSettings.SfxVolume;

        if (bgmSlider != null && !Mathf.Approximately(bgmSlider.value, bgm))
        {
            bgmSlider.SetValueWithoutNotify(bgm);
        }

        if (sfxSlider != null && !Mathf.Approximately(sfxSlider.value, sfx))
        {
            sfxSlider.SetValueWithoutNotify(sfx);
        }

        if (bgmValueLabel != null)
        {
            bgmValueLabel.text = $"{Mathf.RoundToInt(bgm * 100f)}%";
        }

        if (sfxValueLabel != null)
        {
            sfxValueLabel.text = $"{Mathf.RoundToInt(sfx * 100f)}%";
        }

        if (fullscreenValueLabel != null)
        {
            fullscreenValueLabel.text = GameSettings.IsFullscreen ? "全画面" : "ウィンドウ";
        }
    }

    private void SelectButton(Button button)
    {
        if (button != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Text CreateText(
        string text,
        Transform parent,
        Vector2 position,
        Vector2 size,
        int fontSize,
        Color color)
    {
        GameObject textObject = CreateUiObject(string.IsNullOrEmpty(text) ? "Label" : text, parent);
        SetRect(textObject.GetComponent<RectTransform>(), position, size);

        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.alignByGeometry = true;
        uiText.fontSize = fontSize;
        uiText.color = color;
        uiText.font = GetUiFont();
        uiText.fontStyle = FontStyle.Bold;
        uiText.resizeTextForBestFit = true;
        uiText.resizeTextMinSize = Mathf.Max(12, fontSize - 4);
        uiText.resizeTextMaxSize = fontSize;
        return uiText;
    }

    private Font GetUiFont()
    {
        if (uiFont == null)
        {
            uiFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Yu Gothic UI Semibold", "Yu Gothic UI", "Yu Gothic", "Meiryo UI", "Meiryo" },
                24);
        }

        return uiFont != null
            ? uiFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void EnsureEventSystem()
    {
        if (globalEventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("GlobalEventSystem");
            eventSystemObject.transform.SetParent(transform, false);
            globalEventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
        for (int index = 0; index < eventSystems.Length; index++)
        {
            EventSystem candidate = eventSystems[index];
            if (candidate != null && candidate != globalEventSystem)
            {
                candidate.enabled = false;
            }
        }

        globalEventSystem.enabled = true;
    }
}
