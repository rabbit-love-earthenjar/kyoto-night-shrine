using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private Transform startPoint;

    private GameObject retryPanel;
    private bool retryVisible;

    private void Awake()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
        }

        if (playerBody == null && player != null)
        {
            playerBody = player.GetComponent<Rigidbody2D>();
        }

        EnsureEventSystem();
        CreateRetryUi();
        HideRetryUi();
    }

    public void PlayerFell()
    {
        if (retryVisible)
        {
            return;
        }

        retryVisible = true;

        if (player != null)
        {
            player.SetControlEnabled(false);
        }

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.simulated = false;
        }

        retryPanel.SetActive(true);
    }

    public void Retry()
    {
        HideRetryUi();
        ResetPlayer();
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

    private void CreateRetryUi()
    {
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
}
