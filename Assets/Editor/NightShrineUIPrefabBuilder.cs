using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class NightShrineUIPrefabBuilder
{
    private const string UiRoot = "Assets/UI";
    private const string ThemePath = UiRoot + "/Styles/NightShrineUITheme.asset";
    private const string PrefabRoot = UiRoot + "/Prefabs/Common";

    [MenuItem("Tools/Night Shrine UI/Build Common UI Prefabs")]
    public static void BuildAll()
    {
        EnsureFolders();
        NightShrineUITheme theme = LoadOrCreateTheme();

        BuildButtonPrefab(theme);
        BuildConfirmDialogPrefab(theme);
        BuildPauseMenuPrefab(theme);
        BuildTextPanelPrefab(theme);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateCommonPrefabs();
        Debug.Log("Night Shrine common UI prefabs built under Assets/UI/Prefabs/Common.");
    }

    public static void BuildAllFromCommandLine()
    {
        BuildAll();
    }

    [MenuItem("Tools/Night Shrine UI/Validate Common UI Prefabs")]
    public static void ValidateCommonPrefabs()
    {
        ValidatePrefab(
            "UI_Button_NightShrine.prefab",
            typeof(Button),
            typeof(TMP_Text),
            typeof(NightShrineButtonState));
        ValidatePrefab(
            "UI_ConfirmDialog_NightShrine.prefab",
            typeof(NightShrineConfirmDialogController),
            typeof(TMP_Text),
            typeof(Button));
        ValidatePrefab(
            "UI_PauseMenu_NightShrine.prefab",
            typeof(NightShrinePauseMenuController),
            typeof(TMP_Text),
            typeof(Button));
        ValidatePrefab(
            "UI_TextPanel_NightShrine.prefab",
            typeof(Image),
            typeof(TMP_Text));

        Debug.Log("Night Shrine common UI prefab validation passed.");
    }

    private static void ValidatePrefab(string fileName, params Type[] requiredTypes)
    {
        string path = PrefabRoot + "/" + fileName;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            throw new InvalidOperationException("Missing common UI prefab: " + path);
        }

        foreach (Type requiredType in requiredTypes)
        {
            if (prefab.GetComponentInChildren(requiredType, true) == null)
            {
                throw new InvalidOperationException(
                    fileName + " is missing required component " + requiredType.Name + ".");
            }
        }
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "UI");
        EnsureFolder(UiRoot, "Fonts");
        EnsureFolder(UiRoot, "Sprites");
        EnsureFolder(UiRoot + "/Sprites", "Common");
        EnsureFolder(UiRoot, "Prefabs");
        EnsureFolder(UiRoot + "/Prefabs", "Common");
        EnsureFolder(UiRoot, "Styles");
        EnsureFolder(UiRoot, "Scenes");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static NightShrineUITheme LoadOrCreateTheme()
    {
        NightShrineUITheme theme = AssetDatabase.LoadAssetAtPath<NightShrineUITheme>(ThemePath);
        if (theme != null)
        {
            return theme;
        }

        theme = ScriptableObject.CreateInstance<NightShrineUITheme>();
        AssetDatabase.CreateAsset(theme, ThemePath);
        return theme;
    }

    private static void BuildButtonPrefab(NightShrineUITheme theme)
    {
        GameObject root = CreateUiObject("UI_Button_NightShrine", null);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(420f, 88f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image background = root.AddComponent<Image>();
        background.color = WithAlpha(theme.PanelDarkBrown, 0.78f);

        Button button = root.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;

        TMP_Text label = CreateText("Label", root.transform, "メニュー", theme.ResolveMenuFont(),
            theme.MenuFontSize, theme.TextPrimary, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, 20f, 8f);

        NightShrineButtonState state = root.AddComponent<NightShrineButtonState>();
        BindButtonState(state, theme, rect, background, label, button);

        SavePrefab(root, PrefabRoot + "/UI_Button_NightShrine.prefab");
    }

    private static void BuildConfirmDialogPrefab(NightShrineUITheme theme)
    {
        GameObject root = CreateUiObject("UI_ConfirmDialog_NightShrine", null);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(720f, 430f);

        Image panel = root.AddComponent<Image>();
        panel.color = WithAlpha(theme.PanelDarkPurple, 0.94f);

        TMP_Text title = CreateText("Title", root.transform, "確認", theme.ResolveMenuFont(),
            theme.MenuFontSize + 4f, theme.TextGold, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0f, 142f), new Vector2(620f, 64f));

        TMP_Text message = CreateText("Message", root.transform, "この操作を実行しますか？",
            theme.ResolveBodyFont(), theme.BodyFontSize, theme.TextPrimary, TextAlignmentOptions.Center);
        message.textWrappingMode = TextWrappingModes.Normal;
        SetRect(message.rectTransform, new Vector2(0f, 35f), new Vector2(610f, 130f));

        Button confirm = CreateButton("ConfirmButton", root.transform, "決定", theme,
            new Vector2(-150f, -135f), new Vector2(250f, 76f));
        Button cancel = CreateButton("CancelButton", root.transform, "キャンセル", theme,
            new Vector2(150f, -135f), new Vector2(250f, 76f));

        NightShrineConfirmDialogController controller = root.AddComponent<NightShrineConfirmDialogController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("dialogRoot").objectReferenceValue = root;
        serialized.FindProperty("titleText").objectReferenceValue = title;
        serialized.FindProperty("messageText").objectReferenceValue = message;
        serialized.FindProperty("confirmButton").objectReferenceValue = confirm;
        serialized.FindProperty("cancelButton").objectReferenceValue = cancel;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, PrefabRoot + "/UI_ConfirmDialog_NightShrine.prefab");
    }

    private static void BuildPauseMenuPrefab(NightShrineUITheme theme)
    {
        GameObject root = CreateUiObject("UI_PauseMenu_NightShrine", null);
        Stretch(root.GetComponent<RectTransform>(), 0f, 0f);

        GameObject menuRoot = CreateUiObject("MenuRoot", root.transform);
        RectTransform menuRect = menuRoot.GetComponent<RectTransform>();
        menuRect.sizeDelta = new Vector2(680f, 760f);
        Image panel = menuRoot.AddComponent<Image>();
        panel.color = WithAlpha(theme.PanelDarkPurple, 0.92f);

        GameObject mainPanel = CreateUiObject("MainPanel", menuRoot.transform);
        Stretch(mainPanel.GetComponent<RectTransform>(), 0f, 0f);

        TMP_Text title = CreateText("Title", mainPanel.transform, "一時停止", theme.ResolveMenuFont(),
            42f, theme.TextGold, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0f, 285f), new Vector2(560f, 80f));

        Button resume = CreateButton("ResumeButton", mainPanel.transform, "ゲームを続ける", theme,
            new Vector2(0f, 155f), new Vector2(460f, 82f));
        Button settings = CreateButton("SettingsButton", mainPanel.transform, "設定", theme,
            new Vector2(0f, 45f), new Vector2(460f, 82f));
        Button returnToTitle = CreateButton("ReturnToTitleButton", mainPanel.transform, "タイトルへ戻る", theme,
            new Vector2(0f, -65f), new Vector2(460f, 82f));
        Button quit = CreateButton("QuitButton", mainPanel.transform, "ゲームを終了", theme,
            new Vector2(0f, -175f), new Vector2(460f, 82f));

        GameObject settingsPanel = CreateUiObject("SettingsPanel", menuRoot.transform);
        Stretch(settingsPanel.GetComponent<RectTransform>(), 0f, 0f);
        TMP_Text settingsTitle = CreateText("Title", settingsPanel.transform, "設定", theme.ResolveMenuFont(),
            42f, theme.TextGold, TextAlignmentOptions.Center);
        SetRect(settingsTitle.rectTransform, new Vector2(0f, 220f), new Vector2(560f, 80f));
        TMP_Text placeholder = CreateText("Placeholder", settingsPanel.transform, "設定画面は次の段階で接続します。",
            theme.ResolveBodyFont(), theme.BodyFontSize, theme.TextPrimary, TextAlignmentOptions.Center);
        SetRect(placeholder.rectTransform, Vector2.zero, new Vector2(560f, 120f));
        Button settingsBack = CreateButton("BackButton", settingsPanel.transform, "戻る", theme,
            new Vector2(0f, -225f), new Vector2(360f, 78f));
        settingsPanel.SetActive(false);

        NightShrinePauseMenuController controller = root.AddComponent<NightShrinePauseMenuController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("menuRoot").objectReferenceValue = menuRoot;
        serialized.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        serialized.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        serialized.FindProperty("resumeButton").objectReferenceValue = resume;
        serialized.FindProperty("settingsButton").objectReferenceValue = settings;
        serialized.FindProperty("settingsBackButton").objectReferenceValue = settingsBack;
        serialized.FindProperty("returnToTitleButton").objectReferenceValue = returnToTitle;
        serialized.FindProperty("quitButton").objectReferenceValue = quit;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, PrefabRoot + "/UI_PauseMenu_NightShrine.prefab");
    }

    private static void BuildTextPanelPrefab(NightShrineUITheme theme)
    {
        GameObject root = CreateUiObject("UI_TextPanel_NightShrine", null);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(920f, 250f);

        Image panel = root.AddComponent<Image>();
        panel.color = WithAlpha(theme.PanelDarkBrown, 0.88f);

        TMP_Text body = CreateText("BodyText", root.transform,
            "説明テキスト", theme.ResolveBodyFont(), theme.BodyFontSize,
            theme.TextPrimary, TextAlignmentOptions.TopLeft);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Ellipsis;
        Stretch(body.rectTransform, 42f, 34f);

        SavePrefab(root, PrefabRoot + "/UI_TextPanel_NightShrine.prefab");
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string text,
        NightShrineUITheme theme,
        Vector2 position,
        Vector2 size)
    {
        GameObject root = CreateUiObject(name, parent);
        RectTransform rect = root.GetComponent<RectTransform>();
        SetRect(rect, position, size);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image background = root.AddComponent<Image>();
        background.color = WithAlpha(theme.PanelDarkBrown, 0.78f);

        Button button = root.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;

        TMP_Text label = CreateText("Label", root.transform, text, theme.ResolveMenuFont(),
            theme.MenuFontSize, theme.TextPrimary, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, 18f, 6f);

        NightShrineButtonState state = root.AddComponent<NightShrineButtonState>();
        BindButtonState(state, theme, rect, background, label, button);
        return button;
    }

    private static void BindButtonState(
        NightShrineButtonState state,
        NightShrineUITheme theme,
        RectTransform visualRoot,
        Image background,
        TMP_Text label,
        Button button)
    {
        SerializedObject serialized = new SerializedObject(state);
        serialized.FindProperty("theme").objectReferenceValue = theme;
        serialized.FindProperty("visualRoot").objectReferenceValue = visualRoot;
        serialized.FindProperty("background").objectReferenceValue = background;
        serialized.FindProperty("label").objectReferenceValue = label;
        serialized.FindProperty("button").objectReferenceValue = button;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string text,
        TMP_FontAsset font,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUiObject(name, parent);
        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        if (font != null)
        {
            label.font = font;
        }
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.outlineColor = new Color32(0x1A, 0x10, 0x20, 0x52);
        label.outlineWidth = 0.055f;
        label.fontStyle = FontStyles.Normal;
        return label;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, float horizontalPadding, float verticalPadding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        rect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        rect.localScale = Vector3.one;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }
}
