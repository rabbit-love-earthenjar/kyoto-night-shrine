using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameCursorController : MonoBehaviour
{
    public enum CursorState
    {
        Normal,
        Hover,
        Click,
        Forbidden,
        Hand,
        Dialogue
    }

    private const string BossSceneName = "Stage_1_Boss_RedOni";
    private static GameCursorController instance;

    [SerializeField, Min(24f)] private float cursorHeight = 92f;
    [SerializeField] private Vector2 cursorPivot = new Vector2(0.17f, 0.84f);

    private GameUiTheme uiTheme;
    private Canvas cursorCanvas;
    private Image cursorImage;
    private CursorState requestedState = CursorState.Normal;
    private bool hasRequestedState;
    private bool ownsCursor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject("GameCursorController");
        instance = controllerObject.AddComponent<GameCursorController>();
        DontDestroyOnLoad(controllerObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        uiTheme = GameUiTheme.LoadDefault();
        CreateSoftwareCursor();
        SceneManager.activeSceneChanged += HandleSceneChanged;
        RefreshOwnership(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        SceneManager.activeSceneChanged -= HandleSceneChanged;
        Cursor.visible = true;
        instance = null;
    }

    private void Update()
    {
        if (!ownsCursor || cursorImage == null)
        {
            return;
        }

        cursorImage.rectTransform.position = Input.mousePosition;

        CursorState state;
        if (Input.GetMouseButton(0))
        {
            state = CursorState.Click;
        }
        else if (hasRequestedState)
        {
            state = requestedState;
        }
        else if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            state = CursorState.Hover;
        }
        else
        {
            state = CursorState.Normal;
        }

        ApplyState(state);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (ownsCursor)
        {
            Cursor.visible = !hasFocus;
        }
    }

    public static void RequestState(CursorState state)
    {
        if (instance == null)
        {
            return;
        }

        instance.requestedState = state;
        instance.hasRequestedState = true;
    }

    public static void ClearRequestedState()
    {
        if (instance != null)
        {
            instance.hasRequestedState = false;
        }
    }

    private void CreateSoftwareCursor()
    {
        if (uiTheme == null || uiTheme.CursorNormal == null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("GameCursorCanvas");
        canvasObject.transform.SetParent(transform, false);
        cursorCanvas = canvasObject.AddComponent<Canvas>();
        cursorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cursorCanvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject cursorObject = new GameObject("CursorVisual");
        cursorObject.transform.SetParent(canvasObject.transform, false);
        cursorImage = cursorObject.AddComponent<Image>();
        cursorImage.raycastTarget = false;
        cursorImage.preserveAspect = true;
        cursorImage.rectTransform.anchorMin = Vector2.zero;
        cursorImage.rectTransform.anchorMax = Vector2.zero;
        cursorImage.rectTransform.pivot = cursorPivot;
        ApplyState(CursorState.Normal);
    }

    private void HandleSceneChanged(Scene previousScene, Scene nextScene)
    {
        hasRequestedState = false;
        RefreshOwnership(nextScene);
    }

    private void RefreshOwnership(Scene scene)
    {
        ownsCursor = scene.IsValid() && scene.name != BossSceneName && cursorImage != null;
        if (cursorCanvas != null)
        {
            cursorCanvas.enabled = ownsCursor;
        }

        Cursor.visible = !ownsCursor;
        if (!ownsCursor)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    private void ApplyState(CursorState state)
    {
        Sprite sprite = ResolveSprite(state);
        if (sprite == null || cursorImage.sprite == sprite)
        {
            return;
        }

        cursorImage.sprite = sprite;
        float aspect = sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : 1f;
        cursorImage.rectTransform.sizeDelta = new Vector2(cursorHeight * aspect, cursorHeight);
    }

    private Sprite ResolveSprite(CursorState state)
    {
        if (uiTheme == null)
        {
            return null;
        }

        switch (state)
        {
            case CursorState.Hover:
                return uiTheme.CursorHover;
            case CursorState.Click:
                return uiTheme.CursorClick;
            case CursorState.Forbidden:
                return uiTheme.CursorForbidden;
            case CursorState.Hand:
                return uiTheme.CursorHand;
            case CursorState.Dialogue:
                return uiTheme.CursorDialogue;
            default:
                return uiTheme.CursorNormal;
        }
    }
}
