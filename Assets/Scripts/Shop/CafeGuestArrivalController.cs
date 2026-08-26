using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

public class CafeGuestArrivalController : MonoBehaviour
{
    [SerializeField] private Vector2 entrancePosition = new Vector2(0f, -3.35f);
    [SerializeField] private Vector2 counterApproachPosition = new Vector2(0f, 0.2f);
    [SerializeField] private float firstGuestDelay = 0.6f;
    [SerializeField] private float guestArrivalInterval = 0.9f;
    [SerializeField] private float moveSpeed = 2.6f;
    [SerializeField] private float walkFrameInterval = 0.18f;
    [SerializeField] private Vector3 guestScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector2 seatedOffset = new Vector2(0f, 0.08f);
    [SerializeField] private int guestSortingOrder = 3;
    [SerializeField] private float departureMessageSeconds = 1.8f;
    [SerializeField] private float refillGuestDelay = 1.2f;
    [SerializeField] private bool useIdleFrameInWalkCycle = true;
    [SerializeField] private bool enableWalkPoseScale;
    [SerializeField] private bool normalizeWalkFrameSize = true;
    [SerializeField] private bool normalizeWalkDirectionsToCommonHeight = true;
    [SerializeField] private bool normalizeWalkDirectionsToCommonWidth;
    [SerializeField] private float maxWalkFrameScaleAdjustment = 0.32f;
    [SerializeField] private bool normalizeWalkFrameWidth;
    [SerializeField] private float maxWalkFrameWidthAdjustment = 0.12f;
    [SerializeField] private float walkStepSquash = 0.035f;
    [SerializeField] private float walkStepStretch = 0.02f;
    [SerializeField] private Color normalGuestColor = Color.white;
    [SerializeField] private Color selectedGuestColor = new Color(1f, 0.88f, 0.52f, 1f);
    [SerializeField] private Sprite requestBubbleSprite;
    [SerializeField] private bool preferProjectRequestBubbleSprite = true;
    [SerializeField] private Vector2 requestBubbleOffset = new Vector2(0f, 1.12f);
    [SerializeField] private Vector3 requestBubbleScale = new Vector3(0.14f, 0.14f, 1f);
    [SerializeField] private Vector3 requestMenuIconScale = new Vector3(0.065f, 0.065f, 1f);
    [SerializeField] private Vector2 requestBubbleClickSize = new Vector2(1.35f, 0.75f);
    [SerializeField] private float requestTextCharacterSize = 0.06f;
    [SerializeField] private int requestBubbleSortingOffset = 12;
    [SerializeField] private Color requestBubbleReadyColor = Color.white;
    [SerializeField] private Color requestBubbleMissingItemColor = new Color(1f, 0.82f, 0.72f, 1f);
    [SerializeField] private Vector3 requestBubbleHoverScale = new Vector3(1.08f, 1.08f, 1f);
    [SerializeField] private GuestVisualSet[] guestVisuals = new GuestVisualSet[4];
    [SerializeField] private VisitorVisualMapping[] visitorVisualMappings = new VisitorVisualMapping[0];
    [SerializeField] private Sprite fallbackGuestSprite;
    [SerializeField] private bool logVisitorVisualResolution;

    private CafeOperationController operationController;
    private GuestRuntimeVisual[] activeGuestVisuals;
    private Coroutine[] departureCoroutines;
    private bool arrivalSequenceStarted;
    private bool searchedRequestBubbleSprite;
    private readonly HashSet<string> loggedMissingSprites = new HashSet<string>();
    private readonly Dictionary<string, Sprite> requestMenuIconCache = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, VisitorSpecialVisualState> specialVisualStates = new Dictionary<string, VisitorSpecialVisualState>();
#if UNITY_EDITOR
    private readonly Dictionary<Sprite, Vector2> spriteVisibleSizeCache = new Dictionary<Sprite, Vector2>();
#endif

    private void Awake()
    {
        operationController = GetComponent<CafeOperationController>();
    }

    private void Start()
    {
        if (operationController == null)
        {
            return;
        }

        operationController.BusinessOpened += BeginGuestArrivals;
        operationController.GuestServed += BeginGuestDeparture;
        operationController.SelectedGuestChanged += HighlightGuestSeat;
        operationController.StateChanged += RefreshAllRequestBubbleDeliveryStates;

        if (operationController.IsOpenForBusiness)
        {
            BeginGuestArrivals();
        }
    }

    private void OnDestroy()
    {
        if (operationController != null)
        {
            operationController.BusinessOpened -= BeginGuestArrivals;
            operationController.GuestServed -= BeginGuestDeparture;
            operationController.SelectedGuestChanged -= HighlightGuestSeat;
            operationController.StateChanged -= RefreshAllRequestBubbleDeliveryStates;
        }
    }

    private void BeginGuestArrivals()
    {
        if (arrivalSequenceStarted)
        {
            return;
        }

        arrivalSequenceStarted = true;
        activeGuestVisuals = new GuestRuntimeVisual[operationController.Guests.Count];
        departureCoroutines = new Coroutine[operationController.Guests.Count];
        StartCoroutine(CreateGuestsInSequence());
    }

    private IEnumerator CreateGuestsInSequence()
    {
        yield return new WaitForSeconds(firstGuestDelay);

        for (int i = 0; i < operationController.Guests.Count; i++)
        {
            StartCoroutine(CreateAndSeatGuest(i));
            yield return new WaitForSeconds(guestArrivalInterval);
        }
    }

    private IEnumerator CreateAndSeatGuest(int guestIndex)
    {
        CafeGuestState guestState = operationController.Guests[guestIndex];
        GuestVisualSet visualSet = ResolveVisualSet(guestState, guestIndex);
        GameObject seatObject = GameObject.Find($"GuestSeat_{guestIndex + 1:00}");
        Sprite initialIdleSprite = GetIdleSprite(GuestFacingDirection.Back, visualSet);

        if (visualSet == null || initialIdleSprite == null || seatObject == null)
        {
            if (visualSet == null)
            {
                LogMissingSpriteOnce($"Cafe visitor visual set is missing for {guestState.VisualId}.");
            }
            else if (initialIdleSprite == null)
            {
                LogMissingSpriteOnce($"Cafe visitor has no usable idle sprite for {guestState.VisualId}.");
            }

            yield break;
        }

        WarnForMissingDirectionalSprites(guestState.VisualId, visualSet);

        GameObject guestObject = new GameObject($"CafeGuestVisual_{guestIndex + 1:00}_{guestState.VisitorId}");
        guestObject.transform.SetParent(transform, false);
        guestObject.transform.position = entrancePosition;
        guestObject.transform.localScale = Vector3.one;

        GameObject spriteVisualObject = new GameObject("GuestSpriteVisual");
        spriteVisualObject.transform.SetParent(guestObject.transform, false);
        spriteVisualObject.transform.localScale = Vector3.Scale(guestScale, visualSet.scaleMultiplier);

        SpriteRenderer spriteRenderer = spriteVisualObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = initialIdleSprite;
        spriteRenderer.sortingOrder = guestSortingOrder;

        GameObject requestBubbleObject = CreateRequestBubble(guestObject.transform, guestState, guestIndex);
        GuestRuntimeVisual runtimeVisual = new GuestRuntimeVisual(guestObject, spriteRenderer, visualSet, requestBubbleObject);
        activeGuestVisuals[guestIndex] = runtimeVisual;
        HighlightGuestSeat(operationController.SelectedGuestIndex);

        yield return MoveGuest(guestObject.transform, spriteRenderer, visualSet, counterApproachPosition);
        Vector2 seatedPosition = (Vector2)seatObject.transform.position + seatedOffset + visualSet.seatedOffset;
        yield return MoveGuest(guestObject.transform, spriteRenderer, visualSet, seatedPosition);

        spriteRenderer.sprite = GetIdleSprite(GuestFacingDirection.Back, visualSet);
        guestObject.transform.position = seatedPosition;
        SetRequestBubbleVisible(runtimeVisual, true);
        HighlightGuestSeat(operationController.SelectedGuestIndex);
        operationController.SetCafeFeedbackMessage("来訪者がやって来ました。");
    }

    public void SetVisitorSpecialState(string visitorId, VisitorSpecialVisualState state)
    {
        if (string.IsNullOrEmpty(visitorId))
        {
            return;
        }

        specialVisualStates[visitorId] = state;
        Debug.Log($"Cafe visitor special state changed: {visitorId} -> {state}");
        RefreshActiveVisitorVisual(visitorId);
    }

    private void RefreshActiveVisitorVisual(string visitorId)
    {
        if (activeGuestVisuals == null || operationController == null)
        {
            return;
        }

        IReadOnlyList<CafeGuestState> guests = operationController.Guests;

        for (int i = 0; i < activeGuestVisuals.Length && i < guests.Count; i++)
        {
            CafeGuestState guestState = guests[i];

            if (guestState == null || guestState.VisitorId != visitorId)
            {
                continue;
            }

            GuestRuntimeVisual runtimeVisual = activeGuestVisuals[i];

            if (runtimeVisual == null || runtimeVisual.SpriteRenderer == null)
            {
                continue;
            }

            GuestVisualSet refreshedVisualSet = ResolveVisualSet(guestState, i);
            Sprite idleSprite = GetIdleSprite(GuestFacingDirection.Back, refreshedVisualSet);

            if (idleSprite != null)
            {
                runtimeVisual.SpriteRenderer.sprite = idleSprite;
            }
        }
    }

    private void BeginGuestDeparture(int guestIndex)
    {
        if (departureCoroutines == null || guestIndex < 0 || guestIndex >= departureCoroutines.Length)
        {
            return;
        }

        if (departureCoroutines[guestIndex] != null)
        {
            return;
        }

        departureCoroutines[guestIndex] = StartCoroutine(DepartGuestAfterMessage(guestIndex));
    }

    private IEnumerator DepartGuestAfterMessage(int guestIndex)
    {
        yield return new WaitForSeconds(departureMessageSeconds);

        operationController.MarkGuestLeaving(guestIndex);
        operationController.SetCafeFeedbackMessage("来訪者は静かに席を立ちました。");

        GuestRuntimeVisual runtimeVisual = activeGuestVisuals != null
            && guestIndex >= 0
            && guestIndex < activeGuestVisuals.Length
            ? activeGuestVisuals[guestIndex]
            : null;

        SetRequestBubbleVisible(runtimeVisual, false);

        if (runtimeVisual != null && runtimeVisual.GuestObject != null)
        {
            yield return MoveGuest(runtimeVisual.Transform, runtimeVisual.SpriteRenderer, runtimeVisual.VisualSet, counterApproachPosition);
            yield return MoveGuest(runtimeVisual.Transform, runtimeVisual.SpriteRenderer, runtimeVisual.VisualSet, entrancePosition);
            Destroy(runtimeVisual.GuestObject);
            activeGuestVisuals[guestIndex] = null;
        }

        operationController.ClearGuestSeat(guestIndex);

        if (operationController.TryRefillGuestSeat(guestIndex))
        {
            yield return new WaitForSeconds(refillGuestDelay);
            yield return CreateAndSeatGuest(guestIndex);
        }

        departureCoroutines[guestIndex] = null;
    }

    private void HighlightGuestSeat(int selectedGuestIndex)
    {
        if (activeGuestVisuals == null)
        {
            return;
        }

        for (int i = 0; i < activeGuestVisuals.Length; i++)
        {
            GuestRuntimeVisual runtimeVisual = activeGuestVisuals[i];

            if (runtimeVisual == null || runtimeVisual.SpriteRenderer == null)
            {
                continue;
            }

            runtimeVisual.SpriteRenderer.color = i == selectedGuestIndex
                ? selectedGuestColor
                : normalGuestColor;
        }
    }

    public void TryServeGuestRequest(int guestIndex)
    {
        if (operationController == null)
        {
            return;
        }

        IReadOnlyList<CafeGuestState> guests = operationController.Guests;

        if (guestIndex < 0 || guestIndex >= guests.Count)
        {
            return;
        }

        CafeGuestState guestState = guests[guestIndex];

        if (guestState == null || !guestState.CanServe)
        {
            operationController.SetCafeFeedbackMessage("\u3053\u306E\u6765\u8A2A\u8005\u306F\u4ECA\u306F\u6CE8\u6587\u5F85\u3061\u3067\u306F\u3042\u308A\u307E\u305B\u3093\u3002");
            return;
        }

        if (!TryFindRequestedMenuIndex(guestState.RequestedMenuId, out int menuIndex))
        {
            operationController.SetCafeFeedbackMessage("\u6CE8\u6587\u3092\u78BA\u8A8D\u3067\u304D\u307E\u305B\u3093\u3067\u3057\u305F\u3002");
            return;
        }

        operationController.SetSelectedGuestIndex(guestIndex);

        CafeMenuItem requestedMenuItem = operationController.MenuItems[menuIndex];

        if (operationController.GetFinishedItemCountForMenu(requestedMenuItem) <= 0)
        {
            operationController.SetCafeFeedbackMessage("\u5B8C\u6210\u54C1\u304C\u307E\u3060\u3042\u308A\u307E\u305B\u3093\u3002\u5148\u306B\u5236\u4F5C\u3057\u3066\u304F\u3060\u3055\u3044\u3002");
            RefreshRequestBubbleDeliveryState(guestIndex);
            return;
        }

        bool served = operationController.TryServe(guestIndex, menuIndex, out string resultMessage);
        operationController.SetCafeFeedbackMessage(resultMessage);

        if (served && activeGuestVisuals != null && guestIndex < activeGuestVisuals.Length)
        {
            SetRequestBubbleVisible(activeGuestVisuals[guestIndex], false);
        }
    }

    private void RefreshRequestBubbleDeliveryState(int guestIndex)
    {
        if (activeGuestVisuals == null || guestIndex < 0 || guestIndex >= activeGuestVisuals.Length)
        {
            return;
        }

        GuestRuntimeVisual runtimeVisual = activeGuestVisuals[guestIndex];

        if (runtimeVisual == null || runtimeVisual.RequestBubbleObject == null || operationController == null)
        {
            return;
        }

        IReadOnlyList<CafeGuestState> guests = operationController.Guests;

        if (guestIndex >= guests.Count || guests[guestIndex] == null)
        {
            return;
        }

        bool hasFinishedItem = false;

        if (TryFindRequestedMenuIndex(guests[guestIndex].RequestedMenuId, out int menuIndex))
        {
            hasFinishedItem = operationController.GetFinishedItemCountForMenu(operationController.MenuItems[menuIndex]) > 0;
        }

        SpriteRenderer bubbleRenderer = runtimeVisual.RequestBubbleObject.GetComponentInChildren<SpriteRenderer>();

        if (bubbleRenderer != null)
        {
            bubbleRenderer.color = hasFinishedItem ? requestBubbleReadyColor : requestBubbleMissingItemColor;
        }
    }

    private void RefreshAllRequestBubbleDeliveryStates()
    {
        if (activeGuestVisuals == null)
        {
            return;
        }

        for (int i = 0; i < activeGuestVisuals.Length; i++)
        {
            RefreshRequestBubbleDeliveryState(i);
        }
    }

    private bool TryFindRequestedMenuIndex(string requestedMenuId, out int menuIndex)
    {
        menuIndex = -1;

        if (string.IsNullOrEmpty(requestedMenuId) || operationController == null)
        {
            return false;
        }

        IReadOnlyList<CafeMenuItem> menuItems = operationController.MenuItems;

        for (int i = 0; i < menuItems.Count; i++)
        {
            CafeMenuItem menuItem = menuItems[i];

            if (menuItem != null && menuItem.MenuId == requestedMenuId)
            {
                menuIndex = i;
                return true;
            }
        }

        return false;
    }

    private GameObject CreateRequestBubble(Transform parent, CafeGuestState guestState, int guestIndex)
    {
        GameObject bubbleObject = new GameObject("RequestBubble");
        bubbleObject.transform.SetParent(parent, false);
        bubbleObject.transform.localPosition = requestBubbleOffset;
        bubbleObject.transform.localScale = GetInverseScale(parent.localScale);

        BoxCollider2D clickCollider = bubbleObject.AddComponent<BoxCollider2D>();
        clickCollider.isTrigger = true;
        clickCollider.size = requestBubbleClickSize;

        CafeRequestBubbleClickTarget clickTarget = bubbleObject.AddComponent<CafeRequestBubbleClickTarget>();
        clickTarget.Configure(this, guestIndex);

        Sprite bubbleSprite = ResolveRequestBubbleSprite();

        if (bubbleSprite != null)
        {
            GameObject backgroundObject = new GameObject("RequestBubbleBackground");
            backgroundObject.transform.SetParent(bubbleObject.transform, false);
            backgroundObject.transform.localScale = requestBubbleScale;

            SpriteRenderer backgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = bubbleSprite;
            backgroundRenderer.sortingOrder = guestSortingOrder + requestBubbleSortingOffset;
            backgroundRenderer.color = Color.white;
        }

        Sprite requestIcon = ResolveRequestMenuIcon(guestState);

        if (requestIcon != null)
        {
            GameObject iconObject = new GameObject("RequestMenuIcon");
            iconObject.transform.SetParent(bubbleObject.transform, false);
            iconObject.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            iconObject.transform.localScale = requestMenuIconScale;

            SpriteRenderer iconRenderer = iconObject.AddComponent<SpriteRenderer>();
            iconRenderer.sprite = requestIcon;
            iconRenderer.sortingOrder = guestSortingOrder + requestBubbleSortingOffset + 1;
            iconRenderer.color = Color.white;
        }
        else
        {
            GameObject textObject = new GameObject("RequestText");
            textObject.transform.SetParent(bubbleObject.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = GetRequestBubbleText(guestState);
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 38;
            textMesh.characterSize = requestTextCharacterSize;
            textMesh.color = new Color(0.16f, 0.09f, 0.06f, 1f);

            MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();

            if (textRenderer != null)
            {
                textRenderer.sortingOrder = guestSortingOrder + requestBubbleSortingOffset + 1;
            }
        }

        bubbleObject.SetActive(false);
        return bubbleObject;
    }

    private string GetRequestBubbleText(CafeGuestState guestState)
    {
        if (guestState == null || string.IsNullOrEmpty(guestState.RequestedMenuDisplayName))
        {
            return "注文";
        }

        return guestState.RequestedMenuDisplayName;
    }

    private Sprite ResolveRequestMenuIcon(CafeGuestState guestState)
    {
        if (guestState == null || string.IsNullOrEmpty(guestState.RequestedMenuId))
        {
            return null;
        }

        if (operationController != null)
        {
            Sprite iconFromController = operationController.GetMenuIcon(guestState.RequestedMenuId);

            if (iconFromController != null)
            {
                return iconFromController;
            }
        }

        if (requestMenuIconCache.TryGetValue(guestState.RequestedMenuId, out Sprite cachedIcon))
        {
            return cachedIcon;
        }

        Sprite loadedIcon = null;

#if UNITY_EDITOR
        string assetPath = GetRequestMenuIconPath(guestState.RequestedMenuId);
        loadedIcon = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        if (loadedIcon == null)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            if (texture != null)
            {
                loadedIcon = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                loadedIcon.name = $"{guestState.RequestedMenuId}_RequestIcon";
            }
        }
#endif

        requestMenuIconCache[guestState.RequestedMenuId] = loadedIcon;

        if (loadedIcon == null)
        {
            LogMissingSpriteOnce($"Cafe request menu icon is missing for {guestState.RequestedMenuId}.");
        }

        return loadedIcon;
    }

    private string GetRequestMenuIconPath(string menuId)
    {
        switch (menuId)
        {
            case "inari_coffee":
                return "Assets/Art/cafe_icon/menu_runtime/inari_coffee.png";
            case "kitsunebi_latte":
                return "Assets/Art/cafe_icon/menu_runtime/kitsunebi_latte.png";
            case "yozakura_cake":
                return "Assets/Art/cafe_icon/menu_runtime/yozakura_cake.png";
            default:
                return string.Empty;
        }
    }

    private Vector3 GetInverseScale(Vector3 scale)
    {
        return new Vector3(
            Mathf.Approximately(scale.x, 0f) ? 1f : 1f / scale.x,
            Mathf.Approximately(scale.y, 0f) ? 1f : 1f / scale.y,
            1f);
    }

    private void SetRequestBubbleVisible(GuestRuntimeVisual runtimeVisual, bool isVisible)
    {
        if (runtimeVisual == null || runtimeVisual.RequestBubbleObject == null)
        {
            return;
        }

        runtimeVisual.RequestBubbleObject.SetActive(isVisible);

        if (isVisible && activeGuestVisuals != null)
        {
            for (int i = 0; i < activeGuestVisuals.Length; i++)
            {
                if (activeGuestVisuals[i] == runtimeVisual)
                {
                    RefreshRequestBubbleDeliveryState(i);
                    break;
                }
            }
        }
    }

    private Sprite ResolveRequestBubbleSprite()
    {
        if (searchedRequestBubbleSprite)
        {
            return requestBubbleSprite;
        }

        searchedRequestBubbleSprite = true;

#if UNITY_EDITOR
        const string assetPath = "Assets/Art/cafe_icon/speak_bubble_request.png";
        Sprite projectBubbleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        if (projectBubbleSprite == null)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            if (texture != null)
            {
                projectBubbleSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                projectBubbleSprite.name = "speak_bubble_request_RuntimeSprite";
            }
        }

        if (projectBubbleSprite != null && (preferProjectRequestBubbleSprite || requestBubbleSprite == null))
        {
            requestBubbleSprite = projectBubbleSprite;
        }
#endif

        if (requestBubbleSprite == null)
        {
            LogMissingSpriteOnce("Cafe request bubble sprite is missing. Add Assets/Art/cafe_icon/speak_bubble_request.png or assign requestBubbleSprite.");
        }

        return requestBubbleSprite;
    }

    private GuestVisualSet ResolveVisualSet(CafeGuestState guestState, int guestIndex)
    {
        if (guestState == null)
        {
            return CreateFallbackVisualSet("unknown", null, guestIndex);
        }

        string visitorId = guestState.VisitorId;
        string visualId = string.IsNullOrEmpty(guestState.VisualId) ? visitorId : guestState.VisualId;
        VisitorVisualMapping mapping = FindVisitorVisualMapping(visitorId) ?? FindVisitorVisualMapping(visualId);
        GuestVisualSet visualSet = CreateVisualSetFromMapping(mapping, visualId);

        if (!HasAnyUsableSprite(visualSet))
        {
            visualSet = CreateProjectVisualSet(visualId, mapping);
        }

        if (!HasAnyUsableSprite(visualSet))
        {
            visualSet = CreateSerializedVisualSet(visualId);
        }

        if (!HasAnyUsableSprite(visualSet))
        {
            visualSet = CreateFallbackVisualSet(visualId, mapping, guestIndex);
        }

        ApplySpecialVisualState(visitorId, visualSet, mapping);
        CacheCommonReferenceSpriteSize(visualSet);

        if (logVisitorVisualResolution && visualSet != null)
        {
            Debug.Log($"Cafe visitor visual resolved: visitorId={visitorId}, visualId={visualId}, source={visualSet.sourceLabel}");
        }

        return visualSet;
    }

    private GuestVisualSet CreateSerializedVisualSet(string visualId)
    {
        if (guestVisuals == null || string.IsNullOrEmpty(visualId))
        {
            return null;
        }

        string assetId = GetGuestAssetId(visualId);

        for (int i = 0; i < guestVisuals.Length; i++)
        {
            GuestVisualSet serializedSet = guestVisuals[i];

            if (serializedSet == null
                || (serializedSet.guestId != visualId && serializedSet.guestId != assetId))
            {
                continue;
            }

            GuestVisualSet visualSet = CloneVisualSet(serializedSet);
            visualSet.sourceLabel = "serialized scene visuals";
            return visualSet;
        }

        return null;
    }

    private GuestVisualSet CloneVisualSet(GuestVisualSet source)
    {
        return new GuestVisualSet
        {
            guestId = source.guestId,
            backIdleSprite = source.backIdleSprite,
            backWalkSprite01 = source.backWalkSprite01,
            backWalkSprite02 = source.backWalkSprite02,
            frontIdleSprite = source.frontIdleSprite,
            frontWalkSprite01 = source.frontWalkSprite01,
            frontWalkSprite02 = source.frontWalkSprite02,
            leftIdleSprite = source.leftIdleSprite,
            leftWalkSprite01 = source.leftWalkSprite01,
            leftWalkSprite02 = source.leftWalkSprite02,
            rightIdleSprite = source.rightIdleSprite,
            rightWalkSprite01 = source.rightWalkSprite01,
            rightWalkSprite02 = source.rightWalkSprite02,
            scaleMultiplier = source.scaleMultiplier,
            seatedOffset = source.seatedOffset,
            normalizeWalkFrameWidthOverride = source.normalizeWalkFrameWidthOverride,
            normalizeWalkDirectionsToCommonWidthOverride = source.normalizeWalkDirectionsToCommonWidthOverride,
            disableWalkPoseSquash = source.disableWalkPoseSquash
        };
    }

    private VisitorVisualMapping FindVisitorVisualMapping(string visitorId)
    {
        if (string.IsNullOrEmpty(visitorId) || visitorVisualMappings == null)
        {
            return null;
        }

        for (int i = 0; i < visitorVisualMappings.Length; i++)
        {
            VisitorVisualMapping mapping = visitorVisualMappings[i];

            if (mapping != null && mapping.visitorId == visitorId)
            {
                return mapping;
            }
        }

        return null;
    }

    private GuestVisualSet CreateVisualSetFromMapping(VisitorVisualMapping mapping, string visualId)
    {
        if (mapping == null)
        {
            return null;
        }

        Sprite seatedOrIdle = mapping.seatedSprite != null ? mapping.seatedSprite : mapping.idleSprite;
        Sprite idleOrFallback = seatedOrIdle != null ? seatedOrIdle : mapping.fallbackSprite;

        if (idleOrFallback == null)
        {
            return null;
        }

        return new GuestVisualSet
        {
            guestId = string.IsNullOrEmpty(mapping.visitorId) ? visualId : mapping.visitorId,
            backIdleSprite = idleOrFallback,
            frontIdleSprite = idleOrFallback,
            leftIdleSprite = idleOrFallback,
            rightIdleSprite = idleOrFallback,
            scaleMultiplier = mapping.scaleMultiplier,
            seatedOffset = mapping.seatedOffset,
            sourceLabel = "inspector mapping"
        };
    }

    private GuestVisualSet CreateProjectVisualSet(string visualId, VisitorVisualMapping mapping)
    {
        GuestVisualSet visualSet = new GuestVisualSet
        {
            guestId = visualId,
            scaleMultiplier = mapping != null ? mapping.scaleMultiplier : Vector3.one,
            seatedOffset = mapping != null ? mapping.seatedOffset : Vector2.zero,
            sourceLabel = "project sprites"
        };

        CompleteVisualSetFromProjectAssets(visualSet, visualId);
        ApplyDefaultVisualTuning(visualSet, visualId, mapping);

        Sprite baseSprite = LoadGuestBaseSprite(GetGuestAssetId(visualId));

        if (baseSprite != null)
        {
            visualSet.frontIdleSprite = visualSet.frontIdleSprite != null ? visualSet.frontIdleSprite : baseSprite;
            visualSet.backIdleSprite = visualSet.backIdleSprite != null ? visualSet.backIdleSprite : baseSprite;
            visualSet.leftIdleSprite = visualSet.leftIdleSprite != null ? visualSet.leftIdleSprite : baseSprite;
            visualSet.rightIdleSprite = visualSet.rightIdleSprite != null ? visualSet.rightIdleSprite : baseSprite;
        }

        return visualSet;
    }

    private void ApplyDefaultVisualTuning(GuestVisualSet visualSet, string visualId, VisitorVisualMapping mapping)
    {
        if (visualSet == null || mapping != null || string.IsNullOrEmpty(visualId))
        {
            return;
        }

        string assetId = GetGuestAssetId(visualId);

        switch (assetId)
        {
            case "tanuki_yokai":
                visualSet.scaleMultiplier = new Vector3(0.9f, 0.9f, 1f);
                visualSet.sourceLabel += " + default tanuki scale";
                break;
            case "nekomata":
                visualSet.scaleMultiplier = new Vector3(0.92f, 0.92f, 1f);
                visualSet.sourceLabel += " + default nekomata scale";
                break;
            case "gramma":
            case "traveler":
                visualSet.disableWalkPoseSquash = true;
                visualSet.sourceLabel += $" + stable {assetId} walk height";
                break;
        }
    }

    private GuestVisualSet CreateFallbackVisualSet(string visualId, VisitorVisualMapping mapping, int guestIndex)
    {
        Sprite fallbackSpriteToUse = mapping != null && mapping.fallbackSprite != null
            ? mapping.fallbackSprite
            : fallbackGuestSprite;

        if (fallbackSpriteToUse == null)
        {
            fallbackSpriteToUse = FindFallbackSpriteFromLegacyVisuals(guestIndex);
        }

        if (fallbackSpriteToUse == null)
        {
            LogMissingSpriteOnce($"Cafe visitor fallback sprite is missing for {visualId}.");
            return null;
        }

        LogMissingSpriteOnce($"Cafe visitor visual fallback used for {visualId}.");

        return new GuestVisualSet
        {
            guestId = visualId,
            backIdleSprite = fallbackSpriteToUse,
            frontIdleSprite = fallbackSpriteToUse,
            leftIdleSprite = fallbackSpriteToUse,
            rightIdleSprite = fallbackSpriteToUse,
            scaleMultiplier = mapping != null ? mapping.scaleMultiplier : Vector3.one,
            seatedOffset = mapping != null ? mapping.seatedOffset : Vector2.zero,
            sourceLabel = "fallback"
        };
    }

    private Sprite FindFallbackSpriteFromLegacyVisuals(int guestIndex)
    {
        if (guestVisuals == null || guestVisuals.Length == 0)
        {
            return null;
        }

        if (guestIndex >= 0 && guestIndex < guestVisuals.Length)
        {
            Sprite indexedSprite = GetIdleSprite(GuestFacingDirection.Back, guestVisuals[guestIndex]);

            if (indexedSprite != null)
            {
                return indexedSprite;
            }
        }

        for (int i = 0; i < guestVisuals.Length; i++)
        {
            Sprite sprite = GetIdleSprite(GuestFacingDirection.Back, guestVisuals[i]);

            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private bool HasAnyUsableSprite(GuestVisualSet visualSet)
    {
        return visualSet != null
            && (visualSet.frontIdleSprite != null
                || visualSet.backIdleSprite != null
                || visualSet.leftIdleSprite != null
                || visualSet.rightIdleSprite != null);
    }

    private void ApplySpecialVisualState(string visitorId, GuestVisualSet visualSet, VisitorVisualMapping mapping)
    {
        if (visualSet == null || mapping == null || string.IsNullOrEmpty(visitorId))
        {
            return;
        }

        if (!specialVisualStates.TryGetValue(visitorId, out VisitorSpecialVisualState state))
        {
            state = VisitorSpecialVisualState.Seated;
        }

        Sprite specialSprite = null;

        switch (state)
        {
            case VisitorSpecialVisualState.Sleepy:
                specialSprite = mapping.sleepySprite;
                break;
            case VisitorSpecialVisualState.Sleeping:
                specialSprite = mapping.sleepingSprite;
                break;
            case VisitorSpecialVisualState.Seated:
                specialSprite = mapping.seatedSprite;
                break;
        }

        if (specialSprite == null)
        {
            return;
        }

        visualSet.backIdleSprite = specialSprite;
        visualSet.frontIdleSprite = specialSprite;
        visualSet.leftIdleSprite = specialSprite;
        visualSet.rightIdleSprite = specialSprite;
        visualSet.sourceLabel += $" + {state}";
    }

    private void CompleteVisualSetFromProjectAssets(GuestVisualSet visualSet, string guestId)
    {
        if (visualSet == null || string.IsNullOrEmpty(guestId))
        {
            return;
        }

        string assetId = GetGuestAssetId(guestId);
        visualSet.frontIdleSprite = LoadGuestSprite(assetId, "front", "idle") ?? visualSet.frontIdleSprite;
        visualSet.frontWalkSprite01 = LoadGuestSprite(assetId, "front", "walk_01") ?? visualSet.frontWalkSprite01;
        visualSet.frontWalkSprite02 = LoadGuestSprite(assetId, "front", "walk_02") ?? visualSet.frontWalkSprite02;
        visualSet.backIdleSprite = LoadGuestSprite(assetId, "back", "idle") ?? visualSet.backIdleSprite;
        visualSet.backWalkSprite01 = LoadGuestSprite(assetId, "back", "walk_01") ?? visualSet.backWalkSprite01;
        visualSet.backWalkSprite02 = LoadGuestSprite(assetId, "back", "walk_02") ?? visualSet.backWalkSprite02;
        visualSet.leftIdleSprite = LoadGuestSprite(assetId, "left", "idle") ?? visualSet.leftIdleSprite;
        visualSet.leftWalkSprite01 = LoadGuestSprite(assetId, "left", "walk_01") ?? visualSet.leftWalkSprite01;
        visualSet.leftWalkSprite02 = LoadGuestSprite(assetId, "left", "walk_02") ?? visualSet.leftWalkSprite02;
        visualSet.rightIdleSprite = LoadGuestSprite(assetId, "right", "idle") ?? visualSet.rightIdleSprite;
        visualSet.rightWalkSprite01 = LoadGuestSprite(assetId, "right", "walk_01") ?? visualSet.rightWalkSprite01;
        visualSet.rightWalkSprite02 = LoadGuestSprite(assetId, "right", "walk_02") ?? visualSet.rightWalkSprite02;
    }

    private string GetGuestAssetId(string guestId)
    {
        switch (guestId)
        {
            case "worshipper":
                return "gramma";
            case "small_yokai":
                return "nekomata";
            case "fiona_student":
                return "fiona";
            case "shikei_visitor":
            case "black_priest":
            case "priest_regular":
                return "priest";
            default:
                return guestId;
        }
    }

    private Sprite LoadGuestSprite(string assetId, string direction, string state)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(assetId))
        {
            return null;
        }

        string runtimeAssetPath = $"Assets/Art/cafe_icon/guest_runtime/{assetId}_{direction}_{state}.png";
        Sprite sprite = LoadSpriteAtPath(runtimeAssetPath);

        if (sprite != null)
        {
            return sprite;
        }

        string assetPath = $"Assets/Art/cafe_icon/guest_{assetId}/guest_{assetId}_{direction}_{state}.png";
        return LoadSpriteAtPath(assetPath);
#else
        return null;
#endif
    }

    private Sprite LoadGuestBaseSprite(string assetId)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(assetId))
        {
            return null;
        }

        Sprite sprite = LoadSpriteAtPath($"Assets/Art/cafe_icon/guest_{assetId}/guest_{assetId}.png");

        if (sprite != null)
        {
            return sprite;
        }

        return LoadSpriteAtPath($"Assets/Art/cafe_icon/{assetId}.png");
#else
        return null;
#endif
    }

#if UNITY_EDITOR
    private Sprite LoadSpriteAtPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

        if (texture == null)
        {
            return null;
        }

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite loadedSprite)
            {
                return loadedSprite;
            }
        }

        return null;
    }
#endif

    private IEnumerator MoveGuest(
        Transform guestTransform,
        SpriteRenderer spriteRenderer,
        GuestVisualSet visualSet,
        Vector2 destination)
    {
        float nextFrameTime = Time.time + walkFrameInterval;
        int walkFrameIndex = 0;
        Transform spriteVisualTransform = spriteRenderer.transform;
        Vector3 baseScale = spriteVisualTransform.localScale;
        GuestFacingDirection facingDirection = GetFacingDirection(guestTransform.position, destination);
        spriteRenderer.sprite = GetDirectionalWalkSprite(facingDirection, visualSet, walkFrameIndex);
        ApplyWalkPoseScale(spriteVisualTransform, baseScale, spriteRenderer.sprite, visualSet, facingDirection, walkFrameIndex);

        while (Vector2.Distance(guestTransform.position, destination) > 0.02f)
        {
            guestTransform.position = Vector2.MoveTowards(
                guestTransform.position,
                destination,
                moveSpeed * Time.deltaTime);

            if (Time.time >= nextFrameTime)
            {
                walkFrameIndex = GetNextWalkFrameIndex(walkFrameIndex);
                facingDirection = GetFacingDirection(guestTransform.position, destination);
                spriteRenderer.sprite = GetDirectionalWalkSprite(facingDirection, visualSet, walkFrameIndex);
                ApplyWalkPoseScale(spriteVisualTransform, baseScale, spriteRenderer.sprite, visualSet, facingDirection, walkFrameIndex);
                nextFrameTime = Time.time + walkFrameInterval;
            }

            yield return null;
        }

        spriteVisualTransform.localScale = baseScale;
    }

    private void ApplyWalkPoseScale(
        Transform guestTransform,
        Vector3 baseScale,
        Sprite currentSprite,
        GuestVisualSet visualSet,
        GuestFacingDirection direction,
        int walkFrameIndex)
    {
        Vector3 targetScale = baseScale;

        if (normalizeWalkFrameSize && currentSprite != null && visualSet != null)
        {
            Vector2 referenceSize = GetReferenceSpriteSize(visualSet, direction);
            float referenceHeight = referenceSize.y;
            float currentHeight = GetSpriteDisplaySize(currentSprite).y;

            if (referenceHeight > 0f && currentHeight > 0f)
            {
                float frameScale = referenceHeight / currentHeight;
                float heightScaleAdjustment = Mathf.Clamp(maxWalkFrameScaleAdjustment, 0f, 0.5f);
                float minScale = 1f - heightScaleAdjustment;
                float maxScale = 1f + heightScaleAdjustment;
                frameScale = Mathf.Clamp(frameScale, minScale, maxScale);
                targetScale = new Vector3(
                    baseScale.x * frameScale,
                    baseScale.y * frameScale,
                    baseScale.z);
            }

            if (normalizeWalkFrameWidth || visualSet.normalizeWalkFrameWidthOverride)
            {
                float referenceWidth = referenceSize.x;
                float currentWidth = GetSpriteDisplaySize(currentSprite).x;

                if (referenceWidth > 0f && currentWidth > 0f)
                {
                    float frameScale = referenceWidth / currentWidth;
                    float widthAdjustment = visualSet.normalizeWalkFrameWidthOverride
                        ? Mathf.Max(maxWalkFrameWidthAdjustment, 0.42f)
                        : maxWalkFrameWidthAdjustment;
                    float minScale = 1f - Mathf.Clamp01(widthAdjustment);
                    float maxScale = 1f + Mathf.Clamp01(widthAdjustment);
                    frameScale = Mathf.Clamp(frameScale, minScale, maxScale);
                    targetScale = new Vector3(
                        targetScale.x * frameScale,
                        targetScale.y,
                        targetScale.z);
                }
            }
        }

        if (enableWalkPoseScale && (visualSet == null || !visualSet.disableWalkPoseSquash))
        {
            int normalizedFrame = GetNormalizedWalkFrameIndex(walkFrameIndex);
            float frameWeight = normalizedFrame == 1 ? -1f : normalizedFrame == 3 ? 1f : 0f;
            float xOffset = walkStepSquash * frameWeight;
            float yOffset = -walkStepStretch * Mathf.Abs(frameWeight);
            targetScale = new Vector3(
                targetScale.x * (1f + xOffset),
                targetScale.y * (1f + yOffset),
                targetScale.z);
        }

        guestTransform.localScale = targetScale;
    }

    private Vector2 GetReferenceSpriteSize(GuestVisualSet visualSet, GuestFacingDirection direction)
    {
        if (visualSet == null)
        {
            return Vector2.zero;
        }

        Sprite referenceSprite = GetIdleSprite(direction, visualSet);

        if (!HasUsableSpriteSize(referenceSprite))
        {
            referenceSprite = visualSet.backIdleSprite != null
                ? visualSet.backIdleSprite
                : visualSet.frontIdleSprite;
        }

        if (!HasUsableSpriteSize(referenceSprite))
        {
            referenceSprite = visualSet.leftIdleSprite != null
                ? visualSet.leftIdleSprite
                : visualSet.rightIdleSprite;
        }

        Vector2 referenceSize = GetSpriteDisplaySize(referenceSprite);

        if (visualSet.hasCommonReferenceSpriteSize)
        {
            if (normalizeWalkDirectionsToCommonHeight && visualSet.commonReferenceSpriteSize.y > 0f)
            {
                referenceSize.y = visualSet.commonReferenceSpriteSize.y;
            }

            if ((normalizeWalkDirectionsToCommonWidth || visualSet.normalizeWalkDirectionsToCommonWidthOverride)
                && visualSet.commonReferenceSpriteSize.x > 0f)
            {
                referenceSize.x = visualSet.commonReferenceSpriteSize.x;
            }
        }

        return referenceSize;
    }

    private void CacheCommonReferenceSpriteSize(GuestVisualSet visualSet)
    {
        if (visualSet == null)
        {
            return;
        }

        Vector2 idleSizeTotal = Vector2.zero;
        int idleSpriteCount = 0;
        AccumulateSpriteSize(ref idleSizeTotal, ref idleSpriteCount, visualSet.frontIdleSprite);
        AccumulateSpriteSize(ref idleSizeTotal, ref idleSpriteCount, visualSet.backIdleSprite);
        AccumulateSpriteSize(ref idleSizeTotal, ref idleSpriteCount, visualSet.leftIdleSprite);
        AccumulateSpriteSize(ref idleSizeTotal, ref idleSpriteCount, visualSet.rightIdleSprite);

        visualSet.commonReferenceSpriteSize = idleSpriteCount > 0
            ? idleSizeTotal / idleSpriteCount
            : Vector2.zero;
        visualSet.hasCommonReferenceSpriteSize = visualSet.commonReferenceSpriteSize.x > 0f
            && visualSet.commonReferenceSpriteSize.y > 0f;
    }

    private void AccumulateSpriteSize(ref Vector2 sizeTotal, ref int spriteCount, Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        Vector2 displaySize = GetSpriteDisplaySize(sprite);

        if (displaySize.x <= 0f || displaySize.y <= 0f)
        {
            return;
        }

        sizeTotal += displaySize;
        spriteCount++;
    }

    private bool HasUsableSpriteSize(Sprite sprite)
    {
        Vector2 displaySize = GetSpriteDisplaySize(sprite);
        return displaySize.x > 0f && displaySize.y > 0f;
    }

    private Vector2 GetSpriteDisplaySize(Sprite sprite)
    {
        if (sprite == null)
        {
            return Vector2.zero;
        }

#if UNITY_EDITOR
        Vector2 visibleSize = GetSpriteVisibleDisplaySize(sprite);
        if (visibleSize.x > 0f && visibleSize.y > 0f)
        {
            return visibleSize;
        }
#endif

        Vector2 boundsSize = sprite.bounds.size;
        if (boundsSize.x > 0f && boundsSize.y > 0f)
        {
            return boundsSize;
        }

        return new Vector2(sprite.rect.width, sprite.rect.height);
    }

#if UNITY_EDITOR
    private Vector2 GetSpriteVisibleDisplaySize(Sprite sprite)
    {
        if (sprite == null)
        {
            return Vector2.zero;
        }

        if (spriteVisibleSizeCache.TryGetValue(sprite, out Vector2 cachedSize))
        {
            return cachedSize;
        }

        Vector2 visibleSize = Vector2.zero;
        string assetPath = AssetDatabase.GetAssetPath(sprite);

        if (!string.IsNullOrEmpty(assetPath) && File.Exists(assetPath))
        {
            byte[] imageBytes = File.ReadAllBytes(assetPath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (texture.LoadImage(imageBytes))
            {
                Rect spriteRect = sprite.rect;
                int startX = Mathf.Clamp(Mathf.FloorToInt(spriteRect.x), 0, texture.width - 1);
                int startY = Mathf.Clamp(Mathf.FloorToInt(spriteRect.y), 0, texture.height - 1);
                int endX = Mathf.Clamp(Mathf.CeilToInt(spriteRect.xMax), startX + 1, texture.width);
                int endY = Mathf.Clamp(Mathf.CeilToInt(spriteRect.yMax), startY + 1, texture.height);
                int minX = endX;
                int minY = endY;
                int maxX = startX - 1;
                int maxY = startY - 1;

                for (int y = startY; y < endY; y++)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        if (texture.GetPixel(x, y).a <= 0.05f)
                        {
                            continue;
                        }

                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }
                }

                if (maxX >= minX && maxY >= minY && sprite.pixelsPerUnit > 0f)
                {
                    visibleSize = new Vector2(
                        (maxX - minX + 1) / sprite.pixelsPerUnit,
                        (maxY - minY + 1) / sprite.pixelsPerUnit);
                }
            }

            Destroy(texture);
        }

        spriteVisibleSizeCache[sprite] = visibleSize;
        return visibleSize;
    }
#endif

    private int GetNextWalkFrameIndex(int currentFrameIndex)
    {
        int cycleLength = useIdleFrameInWalkCycle ? 4 : 2;
        return (currentFrameIndex + 1) % cycleLength;
    }

    private int GetNormalizedWalkFrameIndex(int walkFrameIndex)
    {
        return useIdleFrameInWalkCycle
            ? Mathf.Abs(walkFrameIndex) % 4
            : (Mathf.Abs(walkFrameIndex) % 2 == 0 ? 1 : 3);
    }

    private Sprite GetWalkSprite(Vector2 currentPosition, Vector2 destination, GuestVisualSet visualSet, int walkFrameIndex)
    {
        GuestFacingDirection direction = GetFacingDirection(currentPosition, destination);

        Sprite walkSprite = GetDirectionalWalkSprite(direction, visualSet, walkFrameIndex);

        if (walkSprite != null)
        {
            return walkSprite;
        }

        return visualSet.backIdleSprite;
    }

    private GuestFacingDirection GetFacingDirection(Vector2 currentPosition, Vector2 destination)
    {
        Vector2 delta = destination - currentPosition;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            return delta.x >= 0f ? GuestFacingDirection.Right : GuestFacingDirection.Left;
        }

        return delta.y < -0.03f ? GuestFacingDirection.Front : GuestFacingDirection.Back;
    }

    private Sprite GetDirectionalWalkSprite(GuestFacingDirection direction, GuestVisualSet visualSet, int walkFrameIndex)
    {
        Sprite idle = GetIdleSprite(direction, visualSet);
        Sprite walk01 = GetWalkSprite01(direction, visualSet);
        Sprite walk02 = GetWalkSprite02(direction, visualSet);
        int normalizedFrame = GetNormalizedWalkFrameIndex(walkFrameIndex);

        if ((normalizedFrame == 0 || normalizedFrame == 2) && idle != null)
        {
            return idle;
        }

        if (normalizedFrame == 3 && walk02 != null)
        {
            return walk02;
        }

        if (walk01 != null)
        {
            return walk01;
        }

        if (idle != null)
        {
            return idle;
        }

        return GetFallbackWalkSprite(visualSet, normalizedFrame);
    }

    private Sprite GetFallbackWalkSprite(GuestVisualSet visualSet, int normalizedFrame)
    {
        if (normalizedFrame == 3 && visualSet.backWalkSprite02 != null)
        {
            return visualSet.backWalkSprite02;
        }

        if (visualSet.backWalkSprite01 != null)
        {
            return visualSet.backWalkSprite01;
        }

        if (visualSet.frontWalkSprite01 != null)
        {
            return visualSet.frontWalkSprite01;
        }

        return visualSet.backIdleSprite != null ? visualSet.backIdleSprite : visualSet.frontIdleSprite;
    }

    private Sprite GetIdleSprite(GuestFacingDirection direction, GuestVisualSet visualSet)
    {
        if (visualSet == null)
        {
            return null;
        }

        switch (direction)
        {
            case GuestFacingDirection.Front:
                return visualSet.frontIdleSprite != null ? visualSet.frontIdleSprite : visualSet.backIdleSprite;
            case GuestFacingDirection.Back:
                return visualSet.backIdleSprite != null ? visualSet.backIdleSprite : visualSet.frontIdleSprite;
            case GuestFacingDirection.Left:
                return visualSet.leftIdleSprite != null ? visualSet.leftIdleSprite : visualSet.frontIdleSprite;
            case GuestFacingDirection.Right:
                return visualSet.rightIdleSprite != null ? visualSet.rightIdleSprite : visualSet.frontIdleSprite;
            default:
                return visualSet.backIdleSprite;
        }
    }

    private Sprite GetWalkSprite01(GuestFacingDirection direction, GuestVisualSet visualSet)
    {
        switch (direction)
        {
            case GuestFacingDirection.Front:
                return visualSet.frontWalkSprite01;
            case GuestFacingDirection.Back:
                return visualSet.backWalkSprite01;
            case GuestFacingDirection.Left:
                return visualSet.leftWalkSprite01;
            case GuestFacingDirection.Right:
                return visualSet.rightWalkSprite01;
            default:
                return null;
        }
    }

    private Sprite GetWalkSprite02(GuestFacingDirection direction, GuestVisualSet visualSet)
    {
        switch (direction)
        {
            case GuestFacingDirection.Front:
                return visualSet.frontWalkSprite02;
            case GuestFacingDirection.Back:
                return visualSet.backWalkSprite02;
            case GuestFacingDirection.Left:
                return visualSet.leftWalkSprite02;
            case GuestFacingDirection.Right:
                return visualSet.rightWalkSprite02;
            default:
                return null;
        }
    }

    private void WarnForMissingDirectionalSprites(string guestId, GuestVisualSet visualSet)
    {
        WarnIfMissing(guestId, "front", "idle", visualSet.frontIdleSprite);
        WarnIfMissing(guestId, "front", "walk_01", visualSet.frontWalkSprite01);
        WarnIfMissing(guestId, "front", "walk_02", visualSet.frontWalkSprite02);
        WarnIfMissing(guestId, "back", "idle", visualSet.backIdleSprite);
        WarnIfMissing(guestId, "back", "walk_01", visualSet.backWalkSprite01);
        WarnIfMissing(guestId, "back", "walk_02", visualSet.backWalkSprite02);
        WarnIfMissing(guestId, "left", "idle", visualSet.leftIdleSprite);
        WarnIfMissing(guestId, "left", "walk_01", visualSet.leftWalkSprite01);
        WarnIfMissing(guestId, "left", "walk_02", visualSet.leftWalkSprite02);
        WarnIfMissing(guestId, "right", "idle", visualSet.rightIdleSprite);
        WarnIfMissing(guestId, "right", "walk_01", visualSet.rightWalkSprite01);
        WarnIfMissing(guestId, "right", "walk_02", visualSet.rightWalkSprite02);
    }

    private void WarnIfMissing(string guestId, string direction, string state, Sprite sprite)
    {
        if (sprite != null)
        {
            return;
        }

        LogMissingSpriteOnce($"Missing cafe visitor sprite: guest_{guestId}_{direction}_{state}.png");
    }

    private void LogMissingSpriteOnce(string message)
    {
        if (loggedMissingSprites.Add(message))
        {
            Debug.LogWarning(message);
        }
    }

    private enum GuestFacingDirection
    {
        Front,
        Back,
        Left,
        Right
    }

    public enum VisitorSpecialVisualState
    {
        Seated,
        Sleepy,
        Sleeping
    }

    [System.Serializable]
    private class VisitorVisualMapping
    {
        public string visitorId;
        public Sprite idleSprite;
        public GameObject idleAnimatorPrefab;
        public Sprite seatedSprite;
        public Sprite sleepySprite;
        public Sprite sleepingSprite;
        public Sprite fallbackSprite;
        public Vector3 scaleMultiplier = Vector3.one;
        public Vector2 seatedOffset;
    }

    [System.Serializable]
    private class GuestVisualSet
    {
        public string guestId;
        public Sprite backIdleSprite;
        public Sprite backWalkSprite01;
        public Sprite backWalkSprite02;
        public Sprite frontIdleSprite;
        public Sprite frontWalkSprite01;
        public Sprite frontWalkSprite02;
        public Sprite leftIdleSprite;
        public Sprite leftWalkSprite01;
        public Sprite leftWalkSprite02;
        public Sprite rightIdleSprite;
        public Sprite rightWalkSprite01;
        public Sprite rightWalkSprite02;
        public Vector3 scaleMultiplier = Vector3.one;
        public Vector2 seatedOffset;
        public string sourceLabel;
        public Vector2 commonReferenceSpriteSize;
        public bool hasCommonReferenceSpriteSize;
        public bool normalizeWalkFrameWidthOverride;
        public bool normalizeWalkDirectionsToCommonWidthOverride;
        public bool disableWalkPoseSquash;
    }

    private class GuestRuntimeVisual
    {
        public GameObject GuestObject { get; }
        public Transform Transform => GuestObject != null ? GuestObject.transform : null;
        public SpriteRenderer SpriteRenderer { get; }
        public GuestVisualSet VisualSet { get; }
        public GameObject RequestBubbleObject { get; }

        public GuestRuntimeVisual(GameObject guestObject, SpriteRenderer spriteRenderer, GuestVisualSet visualSet, GameObject requestBubbleObject)
        {
            GuestObject = guestObject;
            SpriteRenderer = spriteRenderer;
            VisualSet = visualSet;
            RequestBubbleObject = requestBubbleObject;
        }
    }

    private class CafeRequestBubbleClickTarget : MonoBehaviour
    {
        private CafeGuestArrivalController owner;
        private int guestIndex = -1;
        private Vector3 baseScale = Vector3.one;

        public void Configure(CafeGuestArrivalController targetOwner, int targetGuestIndex)
        {
            owner = targetOwner;
            guestIndex = targetGuestIndex;
            baseScale = transform.localScale;
        }

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void OnMouseDown()
        {
            if (owner == null)
            {
                owner = GetComponentInParent<CafeGuestArrivalController>();
            }

            if (owner != null)
            {
                owner.TryServeGuestRequest(guestIndex);
            }
        }

        private void OnMouseEnter()
        {
            if (owner != null)
            {
                transform.localScale = new Vector3(
                    baseScale.x * owner.requestBubbleHoverScale.x,
                    baseScale.y * owner.requestBubbleHoverScale.y,
                    baseScale.z * owner.requestBubbleHoverScale.z);
            }
        }

        private void OnMouseExit()
        {
            transform.localScale = baseScale;
        }
    }
}
