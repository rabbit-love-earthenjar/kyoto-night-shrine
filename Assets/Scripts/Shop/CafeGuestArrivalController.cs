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
    [SerializeField] private GuestVisualSet[] guestVisuals = new GuestVisualSet[4];

    private CafeOperationController operationController;
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
        }
    }

    private void BeginGuestArrivals()
    {
        if (arrivalSequenceStarted)
        {
            return;
        }

        arrivalSequenceStarted = true;
        StartCoroutine(CreateGuestsInSequence());
    }

    private IEnumerator CreateGuestsInSequence()
    {
        yield return new WaitForSeconds(firstGuestDelay);

        for (int i = 0; i < guestVisuals.Length; i++)
        {
            StartCoroutine(CreateAndSeatGuest(i));
            yield return new WaitForSeconds(guestArrivalInterval);
        }
    }

    private IEnumerator CreateAndSeatGuest(int guestIndex)
    {
        GuestVisualSet visualSet = guestVisuals[guestIndex];
        GameObject seatObject = GameObject.Find($"GuestSeat_{guestIndex + 1:00}");

        if (visualSet == null || visualSet.backIdleSprite == null || seatObject == null)
        {
            yield break;
        }

        GameObject guestObject = new GameObject($"CafeGuestVisual_{guestIndex + 1:00}");
        guestObject.transform.SetParent(transform, false);
        guestObject.transform.position = entrancePosition;
        guestObject.transform.localScale = Vector3.Scale(guestScale, visualSet.scaleMultiplier);

        SpriteRenderer spriteRenderer = guestObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = visualSet.backIdleSprite;
        spriteRenderer.sortingOrder = guestSortingOrder;

        yield return MoveGuest(guestObject.transform, spriteRenderer, visualSet, counterApproachPosition);
        Vector2 seatedPosition = (Vector2)seatObject.transform.position + seatedOffset + visualSet.seatedOffset;
        yield return MoveGuest(guestObject.transform, spriteRenderer, visualSet, seatedPosition);

        spriteRenderer.sprite = visualSet.backIdleSprite;
        guestObject.transform.position = seatedPosition;
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
        public Sprite backIdleSprite;
        public Sprite backWalkSprite01;
        public Sprite backWalkSprite02;
        public Vector3 scaleMultiplier = Vector3.one;
        public Vector2 seatedOffset;
    }
}
