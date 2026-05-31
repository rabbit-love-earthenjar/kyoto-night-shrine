using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CombatPauseController : MonoBehaviour
{
    [SerializeField] private string returnSceneName = "HubMap_Day";
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    private GameObject pausePanel;
    private bool isPaused;

    private void Awake()
    {
        Time.timeScale = 1f;
        EnsureEventSystem();
        CreatePauseUi();
        SetPaused(false);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(pauseKey))
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
        if (isPaused)
        {
            Time.timeScale = 1f;
        }
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void ReturnToMap()
    {
        Time.timeScale = 1f;
        GameAudio.ExitRetryAudioState();

        if (!string.IsNullOrEmpty(returnSceneName))
        {
            SceneManager.LoadScene(returnSceneName);
        }
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(paused);
        }
    }

    private void CreatePauseUi()
    {
        GameObject canvasObject = new GameObject("CombatPauseCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 140;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        canvasObject.AddComponent<GraphicRaycaster>();

        pausePanel = new GameObject("CombatPausePanel");
        pausePanel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = pausePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(300f, 220f);

        Image panelImage = pausePanel.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.08f, 0.12f, 0.86f);

        CreateText("Pause", pausePanel.transform, new Vector2(0f, 72f), new Vector2(240f, 42f), 30);
        CreateButton("ResumeButton", "Resume", pausePanel.transform, new Vector2(0f, 10f), Resume);
        CreateButton("ReturnToMapButton", "Return to Map", pausePanel.transform, new Vector2(0f, -52f), ReturnToMap);
    }

    private void CreateButton(string objectName, string label, Transform parent, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(190f, 44f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.86f, 0.82f, 0.72f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        Text buttonText = CreateText(label, buttonObject.transform, Vector2.zero, new Vector2(180f, 36f), 19);
        buttonText.color = Color.black;
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
