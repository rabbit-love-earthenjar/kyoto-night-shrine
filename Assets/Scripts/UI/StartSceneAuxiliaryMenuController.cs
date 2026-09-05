using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class StartSceneAuxiliaryMenuController : MonoBehaviour
{
    private static readonly Color PanelColor = new Color(0.105f, 0.065f, 0.14f, 0.94f);
    private static readonly Color PanelInnerColor = new Color(0.20f, 0.115f, 0.12f, 0.82f);
    private static readonly Color GoldColor = new Color(0.95f, 0.79f, 0.42f, 1f);
    private static readonly Color PrimaryTextColor = new Color(0.965f, 0.905f, 0.775f, 1f);

    private GameObject menuRoot;
    private GameObject overlay;
    private GameObject settingsRoot;
    private GameObject creditsRoot;
    private Slider bgmSlider;
    private Slider sfxSlider;
    private Text bgmValue;
    private Text sfxValue;
    private Text titleBgmValue;
    private Text resolutionValue;
    private Text displayModeValue;
    private Button settingsBackButton;
    private Button creditsBackButton;
    private Action onClosed;
    private Font uiFont;
    private GameUiTheme uiTheme;
    private bool configured;

    public bool IsOpen => overlay != null && overlay.activeSelf;

    public void Configure(GameObject sourceMenuRoot, Action closedCallback)
    {
        if (configured)
        {
            return;
        }

        menuRoot = sourceMenuRoot;
        onClosed = closedCallback;
        uiTheme = GameUiTheme.LoadDefault();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("StartScene auxiliary menus require a parent Canvas.", this);
            return;
        }

        BuildOverlay(canvas.transform);
        GameSettings.Changed += RefreshValues;
        configured = true;
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    private void OnDestroy()
    {
        GameSettings.Changed -= RefreshValues;
    }

    public void ShowSettings()
    {
        if (!configured || overlay == null)
        {
            return;
        }

        RefreshValues();
        menuRoot?.SetActive(false);
        overlay.SetActive(true);
        settingsRoot.SetActive(true);
        creditsRoot.SetActive(false);
        SelectButton(settingsBackButton);
    }

    public void ShowCredits()
    {
        if (!configured || overlay == null)
        {
            return;
        }

        menuRoot?.SetActive(false);
        overlay.SetActive(true);
        settingsRoot.SetActive(false);
        creditsRoot.SetActive(true);
        SelectButton(creditsBackButton);
    }

    public void Close()
    {
        if (overlay != null)
        {
            overlay.SetActive(false);
        }

        menuRoot?.SetActive(true);
        onClosed?.Invoke();
    }

    private void BuildOverlay(Transform canvasTransform)
    {
        overlay = CreateUiObject("StartAuxiliaryOverlay", canvasTransform);
        StretchToParent(overlay.GetComponent<RectTransform>());
        overlay.transform.SetAsLastSibling();
        Image veil = overlay.AddComponent<Image>();
        veil.color = new Color(0.015f, 0.01f, 0.035f, 0.68f);

        GameObject panel = CreateUiObject("StartAuxiliaryPanel", overlay.transform);
        SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(590f, 650f));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = PanelColor;

        GameObject panelFrame = CreateUiObject("PanelFrame", panel.transform);
        StretchToParent(panelFrame.GetComponent<RectTransform>());
        Image panelFrameImage = panelFrame.AddComponent<Image>();
        panelFrameImage.sprite = uiTheme != null ? uiTheme.LargePanel : null;
        panelFrameImage.color = panelFrameImage.sprite != null
            ? new Color(1.03f, 1.01f, 0.97f, 0.98f)
            : Color.clear;
        panelFrameImage.preserveAspect = false;
        panelFrameImage.raycastTarget = false;

        GameObject header = CreateUiObject("HeaderFrame", panel.transform);
        SetRect(header.GetComponent<RectTransform>(), new Vector2(0f, 270f), new Vector2(470f, 112f));
        Image headerImage = header.AddComponent<Image>();
        headerImage.sprite = uiTheme != null ? uiTheme.HeaderFrame : null;
        headerImage.color = headerImage.sprite != null ? new Color(1.03f, 1.01f, 0.97f, 1f) : PanelInnerColor;
        headerImage.preserveAspect = false;
        CreateLabel("夜神社カフェ", header.transform, new Vector2(0f, 5f), new Vector2(340f, 42f), 26, GoldColor);

        Button closeButton = CreateButton("CloseButton", "X", panel.transform, new Vector2(249f, 279f), new Vector2(52f, 52f), Close);
        closeButton.GetComponentInChildren<Text>().fontSize = 24;

        settingsRoot = CreateUiObject("SettingsPanel", panel.transform);
        StretchToParent(settingsRoot.GetComponent<RectTransform>());
        BuildSettings(settingsRoot.transform);

        creditsRoot = CreateUiObject("CreditsPanel", panel.transform);
        StretchToParent(creditsRoot.GetComponent<RectTransform>());
        BuildCredits(creditsRoot.transform);

        overlay.SetActive(false);
    }

    private void BuildSettings(Transform parent)
    {
        CreateLabel("設定", parent, new Vector2(0f, 207f), new Vector2(300f, 42f), 25, new Color(0.28f, 0.12f, 0.08f, 1f));
        bgmSlider = CreateVolumeSlider("BgmVolume", "BGM 音量", parent, 140f, GameSettings.BgmVolume, out bgmValue);
        bgmSlider.onValueChanged.AddListener(GameSettings.SetBgmVolume);
        sfxSlider = CreateVolumeSlider("SfxVolume", "効果音", parent, 62f, GameSettings.SfxVolume, out sfxValue);
        sfxSlider.onValueChanged.AddListener(GameSettings.SetSfxVolume);

        CreateLabel("タイトル曲", parent, new Vector2(-175f, -20f), new Vector2(180f, 38f), 18, PrimaryTextColor);
        Button titleBgmButton = CreateButton("TitleBgmButton", string.Empty, parent, new Vector2(115f, -20f), new Vector2(250f, 52f), GameSettings.CycleTitleBgmPreference);
        titleBgmValue = titleBgmButton.GetComponentInChildren<Text>();

        CreateLabel("画面サイズ", parent, new Vector2(-175f, -90f), new Vector2(180f, 38f), 18, PrimaryTextColor);
        Button resolutionButton = CreateButton("ResolutionButton", string.Empty, parent, new Vector2(115f, -90f), new Vector2(250f, 52f), GameSettings.CycleResolution);
        resolutionValue = resolutionButton.GetComponentInChildren<Text>();

        CreateLabel("表示モード", parent, new Vector2(-175f, -160f), new Vector2(180f, 38f), 18, PrimaryTextColor);
        Button displayButton = CreateButton("DisplayModeButton", string.Empty, parent, new Vector2(115f, -160f), new Vector2(250f, 52f), ToggleFullscreen);
        displayModeValue = displayButton.GetComponentInChildren<Text>();

        settingsBackButton = CreateButton("SettingsBackButton", "戻る", parent, new Vector2(0f, -235f), new Vector2(220f, 58f), Close);
        RefreshValues();
    }

    private void BuildCredits(Transform parent)
    {
        CreateLabel("クレジット", parent, new Vector2(0f, 207f), new Vector2(360f, 42f), 25, new Color(0.28f, 0.12f, 0.08f, 1f));

        GameObject textPanel = CreateUiObject("CreditsTextPanel", parent);
        SetRect(textPanel.GetComponent<RectTransform>(), new Vector2(0f, 7f), new Vector2(500f, 350f));
        Image textPanelImage = textPanel.AddComponent<Image>();
        textPanelImage.sprite = uiTheme != null ? uiTheme.ConfirmPanel : null;
        textPanelImage.color = textPanelImage.sprite != null
            ? new Color(1f, 0.98f, 0.92f, 0.96f)
            : PanelInnerColor;
        textPanelImage.preserveAspect = false;

        string credits =
            "企画・ゲームデザイン・制作\n" +
            "個人制作プロジェクト\n\n" +
            "開発支援\n" +
            "OpenAI Codex / ChatGPT\n\n" +
            "音楽・効果音・使用素材\n" +
            "正式な出典・ライセンス表を整理中\n\n" +
            "Night Shrine Cafe - Playable Vertical Slice";
        Text creditsLabel = CreateLabel(credits, textPanel.transform, Vector2.zero, new Vector2(450f, 300f), 18, new Color(0.25f, 0.11f, 0.08f, 1f));
        creditsLabel.lineSpacing = 1.18f;

        creditsBackButton = CreateButton("CreditsBackButton", "戻る", parent, new Vector2(0f, -235f), new Vector2(220f, 58f), Close);
    }

    private Slider CreateVolumeSlider(string name, string label, Transform parent, float y, float value, out Text valueText)
    {
        GameObject root = CreateUiObject(name, parent);
        SetRect(root.GetComponent<RectTransform>(), new Vector2(0f, y), new Vector2(540f, 66f));
        CreateLabel(label, root.transform, new Vector2(-178f, 16f), new Vector2(180f, 30f), 18, PrimaryTextColor);
        valueText = CreateLabel("100%", root.transform, new Vector2(218f, 16f), new Vector2(72f, 30f), 17, GoldColor);

        GameObject sliderObject = CreateUiObject("Slider", root.transform);
        SetRect(sliderObject.GetComponent<RectTransform>(), new Vector2(30f, -16f), new Vector2(390f, 28f));
        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;

        GameObject background = CreateUiObject("Background", sliderObject.transform);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(0f, 10f);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.05f, 0.025f, 0.075f, 0.9f);

        GameObject fillArea = CreateUiObject("Fill Area", sliderObject.transform);
        StretchToParent(fillArea.GetComponent<RectTransform>());
        GameObject fill = CreateUiObject("Fill", fillArea.transform);
        StretchToParent(fill.GetComponent<RectTransform>());
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.55f, 0.22f, 0.48f, 1f);

        GameObject handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform);
        StretchToParent(handleArea.GetComponent<RectTransform>());
        GameObject handle = CreateUiObject("Handle", handleArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(24f, 24f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = GoldColor;

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        return slider;
    }

    private Button CreateButton(string name, string label, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        SetRect(buttonObject.GetComponent<RectTransform>(), position, size);
        Image image = buttonObject.AddComponent<Image>();
        Sprite normalSprite = uiTheme != null ? uiTheme.PurpleButtonNormal : null;
        Sprite selectedSprite = uiTheme != null ? uiTheme.PurpleButtonSelected : null;
        image.sprite = normalSprite;
        image.color = normalSprite != null
            ? new Color(1.03f, 1.01f, 0.97f, 0.9f)
            : new Color(0.30f, 0.14f, 0.34f, 0.88f);
        image.preserveAspect = false;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        if (normalSprite != null && selectedSprite != null)
        {
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState spriteState = button.spriteState;
            spriteState.highlightedSprite = selectedSprite;
            spriteState.selectedSprite = selectedSprite;
            spriteState.pressedSprite = selectedSprite;
            button.spriteState = spriteState;
        }
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.08f, 0.88f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.86f, 0.72f, 0.55f, 1f);
        button.colors = colors;

        Text buttonLabel = CreateLabel(label, buttonObject.transform, Vector2.zero, size - new Vector2(18f, 12f), 18, PrimaryTextColor);
        buttonLabel.raycastTarget = false;

        UiSelectionScale selectionScale = buttonObject.AddComponent<UiSelectionScale>();
        selectionScale.Configure(1.05f, image, 0.9f, 1f, buttonObject.transform, buttonLabel.rectTransform);
        selectionScale.SetSelected(false);
        return button;
    }

    private Text CreateLabel(string text, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        GameObject textObject = CreateUiObject(string.IsNullOrEmpty(text) ? "Value" : "Label", parent);
        SetRect(textObject.GetComponent<RectTransform>(), position, size);
        Text label = textObject.AddComponent<Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = Mathf.Max(12, fontSize - 4);
        label.resizeTextMaxSize = fontSize;
        NightShrineTextStyle.Apply(label, NightShrineTextRole.Menu);
        return label;
    }

    private void ToggleFullscreen()
    {
        GameSettings.SetFullscreen(!GameSettings.IsFullscreen);
    }

    private void RefreshValues()
    {
        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(GameSettings.BgmVolume);
        }
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
        }
        if (bgmValue != null)
        {
            bgmValue.text = $"{Mathf.RoundToInt(GameSettings.BgmVolume * 100f)}%";
        }
        if (sfxValue != null)
        {
            sfxValue.text = $"{Mathf.RoundToInt(GameSettings.SfxVolume * 100f)}%";
        }
        if (titleBgmValue != null)
        {
            titleBgmValue.text = GameSettings.GetTitleBgmLabelJapanese();
        }
        if (resolutionValue != null)
        {
            resolutionValue.text = GameSettings.GetResolutionLabel();
        }
        if (displayModeValue != null)
        {
            displayModeValue.text = GameSettings.IsFullscreen ? "全画面" : "ウィンドウ";
        }
    }

    private void SelectButton(Button button)
    {
        if (button != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    private Font GetUiFont()
    {
        if (uiFont == null)
        {
            uiFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Yu Gothic UI Semibold", "Yu Gothic UI", "Yu Gothic", "Meiryo UI", "Meiryo" },
                24);
        }

        return uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject result = new GameObject(name, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
