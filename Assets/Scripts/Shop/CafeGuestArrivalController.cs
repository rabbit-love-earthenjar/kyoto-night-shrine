using System.Collections;
using UnityEngine;

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
    [SerializeField] private GuestVisualSet[] guestVisuals = new GuestVisualSet[4];

    private CafeOperationController operationController;
    private GuestRuntimeVisual[] activeGuestVisuals;
    private Coroutine[] departureCoroutines;
    private bool arrivalSequenceStarted;

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
        GuestVisualSet visualSet = GetVisualSet(guestState.GuestId, guestIndex);
        GameObject seatObject = GameObject.Find($"GuestSeat_{guestIndex + 1:00}");

        if (visualSet == null || visualSet.backIdleSprite == null || seatObject == null)
        {
            yield break;
        }

        GameObject guestObject = new GameObject($"CafeGuestVisual_{guestIndex + 1:00}_{guestState.GuestId}");
        guestObject.transform.SetParent(transform, false);
        guestObject.transform.position = entrancePosition;
        guestObject.transform.localScale = Vector3.Scale(guestScale, visualSet.scaleMultiplier);

        SpriteRenderer spriteRenderer = guestObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = visualSet.backIdleSprite;
        spriteRenderer.sortingOrder = guestSortingOrder;
        activeGuestVisuals[guestIndex] = new GuestRuntimeVisual(guestObject, spriteRenderer, visualSet);

        yield return MoveGuest(guestObject.transform, spriteRenderer, visualSet, counterApproachPosition);
        Vector2 seatedPosition = (Vector2)seatObject.transform.position + seatedOffset + visualSet.seatedOffset;
        yield return MoveGuest(guestObject.transform, spriteRenderer, visualSet, seatedPosition);

        spriteRenderer.sprite = visualSet.backIdleSprite;
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
        departureCoroutines[guestIndex] = null;
    }

    private GuestVisualSet GetVisualSet(string guestId, int guestIndex)
    {
        for (int i = 0; i < guestVisuals.Length; i++)
        {
            GuestVisualSet visualSet = guestVisuals[i];

            if (visualSet != null && visualSet.guestId == guestId)
            {
                return visualSet;
            }
        }

        return guestIndex >= 0 && guestIndex < guestVisuals.Length ? guestVisuals[guestIndex] : null;
    }

    private IEnumerator MoveGuest(
        Transform guestTransform,
        SpriteRenderer spriteRenderer,
        GuestVisualSet visualSet,
        Vector2 destination)
    {
        float nextFrameTime = Time.time + walkFrameInterval;
        bool useSecondFrame = false;

        while (Vector2.Distance(guestTransform.position, destination) > 0.02f)
        {
            guestTransform.position = Vector2.MoveTowards(
                guestTransform.position,
                destination,
                moveSpeed * Time.deltaTime);

            if (Time.time >= nextFrameTime)
            {
                useSecondFrame = !useSecondFrame;
                spriteRenderer.sprite = useSecondFrame && visualSet.backWalkSprite02 != null
                    ? visualSet.backWalkSprite02
                    : visualSet.backWalkSprite01 ?? visualSet.backIdleSprite;
                nextFrameTime = Time.time + walkFrameInterval;
            }

            yield return null;
        }
    }

    [System.Serializable]
    private class GuestVisualSet
    {
        public string guestId;
        public Sprite backIdleSprite;
        public Sprite backWalkSprite01;
        public Sprite backWalkSprite02;
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
