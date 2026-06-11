using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
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
    [SerializeField] private float walkStepSquash = 0.035f;
    [SerializeField] private float walkStepStretch = 0.02f;
    [SerializeField] private GuestVisualSet[] guestVisuals = new GuestVisualSet[4];

    private CafeOperationController operationController;
    private GuestRuntimeVisual[] activeGuestVisuals;
    private Coroutine[] departureCoroutines;
    private bool arrivalSequenceStarted;
    private readonly HashSet<string> loggedMissingSprites = new HashSet<string>();

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
        GuestVisualSet visualSet = GetVisualSet(guestState.VisualId, guestIndex);
        GameObject seatObject = GameObject.Find($"GuestSeat_{guestIndex + 1:00}");

        if (visualSet == null || GetIdleSprite(GuestFacingDirection.Back, visualSet) == null || seatObject == null)
        {
            if (visualSet == null)
            {
                LogMissingSpriteOnce($"Cafe visitor visual set is missing for {guestState.VisualId}.");
            }
            else if (GetIdleSprite(GuestFacingDirection.Back, visualSet) == null)
            {
                LogMissingSpriteOnce($"Cafe visitor has no usable idle sprite for {guestState.VisualId}.");
            }

            yield break;
        }

        WarnForMissingDirectionalSprites(guestState.VisualId, visualSet);

        GameObject guestObject = new GameObject($"CafeGuestVisual_{guestIndex + 1:00}_{guestState.VisitorId}");
        guestObject.transform.SetParent(transform, false);
        guestObject.transform.position = entrancePosition;
        guestObject.transform.localScale = Vector3.Scale(guestScale, visualSet.scaleMultiplier);

        SpriteRenderer spriteRenderer = guestObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetIdleSprite(GuestFacingDirection.Back, visualSet);
        spriteRenderer.sortingOrder = guestSortingOrder;
        activeGuestVisuals[guestIndex] = new GuestRuntimeVisual(guestObject, spriteRenderer, visualSet);

        yield return MoveGuest(guestObject.transform, spriteRenderer, visualSet, counterApproachPosition);
        Vector2 seatedPosition = (Vector2)seatObject.transform.position + seatedOffset + visualSet.seatedOffset;
        yield return MoveGuest(guestObject.transform, spriteRenderer, visualSet, seatedPosition);

        spriteRenderer.sprite = GetIdleSprite(GuestFacingDirection.Back, visualSet);
        guestObject.transform.position = seatedPosition;
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

        GuestRuntimeVisual runtimeVisual = activeGuestVisuals != null
            && guestIndex >= 0
            && guestIndex < activeGuestVisuals.Length
            ? activeGuestVisuals[guestIndex]
            : null;

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

    private GuestVisualSet GetVisualSet(string guestId, int guestIndex)
    {
        for (int i = 0; i < guestVisuals.Length; i++)
        {
            GuestVisualSet visualSet = guestVisuals[i];

            if (visualSet != null && visualSet.guestId == guestId)
            {
                CompleteVisualSetFromProjectAssets(visualSet, guestId);
                return visualSet;
            }
        }

        GuestVisualSet fallbackVisualSet = guestIndex >= 0 && guestIndex < guestVisuals.Length ? guestVisuals[guestIndex] : null;

        if (fallbackVisualSet != null)
        {
            CompleteVisualSetFromProjectAssets(fallbackVisualSet, guestId);
        }

        return fallbackVisualSet;
    }

    private void CompleteVisualSetFromProjectAssets(GuestVisualSet visualSet, string guestId)
    {
        if (visualSet == null || string.IsNullOrEmpty(guestId))
        {
            return;
        }

        string assetId = GetGuestAssetId(guestId);
        visualSet.frontIdleSprite = visualSet.frontIdleSprite != null ? visualSet.frontIdleSprite : LoadGuestSprite(assetId, "front", "idle");
        visualSet.frontWalkSprite01 = visualSet.frontWalkSprite01 != null ? visualSet.frontWalkSprite01 : LoadGuestSprite(assetId, "front", "walk_01");
        visualSet.frontWalkSprite02 = visualSet.frontWalkSprite02 != null ? visualSet.frontWalkSprite02 : LoadGuestSprite(assetId, "front", "walk_02");
        visualSet.backIdleSprite = visualSet.backIdleSprite != null ? visualSet.backIdleSprite : LoadGuestSprite(assetId, "back", "idle");
        visualSet.backWalkSprite01 = visualSet.backWalkSprite01 != null ? visualSet.backWalkSprite01 : LoadGuestSprite(assetId, "back", "walk_01");
        visualSet.backWalkSprite02 = visualSet.backWalkSprite02 != null ? visualSet.backWalkSprite02 : LoadGuestSprite(assetId, "back", "walk_02");
        visualSet.leftIdleSprite = visualSet.leftIdleSprite != null ? visualSet.leftIdleSprite : LoadGuestSprite(assetId, "left", "idle");
        visualSet.leftWalkSprite01 = visualSet.leftWalkSprite01 != null ? visualSet.leftWalkSprite01 : LoadGuestSprite(assetId, "left", "walk_01");
        visualSet.leftWalkSprite02 = visualSet.leftWalkSprite02 != null ? visualSet.leftWalkSprite02 : LoadGuestSprite(assetId, "left", "walk_02");
        visualSet.rightIdleSprite = visualSet.rightIdleSprite != null ? visualSet.rightIdleSprite : LoadGuestSprite(assetId, "right", "idle");
        visualSet.rightWalkSprite01 = visualSet.rightWalkSprite01 != null ? visualSet.rightWalkSprite01 : LoadGuestSprite(assetId, "right", "walk_01");
        visualSet.rightWalkSprite02 = visualSet.rightWalkSprite02 != null ? visualSet.rightWalkSprite02 : LoadGuestSprite(assetId, "right", "walk_02");
    }

    private string GetGuestAssetId(string guestId)
    {
        switch (guestId)
        {
            case "worshipper":
                return "gramma";
            case "small_yokai":
                return "nekomata";
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

        string assetPath = $"Assets/Art/cafe_icon/guest_{assetId}/guest_{assetId}_{direction}_{state}.png";
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
#endif

        return null;
    }

    private IEnumerator MoveGuest(
        Transform guestTransform,
        SpriteRenderer spriteRenderer,
        GuestVisualSet visualSet,
        Vector2 destination)
    {
        float nextFrameTime = Time.time + walkFrameInterval;
        bool useSecondFrame = false;
        Vector3 baseScale = guestTransform.localScale;
        spriteRenderer.sprite = GetWalkSprite(guestTransform.position, destination, visualSet, useSecondFrame);
        ApplyWalkPoseScale(guestTransform, baseScale, useSecondFrame);

        while (Vector2.Distance(guestTransform.position, destination) > 0.02f)
        {
            guestTransform.position = Vector2.MoveTowards(
                guestTransform.position,
                destination,
                moveSpeed * Time.deltaTime);

            if (Time.time >= nextFrameTime)
            {
                useSecondFrame = !useSecondFrame;
                spriteRenderer.sprite = GetWalkSprite(guestTransform.position, destination, visualSet, useSecondFrame);
                ApplyWalkPoseScale(guestTransform, baseScale, useSecondFrame);
                nextFrameTime = Time.time + walkFrameInterval;
            }

            yield return null;
        }

        guestTransform.localScale = baseScale;
    }

    private void ApplyWalkPoseScale(Transform guestTransform, Vector3 baseScale, bool useSecondFrame)
    {
        float xOffset = useSecondFrame ? walkStepSquash : -walkStepSquash;
        float yOffset = useSecondFrame ? -walkStepStretch : walkStepStretch;
        guestTransform.localScale = new Vector3(
            baseScale.x * (1f + xOffset),
            baseScale.y * (1f + yOffset),
            baseScale.z);
    }

    private Sprite GetWalkSprite(Vector2 currentPosition, Vector2 destination, GuestVisualSet visualSet, bool useSecondFrame)
    {
        GuestFacingDirection direction = GetFacingDirection(currentPosition, destination);

        Sprite walkSprite = GetDirectionalWalkSprite(direction, visualSet, useSecondFrame);

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

    private Sprite GetDirectionalWalkSprite(GuestFacingDirection direction, GuestVisualSet visualSet, bool useSecondFrame)
    {
        Sprite idle = GetIdleSprite(direction, visualSet);
        Sprite walk01 = GetWalkSprite01(direction, visualSet);
        Sprite walk02 = GetWalkSprite02(direction, visualSet);

        if (useSecondFrame && walk02 != null)
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

        return GetFallbackWalkSprite(visualSet, useSecondFrame);
    }

    private Sprite GetFallbackWalkSprite(GuestVisualSet visualSet, bool useSecondFrame)
    {
        if (useSecondFrame && visualSet.backWalkSprite02 != null)
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
    }

    private class GuestRuntimeVisual
    {
        public GameObject GuestObject { get; }
        public Transform Transform => GuestObject != null ? GuestObject.transform : null;
        public SpriteRenderer SpriteRenderer { get; }
        public GuestVisualSet VisualSet { get; }

        public GuestRuntimeVisual(GameObject guestObject, SpriteRenderer spriteRenderer, GuestVisualSet visualSet)
        {
            GuestObject = guestObject;
            SpriteRenderer = spriteRenderer;
            VisualSet = visualSet;
        }
    }
}
