using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A small linear dialogue panel. Owns its pause only while a dialogue is open.
public sealed class DialoguePresenter : MonoBehaviour
{
    [SerializeField, Min(1f)] private float charactersPerSecond = 40f;
    [SerializeField, Min(0f)] private float fadeSeconds = 0.18f;

    public static DialoguePresenter Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.open;
    public int LineIndex { get; private set; }
    public bool IsTyping => open && visibleCharacters < fullText.Length;
    public string DisplayedText => bodyText != null ? bodyText.text : string.Empty;
    public string CurrentSequenceId { get; private set; }

    private readonly List<DialogueLine> playableLines = new List<DialogueLine>();
    private CanvasGroup panel;
    private Text speakerText;
    private Text bodyText;
    private Image portrait;
    private PlayerController player;
    private Rigidbody2D playerBody;
    private bool open;
    private bool previousControls;
    private bool previousSimulation;
    private float previousTimeScale;
    private float revealProgress;
    private int visibleCharacters;
    private int inputFrame;
    private string fullText = string.Empty;
    private Coroutine releaseRoutine;

    public static DialoguePresenter ForScene(Scene scene)
    {
        if (Instance != null && Instance.gameObject.scene == scene) return Instance;
        GameObject owner = new GameObject("DialoguePresenter");
        SceneManager.MoveGameObjectToScene(owner, scene);
        return owner.AddComponent<DialoguePresenter>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CreatePanel();
        panel.gameObject.SetActive(false);
    }

    private void OnEnable() => GameManager.RetryCompleted += HandleRetry;

    private void OnDisable()
    {
        GameManager.RetryCompleted -= HandleRetry;
        if (open)
        {
            open = false;
            panel.gameObject.SetActive(false);
            Time.timeScale = previousTimeScale;
            GameCursorController.ClearRequestedState();
        }
        if (releaseRoutine != null) StopCoroutine(releaseRoutine);
        releaseRoutine = null;
        RestorePlayer();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool Show(DialogueSequence sequence, PlayerController targetPlayer)
    {
        // Empty authoring slots must never consume a trigger or lock the player.
        if (!isActiveAndEnabled || open || releaseRoutine != null || sequence == null
            || !sequence.HasContent || targetPlayer == null || !targetPlayer.ControlsEnabled
            || Time.timeScale < 0.99f
            || (GameManager.Instance != null && GameManager.Instance.IsBlockingUiVisible)) return false;

        playableLines.Clear();
        foreach (DialogueLine line in sequence.Lines)
            if (line != null && !string.IsNullOrWhiteSpace(line.text)) playableLines.Add(line);

        player = targetPlayer;
        playerBody = player.GetComponent<Rigidbody2D>();
        previousControls = player.ControlsEnabled;
        previousSimulation = playerBody != null && playerBody.simulated;
        previousTimeScale = Time.timeScale;
        CurrentSequenceId = sequence.SequenceId;
        LineIndex = 0;
        open = true;
        player.SetControlEnabled(false);
        // Stop gravity too: a grounded dialogue must not resume with a stored fall.
        if (playerBody != null) playerBody.simulated = false;
        Time.timeScale = 0f;
        panel.alpha = 0f;
        panel.gameObject.SetActive(true);
        GameCursorController.RequestState(GameCursorController.CursorState.Dialogue);
        ShowLine();
        return true;
    }

    private void Update()
    {
        if (!open) return;
        if (GameManager.Instance != null && GameManager.Instance.IsBlockingUiVisible)
        {
            Close(false);
            return;
        }
        panel.alpha = fadeSeconds <= 0f ? 1f
            : Mathf.MoveTowards(panel.alpha, 1f, Time.unscaledDeltaTime / fadeSeconds);
        if (IsTyping)
        {
            revealProgress += Time.unscaledDeltaTime * charactersPerSecond;
            int nextCount = Mathf.Min(fullText.Length, Mathf.FloorToInt(revealProgress));
            // Do not reveal half of a UTF-16 surrogate pair.
            if (nextCount > 0 && nextCount < fullText.Length
                && char.IsHighSurrogate(fullText[nextCount - 1])) nextCount--;
            if (nextCount != visibleCharacters)
            {
                visibleCharacters = nextCount;
                bodyText.text = fullText.Substring(0, visibleCharacters);
            }
        }
        if (Time.frameCount <= inputFrame) return;
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E)) Advance();
        else if (Input.GetKeyDown(KeyCode.Tab)) Skip();
    }

    private void ShowLine()
    {
        DialogueLine line = playableLines[LineIndex];
        fullText = line.text;
        speakerText.text = line.speaker ?? string.Empty;
        bodyText.text = string.Empty;
        portrait.sprite = line.portrait;
        portrait.enabled = line.portrait != null;
        visibleCharacters = 0;
        revealProgress = 0f;
        inputFrame = Time.frameCount;
    }

    public void Advance()
    {
        if (!open || Time.frameCount < inputFrame) return;
        inputFrame = Time.frameCount + 1;
        if (IsTyping)
        {
            visibleCharacters = fullText.Length;
            revealProgress = visibleCharacters;
            bodyText.text = fullText;
            return;
        }
        if (++LineIndex < playableLines.Count) ShowLine();
        else Close(true);
    }

    public void Skip()
    {
        if (open) Close(true);
    }

    private void Close(bool restoreControls)
    {
        if (!open) return;
        open = false;
        panel.gameObject.SetActive(false);
        Time.timeScale = previousTimeScale;
        GameCursorController.ClearRequestedState();
        if (restoreControls && isActiveAndEnabled)
            releaseRoutine = StartCoroutine(ReleaseAfterInputFrame());
        else ClearPlayerReference();
    }

    private IEnumerator ReleaseAfterInputFrame()
    {
        // Closing via keyboard or UI must not also move/attack in that frame.
        yield return null;
        RestorePlayer();
        releaseRoutine = null;
    }

    private void RestorePlayer()
    {
        if (player != null && (GameManager.Instance == null || !GameManager.Instance.IsBlockingUiVisible))
        {
            player.ResetMotion();
            if (playerBody != null) playerBody.simulated = previousSimulation;
            player.SetControlEnabled(previousControls);
        }
        ClearPlayerReference();
    }

    private void ClearPlayerReference()
    {
        player = null;
        playerBody = null;
    }

    private void HandleRetry()
    {
        // Retry has already restored the player's body and health; preserve that state.
        if (open) Close(false);
        if (releaseRoutine != null) StopCoroutine(releaseRoutine);
        releaseRoutine = null;
        ClearPlayerReference();
    }

    private void CreatePanel()
    {
        GameObject canvasObject = new GameObject("DialogueCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelObject = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 24f);
        rect.sizeDelta = new Vector2(1040f, 194f);
        panelObject.GetComponent<Image>().color = new Color32(0x20, 0x16, 0x30, 0xEF);
        panel = panelObject.GetComponent<CanvasGroup>();

        GameObject portraitObject = new GameObject("DialoguePortrait", typeof(RectTransform), typeof(Image));
        portraitObject.transform.SetParent(panelObject.transform, false);
        RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
        portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0f, 0.5f);
        portraitRect.anchoredPosition = new Vector2(90f, 0f);
        portraitRect.sizeDelta = new Vector2(140f, 140f);
        portrait = portraitObject.GetComponent<Image>();
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;

        speakerText = AddText("DialogueSpeaker", panelObject.transform, new Vector2(190f, -12f),
            new Vector2(650f, 34f), 24);
        speakerText.color = new Color32(0xFA, 0xDF, 0xA2, 0xFF);
        bodyText = AddText("DialogueBody", panelObject.transform, new Vector2(190f, -48f),
            new Vector2(790f, 90f), 24);
        bodyText.supportRichText = false;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Truncate;
        AddButton("DialogueNext", "继续  [Enter / E]", panelObject.transform,
            new Vector2(824f, -158f), new Vector2(196f, 32f), Advance);
        AddButton("DialogueSkip", "跳过  [Tab]", panelObject.transform,
            new Vector2(625f, -158f), new Vector2(165f, 32f), Skip);
    }

    private static Text AddText(string name, Transform parent, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject owner = new GameObject(name, typeof(RectTransform), typeof(Text));
        owner.transform.SetParent(parent, false);
        RectTransform rect = owner.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text text = owner.GetComponent<Text>();
        text.fontSize = fontSize;
        text.alignment = TextAnchor.UpperLeft;
        text.color = new Color32(0xF4, 0xEE, 0xDD, 0xFF);
        text.raycastTarget = false;
        NightShrineTextStyle.Apply(text);
        return text;
    }

    private static void AddButton(string name, string label, Transform parent, Vector2 position,
        Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject owner = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        owner.transform.SetParent(parent, false);
        RectTransform rect = owner.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        owner.GetComponent<Image>().color = new Color32(0x72, 0x53, 0x83, 0xFF);
        owner.GetComponent<Button>().onClick.AddListener(action);
        Text text = AddText(name + "Label", owner.transform, Vector2.zero, size, 18);
        text.text = label;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        text.alignment = TextAnchor.MiddleCenter;
    }
}
