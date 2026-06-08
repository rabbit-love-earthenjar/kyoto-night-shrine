using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HubMapController : MonoBehaviour
{
    [Header("Hub Panels")]
    [SerializeField] private string warehouseTitle = "資材小屋";
    [SerializeField] private string shrineTitle = "荒れた小神社";
    [SerializeField] private string shrineStatus = "まだ修復されていません";
    [SerializeField] private string shrineRepairedStatus = "修復が完了しました";

    [Header("Shrine Repair")]
    [SerializeField] private int shrineRepairFaithCost = 10;
    [SerializeField] private bool persistShrineRepair = true;
    [SerializeField] private string shrineRepairSaveKey = "HubMap_Day.ShrineRepaired";
    [SerializeField] private string cafeSceneName = "CafeInterior_Temporary";

    [Header("Night Stage Select")]
    [SerializeField] private string nightSceneName = "Stage_0_0";
    [SerializeField] private string stageOneTwoSceneName = "Stage_1_1";
    [SerializeField] private string stageOneThreeSceneName = "Stage_1_2";
    [SerializeField] private bool stageOneTwoAvailable = true;
    [SerializeField] private bool stageOneThreeAvailable = true;
    [SerializeField] private Sprite nightStageSelectBackgroundSprite;
    [SerializeField] private Sprite stageAvailableIconSprite;
    [SerializeField] private Sprite stageLockedIconSprite;

    [Header("Night Stage Audio")]
    [SerializeField] private LevelMenuAudioController levelMenuAudioController;
    [SerializeField] private AudioClip levelMenuHoverClip;
    [SerializeField] private AudioClip levelMenuIgniteClip;
    [SerializeField] private float levelMenuHoverVol = 0.4f;
    [SerializeField] private float levelMenuIgniteVol = 1f;
    [SerializeField] private AudioClip levelMenuBgmClip;
    [SerializeField, Range(0f, 1f)] private float levelMenuBgmVolume = 0.22f;

    [Header("Scene References")]
    [SerializeField] private Transform uiRoot;
    [SerializeField] private SpriteRenderer shrineIconRenderer;
    [SerializeField] private Sprite repairedShrineSprite;
    [SerializeField] private Vector3 repairedShrineScale = new Vector3(0.16f, 0.16f, 1f);
    [SerializeField] private Sprite nightPatrolSprite;
    [SerializeField] private Vector2 nightPatrolPosition = new Vector2(3.65f, -1.7f);
    [SerializeField] private Vector3 nightPatrolScale = new Vector3(0.23f, 0.23f, 1f);
    [SerializeField] private Vector2 nightPatrolColliderSize = new Vector2(2.4f, 2f);
    [SerializeField] private Vector2 nightPatrolLabelOffset = new Vector2(0f, -1.45f);

    private GameObject panelCanvasObject;
    private GameObject panelObject;
    private GameObject nightStageSelectPanel;
    private GameObject repairButtonObject;
    private Button repairButton;
    private Image repairButtonImage;
    private Text titleText;
    private Text bodyText;
    private Text repairButtonText;
    private Text closeButtonText;
    private ResourceInventory resourceInventory;
    private HubPlayerController hubPlayer;
    private AudioSource levelMenuBgmSource;
    private Sprite originalShrineSprite;
    private Vector3 originalShrineScale;
    private bool shrineRepaired;
    private bool shrineVisualCached;
    private Coroutine loadNightStageRoutine;
    private static Sprite stageNodeHaloSprite;
    private static bool shrineRepairedInSession;

    public bool BlocksHubInteraction => nightStageSelectPanel != null && nightStageSelectPanel.activeSelf;

    private void Awake()
    {
        shrineRepaired = shrineRepairedInSession || LoadShrineRepairState();
        ApplyShrineVisualState();
        EnsureEventSystem();
        CreateNightPatrolIcon();
    }

    public void ShowWarehousePanel()
    {
        ResourceInventory inventory = ResolveResourceInventory();
        int faithPoints = inventory != null ? inventory.FaithPoints : 0;
        int basicMaterialCount = inventory != null ? inventory.GetMaterialCount(ResourceInventory.BasicYokaiMaterialId) : 0;

        HideNightStageSelectPanel();
        EnsurePanel();
        titleText.text = warehouseTitle;
        bodyText.text = $"FaithPoints: {faithPoints}\n{ResourceInventory.BasicYokaiMaterialId}: {basicMaterialCount}";
        repairButtonObject.SetActive(false);
        closeButtonText.text = "Close";
        panelObject.SetActive(true);
    }

    public void ShowShrinePanel()
    {
        HideNightStageSelectPanel();
        EnsurePanel();
        titleText.text = shrineTitle;
        repairButtonObject.SetActive(true);
        closeButtonText.text = "Close";
        RefreshShrinePanel();
        panelObject.SetActive(true);
    }

    public void HidePanel()
    {
        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }
    }

    public void TryRepairShrine()
    {
        if (shrineRepaired)
        {
            EnterCafe();
            return;
        }

        ResourceInventory inventory = ResolveResourceInventory();

        if (inventory == null || !inventory.SpendFaithPoints(shrineRepairFaithCost))
        {
            RefreshShrinePanel();
            return;
        }

        shrineRepaired = true;
        shrineRepairedInSession = true;
        SaveShrineRepairState();
        ApplyShrineVisualState();
        RefreshShrinePanel();
    }

    public void EnterCafe()
    {
        if (string.IsNullOrEmpty(cafeSceneName))
        {
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(cafeSceneName);
    }

    public void EnterNight()
    {
        ShowNightStageSelectPanel();
    }

    public void ShowNightStageSelectPanel()
    {
        EnsureNightStageSelectPanel();
        HidePanel();
        SetHubPlayerControl(false);
        nightStageSelectPanel.SetActive(true);
        nightStageSelectPanel.transform.SetAsLastSibling();
        PlayLevelMenuBgm();
    }

    public void HideNightStageSelectPanel()
    {
        if (nightStageSelectPanel != null)
        {
            nightStageSelectPanel.SetActive(false);
        }

        StopLevelMenuBgm(true);
        SetHubPlayerControl(true);
    }

    public void ShowIngredientShopPanel()
    {
        HideNightStageSelectPanel();
        HubIngredientShopController shopController = GetComponent<HubIngredientShopController>();

        if (shopController != null)
        {
            shopController.ShowPanel();
        }
    }

    private void LoadNightStage(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        SetHubPlayerControl(true);
        StopLevelMenuBgm(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void CreateNightPatrolIcon()
    {
        if (GameObject.Find("NightPatrolIcon_夜の巡回へ") != null)
        {
            return;
        }

        GameObject iconObject = new GameObject("NightPatrolIcon_夜の巡回へ");
        Transform buildingsRoot = transform.Find("Buildings");

        if (buildingsRoot != null)
        {
            iconObject.transform.SetParent(buildingsRoot, false);
        }

        iconObject.transform.position = nightPatrolPosition;

        GameObject visualObject = new GameObject("NightPatrolVisual");
        visualObject.transform.SetParent(iconObject.transform, false);
        visualObject.transform.localScale = nightPatrolScale;

        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = nightPatrolSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 2;

        BoxCollider2D collider = iconObject.AddComponent<BoxCollider2D>();
        collider.size = nightPatrolColliderSize;

        HubMapInteractable interactable = iconObject.AddComponent<HubMapInteractable>();
        interactable.Configure(this, HubInteractionType.NightPatrol);

        GameObject labelObject = new GameObject("NightPatrolLabel");
        labelObject.transform.SetParent(iconObject.transform, false);
        labelObject.transform.localPosition = nightPatrolLabelOffset;

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = "夜の巡回へ";
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 42;
        label.characterSize = 0.06f;
        label.color = new Color(0.88f, 0.92f, 1f, 1f);
    }

    private bool LoadShrineRepairState()
    {
        return persistShrineRepair
            && !string.IsNullOrEmpty(shrineRepairSaveKey)
            && PlayerPrefs.GetInt(shrineRepairSaveKey, 0) == 1;
    }

    private void SaveShrineRepairState()
    {
        if (!persistShrineRepair || string.IsNullOrEmpty(shrineRepairSaveKey))
        {
            return;
        }

        PlayerPrefs.SetInt(shrineRepairSaveKey, 1);
        PlayerPrefs.Save();
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

    private void EnsurePanelCanvas()
    {
        if (panelCanvasObject != null)
        {
            return;
        }

        panelCanvasObject = new GameObject("HubMapPanelCanvas");
        ResolveUiRoot();

        if (uiRoot != null)
        {
            panelCanvasObject.transform.SetParent(uiRoot, false);
        }

        Canvas canvas = panelCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        CanvasScaler scaler = panelCanvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        panelCanvasObject.AddComponent<GraphicRaycaster>();
    }

    private void EnsurePanel()
    {
        if (panelObject != null)
        {
            return;
        }

        EnsurePanelCanvas();

        panelObject = new GameObject("HubMapInfoPanel");
        panelObject.transform.SetParent(panelCanvasObject.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(380f, 270f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.09f, 0.1f, 0.88f);

        titleText = CreateText("Title", panelObject.transform, new Vector2(0f, 88f), new Vector2(320f, 42f), 28, TextAnchor.MiddleCenter);
        bodyText = CreateText("Body", panelObject.transform, new Vector2(0f, 32f), new Vector2(320f, 82f), 21, TextAnchor.MiddleCenter);
        CreateRepairButton(panelObject.transform);
        CreateCloseButton(panelObject.transform);

        panelObject.SetActive(false);
    }

    private void EnsureNightStageSelectPanel()
    {
        if (nightStageSelectPanel != null)
        {
            return;
        }

        EnsurePanelCanvas();

        nightStageSelectPanel = new GameObject("NightStageSelectPanel");
        nightStageSelectPanel.transform.SetParent(panelCanvasObject.transform, false);

        RectTransform panelRect = nightStageSelectPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = Vector2.zero;

        Image panelImage = nightStageSelectPanel.AddComponent<Image>();
        panelImage.sprite = nightStageSelectBackgroundSprite;
        panelImage.preserveAspect = false;
        panelImage.color = nightStageSelectBackgroundSprite != null
            ? new Color(0.9f, 0.92f, 1f, 1f)
            : new Color(0.035f, 0.045f, 0.07f, 0.92f);

        CreateDecorPanel("NightStageDarkVeil", nightStageSelectPanel.transform, Vector2.zero, new Vector2(1280f, 720f), new Color(0.02f, 0.025f, 0.055f, 0.12f));
        CreateDecorPanel("NightStageHeader", nightStageSelectPanel.transform, new Vector2(0f, 278f), new Vector2(500f, 62f), new Color(0.035f, 0.03f, 0.065f, 0.58f));

        Text title = CreateText("夜の巡回", nightStageSelectPanel.transform, new Vector2(0f, 202f), new Vector2(420f, 44f), 34, TextAnchor.MiddleCenter);
        title.color = new Color(0.98f, 0.9f, 1f, 1f);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, 278f);
        titleRect.sizeDelta = new Vector2(480f, 50f);
        title.fontSize = 38;

        CreateButtonWithLabel("BackButton", "← 戻る", nightStageSelectPanel.transform, new Vector2(-372f, 210f), new Vector2(118f, 38f), HideNightStageSelectPanel);
        CreateStageNode("StageNode_1_1", "1", new Vector2(-380f, -100f), true, stageAvailableIconSprite, () => PlayIgniteAndLoadNightStage(nightSceneName));
        CreateStageNode("StageNode_1_2", "2", new Vector2(-128f, -160f), stageOneTwoAvailable, stageOneTwoAvailable ? stageAvailableIconSprite : stageLockedIconSprite, () => PlayIgniteAndLoadNightStage(stageOneTwoSceneName));
        CreateStageNode("StageNode_1_3", "3", new Vector2(50f, -64f), stageOneThreeAvailable, stageOneThreeAvailable ? stageAvailableIconSprite : stageLockedIconSprite, () => PlayIgniteAndLoadNightStage(stageOneThreeSceneName));
        CreateStageNode("StageNode_Boss", "4", new Vector2(208f, -124f), false, stageLockedIconSprite, null);

        nightStageSelectPanel.SetActive(false);
    }

    private void PlayLevelMenuBgm()
    {
        if (levelMenuBgmClip == null)
        {
            return;
        }

        EnsureLevelMenuBgmSource();

        if (levelMenuBgmSource == null)
        {
            return;
        }

        if (levelMenuBgmSource.clip != levelMenuBgmClip)
        {
            levelMenuBgmSource.clip = levelMenuBgmClip;
        }

        levelMenuBgmSource.loop = true;
        levelMenuBgmSource.volume = Mathf.Max(0f, levelMenuBgmVolume);
        levelMenuBgmSource.mute = false;
        levelMenuBgmSource.enabled = true;
        levelMenuBgmSource.Stop();
        levelMenuBgmSource.time = 0f;
        levelMenuBgmSource.Play();

        GameAudio.PauseBgmForOverlay();
    }

    private void StopLevelMenuBgm(bool resumeHubBgm)
    {
        if (levelMenuBgmSource != null && levelMenuBgmSource.isPlaying)
        {
            levelMenuBgmSource.Stop();
            levelMenuBgmSource.time = 0f;
        }

        if (resumeHubBgm)
        {
            GameAudio.ResumeBgmFromOverlay();
        }
    }

    private void EnsureLevelMenuBgmSource()
    {
        if (levelMenuBgmSource != null)
        {
            return;
        }

        Transform sourceTransform = transform.Find("NightStageSelectBgmSource");

        if (sourceTransform == null)
        {
            GameObject sourceObject = new GameObject("NightStageSelectBgmSource");
            sourceObject.transform.SetParent(transform, false);
            sourceTransform = sourceObject.transform;
        }

        levelMenuBgmSource = sourceTransform.GetComponent<AudioSource>();

        if (levelMenuBgmSource == null)
        {
            levelMenuBgmSource = sourceTransform.gameObject.AddComponent<AudioSource>();
        }

        levelMenuBgmSource.playOnAwake = false;
        levelMenuBgmSource.spatialBlend = 0f;
        levelMenuBgmSource.loop = true;
        levelMenuBgmSource.ignoreListenerPause = true;
    }

    private void CreateStageNode(string objectName, string label, Vector2 position, bool interactable, Sprite iconSprite, UnityEngine.Events.UnityAction action)
    {
        Button button = CreateButton(objectName, nightStageSelectPanel.transform, position, new Vector2(138f, 150f), action);
        button.interactable = interactable;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
        }

        button.transition = Selectable.Transition.None;

        GameObject haloObject = new GameObject("StageNodeHalo");
        haloObject.transform.SetParent(button.transform, false);

        RectTransform haloRect = haloObject.AddComponent<RectTransform>();
        haloRect.anchorMin = new Vector2(0.5f, 0.5f);
        haloRect.anchorMax = new Vector2(0.5f, 0.5f);
        haloRect.pivot = new Vector2(0.5f, 0.5f);
        haloRect.anchoredPosition = new Vector2(0f, 16f);
        haloRect.sizeDelta = new Vector2(142f, 142f);

        Image haloImage = haloObject.AddComponent<Image>();
        haloImage.sprite = GetStageNodeHaloSprite();
        haloImage.preserveAspect = true;
        haloImage.raycastTarget = false;
        haloImage.color = interactable
            ? new Color(1f, 0.76f, 0.22f, 0f)
            : new Color(0.68f, 0.45f, 1f, 0f);

        if (iconSprite != null)
        {
            GameObject iconObject = new GameObject("StageIcon");
            iconObject.transform.SetParent(button.transform, false);

            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 16f);
            iconRect.sizeDelta = new Vector2(118f, 118f);

            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;
            iconImage.color = interactable ? Color.white : new Color(0.82f, 0.74f, 1f, 0.9f);
        }

        Text text = CreateText("NodeNumber", button.transform, new Vector2(0f, -58f), new Vector2(60f, 28f), 22, TextAnchor.MiddleCenter);
        text.text = label;
        text.color = interactable ? new Color(1f, 0.88f, 0.38f, 1f) : new Color(0.78f, 0.66f, 1f, 1f);

        AddStageNodeHoverAudio(button.gameObject);
    }

    private void AddStageNodeHoverAudio(GameObject target)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger = target.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry hoverEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };

        Image haloImage = target.transform.Find("StageNodeHalo")?.GetComponent<Image>();

        hoverEntry.callback.AddListener(_ =>
        {
            target.transform.localScale = new Vector3(1.08f, 1.08f, 1f);
            SetStageNodeHaloAlpha(haloImage, 0.58f);
            ResolveLevelMenuAudioController()?.PlayHoverSFX();
        });
        trigger.triggers.Add(hoverEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };

        exitEntry.callback.AddListener(_ =>
        {
            target.transform.localScale = Vector3.one;
            SetStageNodeHaloAlpha(haloImage, 0f);
        });
        trigger.triggers.Add(exitEntry);
    }

    private static void SetStageNodeHaloAlpha(Image haloImage, float alpha)
    {
        if (haloImage == null)
        {
            return;
        }

        Color color = haloImage.color;
        color.a = alpha;
        haloImage.color = color;
    }

    private static Sprite GetStageNodeHaloSprite()
    {
        if (stageNodeHaloSprite != null)
        {
            return stageNodeHaloSprite;
        }

        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "RuntimeStageNodeHalo",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float ringRadius = size * 0.39f;
        float ringThickness = size * 0.07f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float ringAlpha = Mathf.Clamp01(1f - Mathf.Abs(distance - ringRadius) / ringThickness);
                float softOuter = Mathf.Clamp01(1f - Mathf.InverseLerp(size * 0.43f, size * 0.49f, distance));
                float softInner = Mathf.Clamp01(Mathf.InverseLerp(size * 0.24f, size * 0.32f, distance));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, ringAlpha * softOuter * softInner));
            }
        }

        texture.Apply();
        stageNodeHaloSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        stageNodeHaloSprite.name = "RuntimeStageNodeHalo";
        return stageNodeHaloSprite;
    }

    private void PlayIgniteAndLoadNightStage(string sceneName)
    {
        ResolveLevelMenuAudioController()?.PlayIgniteSFX();

        if (loadNightStageRoutine != null)
        {
            StopCoroutine(loadNightStageRoutine);
        }

        loadNightStageRoutine = StartCoroutine(LoadNightStageAfterSfx(sceneName));
    }

    private IEnumerator LoadNightStageAfterSfx(string sceneName)
    {
        yield return new WaitForSecondsRealtime(0.12f);
        loadNightStageRoutine = null;
        LoadNightStage(sceneName);
    }

    private LevelMenuAudioController ResolveLevelMenuAudioController()
    {
        if (levelMenuAudioController == null)
        {
            levelMenuAudioController = GetComponent<LevelMenuAudioController>();
        }

        if (levelMenuAudioController == null)
        {
            levelMenuAudioController = gameObject.AddComponent<LevelMenuAudioController>();
        }

        levelMenuAudioController.Configure(levelMenuHoverClip, levelMenuIgniteClip, levelMenuHoverVol, levelMenuIgniteVol);
        return levelMenuAudioController;
    }

    private Image CreateDecorPanel(string objectName, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject panelObject = new GameObject(objectName);
        panelObject.transform.SetParent(parent, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = position;
        panelRect.sizeDelta = size;

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = color;
        return panelImage;
    }

    private void CreateCloseButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("HubMapPanelButton");
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -98f);
        buttonRect.sizeDelta = new Vector2(170f, 42f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.86f, 0.82f, 0.72f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(HidePanel);

        closeButtonText = CreateText("Close", buttonObject.transform, Vector2.zero, new Vector2(160f, 36f), 20, TextAnchor.MiddleCenter);
        closeButtonText.color = Color.black;
    }

    private void CreateRepairButton(Transform parent)
    {
        repairButtonObject = new GameObject("RepairButton");
        repairButtonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = repairButtonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -48f);
        buttonRect.sizeDelta = new Vector2(190f, 38f);

        repairButtonImage = repairButtonObject.AddComponent<Image>();
        repairButtonImage.color = new Color(0.35f, 0.35f, 0.35f, 0.9f);

        repairButton = repairButtonObject.AddComponent<Button>();
        repairButton.onClick.AddListener(TryRepairShrine);

        repairButtonText = CreateText("修復する", repairButtonObject.transform, Vector2.zero, new Vector2(180f, 34f), 18, TextAnchor.MiddleCenter);
        repairButtonText.color = Color.black;
        repairButtonObject.SetActive(false);
    }

    private Button CreateButton(string objectName, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = size;

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.86f, 0.82f, 0.72f, 1f);

        Button button = buttonObject.AddComponent<Button>();

        if (action != null)
        {
            button.onClick.AddListener(action);
        }

        return button;
    }

    private void CreateButtonWithLabel(string objectName, string label, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        Button button = CreateButton(objectName, parent, position, size, action);
        if (objectName == "BackButton")
        {
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = new Vector2(-558f, 298f);
            buttonRect.sizeDelta = new Vector2(126f, 42f);
        }

        Text buttonText = CreateText("Label", button.transform, Vector2.zero, new Vector2(size.x - 10f, size.y - 6f), 20, TextAnchor.MiddleCenter);
        buttonText.text = label;
        buttonText.color = Color.black;
    }

    private Text CreateText(string objectName, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = position;
        textRect.sizeDelta = size;

        Text uiText = textObject.AddComponent<Text>();
        uiText.text = objectName;
        uiText.alignment = alignment;
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

    private void ResolveUiRoot()
    {
        if (uiRoot != null)
        {
            return;
        }

        Transform foundRoot = transform.Find("UI");

        if (foundRoot != null)
        {
            uiRoot = foundRoot;
        }
    }

    private void RefreshShrinePanel()
    {
        ResourceInventory inventory = ResolveResourceInventory();
        int faithPoints = inventory != null ? inventory.FaithPoints : 0;
        bool canRepair = !shrineRepaired && faithPoints >= shrineRepairFaithCost;

        if (shrineRepaired)
        {
            bodyText.text = $"{shrineRepairedStatus}\n小さな神社に灯りが戻りました";
            SetRepairButtonState(true, "カフェに入る", new Color(0.86f, 0.82f, 0.72f, 1f), Color.black);
            return;
        }

        bodyText.text = $"{shrineStatus}\n必要な信仰値: {shrineRepairFaithCost}\n現在の信仰値: {faithPoints}";

        if (canRepair)
        {
            SetRepairButtonState(true, "修復する", new Color(0.86f, 0.82f, 0.72f, 1f), Color.black);
        }
        else
        {
            SetRepairButtonState(false, "信仰値不足", new Color(0.35f, 0.35f, 0.35f, 0.9f), new Color(0.82f, 0.82f, 0.82f, 1f));
        }
    }

    private void SetRepairButtonState(bool interactable, string label, Color backgroundColor, Color textColor)
    {
        if (repairButton != null)
        {
            repairButton.interactable = interactable;
        }

        if (repairButtonImage != null)
        {
            repairButtonImage.color = backgroundColor;
        }

        if (repairButtonText != null)
        {
            repairButtonText.text = label;
            repairButtonText.color = textColor;
        }
    }

    private void ApplyShrineVisualState()
    {
        ResolveShrineIconRenderer();

        if (shrineIconRenderer == null)
        {
            return;
        }

        if (shrineRepaired)
        {
            if (repairedShrineSprite != null)
            {
                shrineIconRenderer.sprite = repairedShrineSprite;
                shrineIconRenderer.transform.localScale = repairedShrineScale;
                shrineIconRenderer.color = Color.white;
                return;
            }

            shrineIconRenderer.color = new Color(1f, 0.95f, 0.78f, 1f);
            return;
        }

        if (originalShrineSprite != null)
        {
            shrineIconRenderer.sprite = originalShrineSprite;
            shrineIconRenderer.transform.localScale = originalShrineScale;
        }

        shrineIconRenderer.color = Color.white;
    }

    private void ResolveShrineIconRenderer()
    {
        if (shrineIconRenderer == null)
        {
            Transform shrineTransform = transform.Find("Buildings/RuinedShrineIcon");
            shrineIconRenderer = shrineTransform != null ? shrineTransform.GetComponent<SpriteRenderer>() : null;
        }

        if (shrineIconRenderer != null && !shrineVisualCached)
        {
            originalShrineSprite = shrineIconRenderer.sprite;
            originalShrineScale = shrineIconRenderer.transform.localScale;
            shrineVisualCached = true;
        }
    }

    private void SetHubPlayerControl(bool enabled)
    {
        HubPlayerController player = ResolveHubPlayer();

        if (player != null)
        {
            player.SetControlEnabled(enabled);
        }
    }

    private HubPlayerController ResolveHubPlayer()
    {
        if (hubPlayer == null)
        {
            hubPlayer = FindAnyObjectByType<HubPlayerController>();
        }

        return hubPlayer;
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
