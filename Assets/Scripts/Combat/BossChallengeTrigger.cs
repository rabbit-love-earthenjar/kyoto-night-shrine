using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BossChallengeTrigger : MonoBehaviour
{
    [Header("Challenge")]
    [SerializeField] private string bossSceneName = "Stage_1_Boss_RedOni";
    [SerializeField] private float promptDelay = 0.35f;
    [SerializeField] private Transform markerAnchor;
    [SerializeField] private Vector3 markerOffset = new Vector3(0f, 1.65f, 0f);
    [SerializeField] private bool requireGroundedPlayer = true;

    private PlayerController player;
    private GameObject markerObject;
    private GameObject promptCanvasObject;
    private Coroutine promptRoutine;
    private bool waitingForExit;
    private bool loadingScene;

    public bool RequiresGroundedPlayer => requireGroundedPlayer;

    public void Configure(Transform targetMarkerAnchor, string targetBossSceneName, bool requireGrounded = true)
    {
        markerAnchor = targetMarkerAnchor;

        if (!string.IsNullOrWhiteSpace(targetBossSceneName))
        {
            bossSceneName = targetBossSceneName;
        }

        requireGroundedPlayer = requireGrounded;
    }

    private void Awake()
    {
        EnsureMarker();
        EnsurePromptUi();
        SetMarkerVisible(false);
        SetPromptVisible(false);
    }

    private void OnDisable()
    {
        if (promptRoutine != null)
        {
            StopCoroutine(promptRoutine);
            promptRoutine = null;
        }

        if (!loadingScene && player != null)
        {
            player.SetControlEnabled(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryBeginChallenge(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryBeginChallenge(other);
    }

    private void TryBeginChallenge(Collider2D other)
    {
        if (waitingForExit || loadingScene || (GameManager.Instance != null && GameManager.Instance.IsBlockingUiVisible))
        {
            return;
        }

        PlayerController enteringPlayer = other.GetComponentInParent<PlayerController>();

        if (enteringPlayer == null)
        {
            return;
        }

        if (requireGroundedPlayer && !enteringPlayer.IsGrounded)
        {
            return;
        }

        player = enteringPlayer;
        waitingForExit = true;
        player.SetControlEnabled(false);
        SetMarkerVisible(true);

        if (promptRoutine != null)
        {
            StopCoroutine(promptRoutine);
        }

        promptRoutine = StartCoroutine(ShowPromptAfterDelay());
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController exitingPlayer = other.GetComponentInParent<PlayerController>();

        if (exitingPlayer == null || exitingPlayer != player || IsPromptVisible())
        {
            return;
        }

        waitingForExit = false;
        SetMarkerVisible(false);
    }

    public void AcceptChallenge()
    {
        if (loadingScene || string.IsNullOrWhiteSpace(bossSceneName))
        {
            return;
        }

        loadingScene = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(bossSceneName);
    }

    public void DeclineChallenge()
    {
        SetPromptVisible(false);
        SetMarkerVisible(false);

        if (player != null)
        {
            player.SetControlEnabled(true);
        }
    }

    private IEnumerator ShowPromptAfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, promptDelay));
        promptRoutine = null;

        if (!loadingScene)
        {
            EnsurePromptUi();
            SetPromptVisible(true);
        }
    }

    private void EnsureMarker()
    {
        if (markerObject != null)
        {
            return;
        }

        Transform parent = markerAnchor != null ? markerAnchor : transform;
        markerObject = new GameObject("GoldenChallengeExclamation");
        markerObject.transform.SetParent(parent, false);
        markerObject.transform.localPosition = markerOffset;

        TextMesh textMesh = markerObject.AddComponent<TextMesh>();
        textMesh.text = "!";
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 120;
        textMesh.characterSize = 0.065f;
        textMesh.color = new Color(1f, 0.78f, 0.12f, 1f);
        textMesh.fontStyle = FontStyle.Bold;

        MeshRenderer renderer = markerObject.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = 50;
        }
    }

    private void EnsurePromptUi()
    {
        if (promptCanvasObject != null)
        {
            return;
        }

        EnsureEventSystem();

        promptCanvasObject = new GameObject("BossChallengeCanvas");
        Canvas canvas = promptCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        promptCanvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        promptCanvasObject.AddComponent<GraphicRaycaster>();

        GameObject veil = CreateImage(
            "ChallengeVeil",
            promptCanvasObject.transform,
            Vector2.zero,
            new Vector2(1920f, 1080f),
            new Color(0.02f, 0.015f, 0.04f, 0.62f));

        GameObject panel = CreateImage(
            "ChallengePanel",
            veil.transform,
            Vector2.zero,
            new Vector2(560f, 300f),
            new Color(0.12f, 0.07f, 0.07f, 0.97f));

        Text title = CreateText("Title", panel.transform, "赤鬼への挑戦", new Vector2(0f, 90f), new Vector2(500f, 50f), 32);
        title.color = new Color(1f, 0.78f, 0.18f, 1f);
        CreateText(
            "Message",
            panel.transform,
            "この先には赤鬼が待っています。\n今すぐ挑戦しますか？",
            new Vector2(0f, 24f),
            new Vector2(500f, 80f),
            22);

        CreateButton("ChallengeButton", "挑戦する", panel.transform, new Vector2(-125f, -88f), AcceptChallenge);
        CreateButton("LaterButton", "今は戻る", panel.transform, new Vector2(125f, -88f), DeclineChallenge);
    }

    private static GameObject CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        imageObject.AddComponent<Image>().color = color;
        return imageObject;
    }

    private static Text CreateText(
        string name,
        Transform parent,
        string content,
        Vector2 position,
        Vector2 size,
        int fontSize)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.text = content;
        text.font = LoadBuiltinFont();
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void CreateButton(
        string name,
        string label,
        Transform parent,
        Vector2 position,
        UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateImage(
            name,
            parent,
            position,
            new Vector2(190f, 54f),
            new Color(0.75f, 0.56f, 0.25f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(action);
        CreateText("Label", buttonObject.transform, label, Vector2.zero, new Vector2(180f, 46f), 21);
    }

    private static Font LoadBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void SetMarkerVisible(bool visible)
    {
        EnsureMarker();
        markerObject.SetActive(visible);
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptCanvasObject != null)
        {
            promptCanvasObject.SetActive(visible);
        }
    }

    private bool IsPromptVisible()
    {
        return promptCanvasObject != null && promptCanvasObject.activeSelf;
    }
}
