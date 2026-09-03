using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartScreenController : MonoBehaviour
{
    private enum ScreenState
    {
        PressStart,
        Menu,
        Transitioning
    }

    private enum MenuAction
    {
        NewGame,
        Continue,
        Settings,
        Credits
    }

    [System.Serializable]
    private sealed class MenuEntry
    {
        [SerializeField] private Button button;
        [SerializeField] private Image image;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private MenuAction action;
        [SerializeField, Range(0f, 1f)] private float normalAlpha = 0.72f;
        [SerializeField, Range(0f, 1f)] private float selectedAlpha = 0.95f;
        [SerializeField, Min(1f)] private float selectedScale = 1.08f;

        public Button Button => button;
        public MenuAction Action => action;
        public bool IsInteractable => button != null && button.interactable;

        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }

            if (image != null)
            {
                Color color = image.color;
                color.a = interactable ? normalAlpha : Mathf.Min(normalAlpha, 0.35f);
                image.color = color;
            }
        }

        public void SetSelected(bool selected)
        {
            bool available = button == null || button.interactable;
            selected &= available;

            if (image != null)
            {
                image.sprite = selected && selectedSprite != null ? selectedSprite : normalSprite;
                Color color = image.color;
                float visibleNormalAlpha = normalAlpha > 0f ? normalAlpha : 0.72f;
                float visibleSelectedAlpha = selectedAlpha > 0f ? selectedAlpha : 0.95f;
                color.a = available
                    ? selected ? visibleSelectedAlpha : visibleNormalAlpha
                    : Mathf.Min(visibleNormalAlpha, 0.35f);
                image.color = color;
            }

            RectTransform targetRect = button != null
                ? button.transform as RectTransform
                : image != null ? image.rectTransform : null;
            if (targetRect != null)
            {
                UiSelectionScale scaleEffect = targetRect.GetComponent<UiSelectionScale>();
                if (scaleEffect == null)
                {
                    scaleEffect = targetRect.gameObject.AddComponent<UiSelectionScale>();
                }

                if (!scaleEffect.IsConfigured)
                {
                    Text label = button != null ? button.GetComponentInChildren<Text>(true) : null;
                    scaleEffect.Configure(
                        selectedScale,
                        image,
                        normalAlpha,
                        selectedAlpha,
                        image != null ? image.rectTransform : targetRect,
                        label != null ? label.rectTransform : null);
                }
                scaleEffect.SetSelected(selected);
            }
        }
    }

    [System.Serializable]
    private sealed class SpriteLoop
    {
        [SerializeField] private Image target;
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(0.05f)] private float secondsPerFrame = 0.3f;
        [SerializeField, Min(0)] private int startFrameOffset;

        public void UpdateFrame(float elapsedTime)
        {
            if (target == null || frames == null || frames.Length == 0)
            {
                return;
            }

            int frameIndex = Mathf.FloorToInt(elapsedTime / Mathf.Max(0.05f, secondsPerFrame));
            frameIndex = (frameIndex + startFrameOffset) % frames.Length;
            target.sprite = frames[frameIndex];
        }
    }

    [Header("Required Bindings")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Graphic pressStartGraphic;
    [SerializeField] private Button startButton;

    [Header("Start Menu")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private MenuEntry[] menuEntries;
    [SerializeField] private RectTransform selectionCursor;
    [SerializeField] private Image selectionCursorImage;
    [SerializeField] private Sprite[] selectionCursorFrames;
    [SerializeField, Min(0.05f)] private float cursorSecondsPerFrame = 0.14f;
    [SerializeField] private Vector2 cursorOffset = new Vector2(-260f, 0f);
    [SerializeField] private CanvasGroup titleLogoGroup;
    [SerializeField] private RectTransform titleLogoRect;
    [SerializeField] private CanvasGroup menuRootCanvasGroup;
    [SerializeField] private Vector2 titleIntroPosition = Vector2.zero;
    [SerializeField] private Vector2 titleFinalPosition = new Vector2(0f, 305f);
    [SerializeField, Min(0.01f)] private float titleFadeSeconds = 0.32f;
    [SerializeField, Min(0f)] private float titleHoldSeconds = 0.08f;
    [SerializeField, Min(0.01f)] private float titleMoveSeconds = 0.7f;
    [SerializeField, Min(0.01f)] private float menuRevealSeconds = 0.38f;
    [SerializeField, Range(0f, 1f)] private float titleVisibleAlpha = 0.9f;
    [SerializeField, Range(0f, 1f)] private float menuVisibleAlpha = 0.94f;

    [Header("Start Menu Music")]
    [SerializeField] private AudioSource menuBgmSource;
    [SerializeField] private AudioClip[] menuBgmClips;
    [SerializeField, Range(0f, 1f)] private float menuBgmVolume = 0.3f;

    [Header("Scene Flow")]
    [SerializeField] private string newGameSceneName = "Stage_1_2";
    [SerializeField] private string continueGameSceneName = "HubMap_Day";
    [SerializeField, Min(0f)] private float inputDelaySeconds = 0.45f;
    [SerializeField, Min(0.01f)] private float fadeInSeconds = 1.25f;
    [SerializeField, Min(0.01f)] private float fadeOutSeconds = 0.65f;

    [Header("Press Start")]
    [SerializeField, Min(0.1f)] private float blinkCycleSeconds = 1.2f;
    [SerializeField, Range(0f, 1f)] private float minimumBlinkAlpha = 0.3f;

    [Header("Optional Lantern Glow")]
    [SerializeField] private CanvasGroup[] lanternGlowGroups;
    [SerializeField, Min(0.1f)] private float glowCycleSeconds = 2.4f;
    [SerializeField, Range(0f, 1f)] private float minimumGlowAlpha = 0.42f;
    [SerializeField, Range(0f, 1f)] private float maximumGlowAlpha = 0.74f;

    [Header("Optional Floating Elements")]
    [SerializeField] private RectTransform[] floatingElements;
    [SerializeField, Min(0f)] private float floatDistance = 8f;
    [SerializeField, Min(0.1f)] private float floatCycleSeconds = 2.8f;

    [Header("Optional Sprite Loops")]
    [SerializeField] private SpriteLoop[] spriteLoops;

    private Vector2[] floatingStartPositions;
    private Color pressStartBaseColor = Color.white;
    private float elapsedTime;
    private bool canStart;
    private bool isTransitioning;
    private int selectedMenuIndex;
    private string pendingSceneName;
    private ScreenState screenState = ScreenState.PressStart;

    private void Awake()
    {
        CacheVisualDefaults();

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.interactable = false;
            fadeCanvasGroup.blocksRaycasts = false;
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(ShowMenu);
            startButton.interactable = false;
        }

        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }

        if (titleLogoGroup != null)
        {
            titleLogoGroup.alpha = 0f;
            titleLogoGroup.interactable = false;
            titleLogoGroup.blocksRaycasts = false;
        }

        if (titleLogoRect != null)
        {
            titleLogoRect.anchoredPosition = titleIntroPosition;
        }

        BindMenuButtons();
        RefreshContinueAvailability();
    }

    private IEnumerator Start()
    {
        yield return FadeCanvas(0f, 1f, fadeInSeconds);

        float remainingDelay = Mathf.Max(0f, inputDelaySeconds - elapsedTime);
        while (remainingDelay > 0f)
        {
            float deltaTime = Time.unscaledDeltaTime;
            remainingDelay -= deltaTime;
            elapsedTime += deltaTime;
            yield return null;
        }

        canStart = true;

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.interactable = true;
            fadeCanvasGroup.blocksRaycasts = true;
        }

        if (startButton != null)
        {
            startButton.interactable = true;
        }
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        elapsedTime += deltaTime;

        UpdatePressStartBlink();
        UpdateLanternGlow();
        UpdateFloatingElements();
        UpdateSpriteLoops();

        if (!canStart || isTransitioning)
        {
            return;
        }

        if (screenState == ScreenState.PressStart && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
        {
            ShowMenu();
        }
        else if (screenState == ScreenState.Menu)
        {
            UpdateMenuInput();
            UpdateSelectionCursor();
        }
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(ShowMenu);
        }

    }

    public void ShowMenu()
    {
        if (!canStart || isTransitioning || screenState != ScreenState.PressStart)
        {
            return;
        }

        screenState = ScreenState.Menu;
        if (pressStartGraphic != null)
        {
            pressStartGraphic.gameObject.SetActive(false);
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(false);
        }

        if (menuRoot != null)
        {
            menuRoot.SetActive(true);
        }

        if (menuRootCanvasGroup != null)
        {
            menuRootCanvasGroup.alpha = 0f;
            menuRootCanvasGroup.interactable = true;
            menuRootCanvasGroup.blocksRaycasts = true;
        }

        SelectMenuItem(0);
        StartRandomMenuMusic();
        StartCoroutine(RevealTitleAndMenu());
    }

    public void SelectMenuItem(int index)
    {
        if (screenState != ScreenState.Menu || menuEntries == null || menuEntries.Length == 0)
        {
            return;
        }

        int candidateIndex = Mathf.Clamp(index, 0, menuEntries.Length - 1);
        if (menuEntries[candidateIndex] == null || !menuEntries[candidateIndex].IsInteractable)
        {
            return;
        }

        selectedMenuIndex = candidateIndex;
        for (int entryIndex = 0; entryIndex < menuEntries.Length; entryIndex++)
        {
            menuEntries[entryIndex]?.SetSelected(entryIndex == selectedMenuIndex);
        }

        UpdateSelectionCursorPosition();
    }

    public void ActivateMenuItem(int index)
    {
        if (screenState != ScreenState.Menu || isTransitioning)
        {
            return;
        }

        if (menuEntries == null
            || index < 0
            || index >= menuEntries.Length
            || menuEntries[index] == null
            || !menuEntries[index].IsInteractable)
        {
            return;
        }

        SelectMenuItem(index);
        MenuAction action = menuEntries[selectedMenuIndex].Action;
        switch (action)
        {
            case MenuAction.NewGame:
                SaveManager.StartNewGame(newGameSceneName);
                BeginSceneTransition(newGameSceneName, "new game");
                break;
            case MenuAction.Continue:
                if (SaveManager.TryGetContinueScene(continueGameSceneName, out string continueScene))
                {
                    BeginSceneTransition(continueScene, "continue game");
                }
                break;
            case MenuAction.Settings:
                Debug.Log("Start menu settings are reserved for a later settings panel.", this);
                break;
            case MenuAction.Credits:
                Debug.Log("Start menu credits are reserved for a later credits panel.", this);
                break;
        }
    }

    public void BeginGame()
    {
        SaveManager.StartNewGame(newGameSceneName);
        BeginSceneTransition(newGameSceneName, "new game");
    }

    private void RefreshContinueAvailability()
    {
        if (menuEntries == null)
        {
            return;
        }

        for (int index = 0; index < menuEntries.Length; index++)
        {
            MenuEntry entry = menuEntries[index];
            if (entry != null && entry.Action == MenuAction.Continue)
            {
                entry.SetInteractable(SaveManager.HasAutosave);
            }
        }
    }

    private void BeginSceneTransition(string sceneName, string flowName)
    {
        if (!canStart || isTransitioning)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"StartScreenController cannot start {flowName} because its scene name is empty.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"StartScreenController cannot start {flowName} because scene '{sceneName}' is not enabled in Build Settings.",
                this);
            return;
        }

        pendingSceneName = sceneName;
        isTransitioning = true;
        canStart = false;
        screenState = ScreenState.Transitioning;

        if (startButton != null)
        {
            startButton.interactable = false;
        }

        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        float currentAlpha = fadeCanvasGroup != null ? fadeCanvasGroup.alpha : 1f;
        yield return FadeCanvas(currentAlpha, 0f, fadeOutSeconds);

        Time.timeScale = 1f;
        SceneManager.LoadScene(pendingSceneName);
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        float time = 0f;
        fadeCanvasGroup.alpha = from;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(time / duration));
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
    }

    private void CacheVisualDefaults()
    {
        if (pressStartGraphic != null)
        {
            pressStartBaseColor = pressStartGraphic.color;
        }

        int elementCount = floatingElements != null ? floatingElements.Length : 0;
        floatingStartPositions = new Vector2[elementCount];

        for (int index = 0; index < elementCount; index++)
        {
            if (floatingElements[index] != null)
            {
                floatingStartPositions[index] = floatingElements[index].anchoredPosition;
            }
        }
    }

    private void UpdatePressStartBlink()
    {
        if (screenState != ScreenState.PressStart || pressStartGraphic == null)
        {
            return;
        }

        float wave = PingPong01(elapsedTime, blinkCycleSeconds);
        Color color = pressStartBaseColor;
        color.a *= Mathf.Lerp(minimumBlinkAlpha, 1f, wave);
        pressStartGraphic.color = color;
    }

    private void UpdateLanternGlow()
    {
        if (lanternGlowGroups == null || lanternGlowGroups.Length == 0)
        {
            return;
        }

        for (int index = 0; index < lanternGlowGroups.Length; index++)
        {
            CanvasGroup glow = lanternGlowGroups[index];
            if (glow == null)
            {
                continue;
            }

            float offsetTime = elapsedTime + index * glowCycleSeconds * 0.17f;
            float wave = PingPong01(offsetTime, glowCycleSeconds);
            float upperAlpha = Mathf.Max(minimumGlowAlpha, maximumGlowAlpha);
            glow.alpha = Mathf.Lerp(minimumGlowAlpha, upperAlpha, wave);
        }
    }

    private void UpdateFloatingElements()
    {
        if (floatingElements == null || floatingStartPositions == null)
        {
            return;
        }

        for (int index = 0; index < floatingElements.Length; index++)
        {
            RectTransform element = floatingElements[index];
            if (element == null)
            {
                continue;
            }

            float phase = index * 0.85f;
            float angle = (elapsedTime / floatCycleSeconds) * Mathf.PI * 2f + phase;
            Vector2 offset = new Vector2(Mathf.Cos(angle) * floatDistance * 0.25f, Mathf.Sin(angle) * floatDistance);
            element.anchoredPosition = floatingStartPositions[index] + offset;
        }
    }

    private void UpdateSpriteLoops()
    {
        if (spriteLoops == null)
        {
            return;
        }

        for (int index = 0; index < spriteLoops.Length; index++)
        {
            spriteLoops[index]?.UpdateFrame(elapsedTime);
        }
    }

    private void BindMenuButtons()
    {
        if (menuEntries == null)
        {
            return;
        }

        for (int index = 0; index < menuEntries.Length; index++)
        {
            int capturedIndex = index;
            Button button = menuEntries[index]?.Button;
            if (button != null)
            {
                button.onClick.AddListener(() => ActivateMenuItem(capturedIndex));
            }
        }
    }

    private void UpdateMenuInput()
    {
        if (menuEntries == null || menuEntries.Length == 0)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            MoveSelection(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            MoveSelection(1);
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            ActivateMenuItem(selectedMenuIndex);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToPressStart();
        }
    }

    private void MoveSelection(int direction)
    {
        for (int offset = 1; offset <= menuEntries.Length; offset++)
        {
            int candidate = (selectedMenuIndex + direction * offset + menuEntries.Length) % menuEntries.Length;
            if (menuEntries[candidate] != null && menuEntries[candidate].IsInteractable)
            {
                SelectMenuItem(candidate);
                return;
            }
        }
    }

    private void ReturnToPressStart()
    {
        screenState = ScreenState.PressStart;
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }

        if (titleLogoGroup != null)
        {
            titleLogoGroup.alpha = 0f;
        }

        if (titleLogoRect != null)
        {
            titleLogoRect.anchoredPosition = titleIntroPosition;
        }

        if (pressStartGraphic != null)
        {
            pressStartGraphic.gameObject.SetActive(true);
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.interactable = true;
        }
    }

    private void UpdateSelectionCursor()
    {
        if (selectionCursorImage != null && selectionCursorFrames != null && selectionCursorFrames.Length > 0)
        {
            int frame = Mathf.FloorToInt(elapsedTime / cursorSecondsPerFrame) % selectionCursorFrames.Length;
            selectionCursorImage.sprite = selectionCursorFrames[frame];
        }

        if (selectionCursor != null)
        {
            float bob = Mathf.Sin(elapsedTime * 3.4f) * 5f;
            UpdateSelectionCursorPosition(new Vector2(0f, bob));
        }
    }

    private void UpdateSelectionCursorPosition()
    {
        UpdateSelectionCursorPosition(Vector2.zero);
    }

    private void UpdateSelectionCursorPosition(Vector2 animatedOffset)
    {
        if (selectionCursor == null || menuEntries == null || selectedMenuIndex < 0 || selectedMenuIndex >= menuEntries.Length)
        {
            return;
        }

        Button selectedButton = menuEntries[selectedMenuIndex]?.Button;
        if (selectedButton == null)
        {
            return;
        }

        RectTransform buttonRect = selectedButton.transform as RectTransform;
        RectTransform cursorParent = selectionCursor.parent as RectTransform;
        if (buttonRect == null || cursorParent == null)
        {
            return;
        }

        Vector3 worldPosition = buttonRect.TransformPoint(Vector3.zero);
        Vector2 localPosition = cursorParent.InverseTransformPoint(worldPosition);
        selectionCursor.anchoredPosition = localPosition + cursorOffset + animatedOffset;
    }

    private IEnumerator RevealTitleAndMenu()
    {
        if (titleLogoRect != null)
        {
            titleLogoRect.anchoredPosition = titleIntroPosition;
        }

        float elapsed = 0f;
        while (elapsed < titleFadeSeconds && screenState == ScreenState.Menu)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / titleFadeSeconds);
            float eased = progress * progress * (3f - 2f * progress);

            if (titleLogoGroup != null)
            {
                titleLogoGroup.alpha = Mathf.Lerp(0f, titleVisibleAlpha, eased);
            }

            yield return null;
        }

        if (screenState != ScreenState.Menu)
        {
            yield break;
        }

        if (titleLogoGroup != null)
        {
            titleLogoGroup.alpha = titleVisibleAlpha;
        }

        if (titleHoldSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(titleHoldSeconds);
        }

        elapsed = 0f;
        while (elapsed < titleMoveSeconds && screenState == ScreenState.Menu)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / titleMoveSeconds);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            if (titleLogoRect != null)
            {
                titleLogoRect.anchoredPosition = Vector2.LerpUnclamped(titleIntroPosition, titleFinalPosition, eased);
            }
            yield return null;
        }

        if (titleLogoRect != null)
        {
            titleLogoRect.anchoredPosition = titleFinalPosition;
        }

        elapsed = 0f;
        while (elapsed < menuRevealSeconds && screenState == ScreenState.Menu)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / menuRevealSeconds);
            float eased = progress * progress * (3f - 2f * progress);
            if (menuRootCanvasGroup != null)
            {
                menuRootCanvasGroup.alpha = Mathf.Lerp(0f, menuVisibleAlpha, eased);
            }
            yield return null;
        }

        if (screenState == ScreenState.Menu)
        {
            if (titleLogoGroup != null)
            {
                titleLogoGroup.alpha = titleVisibleAlpha;
            }

            if (menuRootCanvasGroup != null)
            {
                menuRootCanvasGroup.alpha = menuVisibleAlpha;
            }
        }
    }

    private void StartRandomMenuMusic()
    {
        if (menuBgmSource == null || menuBgmSource.isPlaying || menuBgmClips == null || menuBgmClips.Length == 0)
        {
            return;
        }

        AudioClip clip = menuBgmClips[Random.Range(0, menuBgmClips.Length)];
        if (clip == null)
        {
            Debug.LogWarning("Start menu BGM selection contains an empty AudioClip reference.", this);
            return;
        }

        menuBgmSource.clip = clip;
        menuBgmSource.loop = true;
        menuBgmSource.playOnAwake = false;
        menuBgmSource.volume = menuBgmVolume * GameSettings.BgmVolume;
        menuBgmSource.Play();
    }

    private static float PingPong01(float time, float cycleSeconds)
    {
        return Mathf.PingPong(time / Mathf.Max(0.1f, cycleSeconds), 1f);
    }
}
