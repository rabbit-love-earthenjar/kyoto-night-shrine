using UnityEngine;

public class HubPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite frontIdleSprite;
    [SerializeField] private Sprite[] frontWalkSprites;
    [SerializeField] private Sprite backIdleSprite;
    [SerializeField] private Sprite[] backWalkSprites;
    [SerializeField] private Sprite leftIdleSprite;
    [SerializeField] private Sprite[] leftWalkSprites;
    [SerializeField] private Sprite rightIdleSprite;
    [SerializeField] private Sprite[] rightWalkSprites;
    [SerializeField] private float walkFrameDuration = 0.18f;
    [SerializeField] private Vector2 movementBoundsMin = new Vector2(-6.4f, -4.3f);
    [SerializeField] private Vector2 movementBoundsMax = new Vector2(6.4f, 4.3f);

    private enum FacingDirection
    {
        Front,
        Back,
        Left,
        Right
    }

    private FacingDirection facingDirection = FacingDirection.Front;
    private float walkTimer;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        SetSprite(GetIdleSprite(facingDirection));
    }

    private void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        Vector3 nextPosition = transform.position + (Vector3)(input * moveSpeed * Time.deltaTime);
        nextPosition.x = Mathf.Clamp(nextPosition.x, movementBoundsMin.x, movementBoundsMax.x);
        nextPosition.y = Mathf.Clamp(nextPosition.y, movementBoundsMin.y, movementBoundsMax.y);
        transform.position = nextPosition;

        UpdateVisual(input);
    }

    private void UpdateVisual(Vector2 input)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        bool isMoving = input.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            facingDirection = GetFacingDirection(input);
            walkTimer += Time.deltaTime;

            Sprite[] walkSprites = GetWalkSprites(facingDirection);

            if (walkSprites != null && walkSprites.Length > 0)
            {
                int frameIndex = Mathf.FloorToInt(walkTimer / Mathf.Max(0.01f, walkFrameDuration)) % walkSprites.Length;
                SetSprite(walkSprites[frameIndex]);
                return;
            }
        }
        else
        {
            walkTimer = 0f;
        }

        SetSprite(GetIdleSprite(facingDirection));
    }

    private FacingDirection GetFacingDirection(Vector2 input)
    {
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            return input.x < 0f ? FacingDirection.Left : FacingDirection.Right;
        }

        return input.y > 0f ? FacingDirection.Back : FacingDirection.Front;
    }

    private Sprite GetIdleSprite(FacingDirection direction)
    {
        switch (direction)
        {
            case FacingDirection.Back:
                return backIdleSprite != null ? backIdleSprite : idleSprite;
            case FacingDirection.Left:
                return leftIdleSprite != null ? leftIdleSprite : idleSprite;
            case FacingDirection.Right:
                return rightIdleSprite != null ? rightIdleSprite : idleSprite;
            default:
                return frontIdleSprite != null ? frontIdleSprite : idleSprite;
        }
    }

    private Sprite[] GetWalkSprites(FacingDirection direction)
    {
        switch (direction)
        {
            case FacingDirection.Back:
                return backWalkSprites;
            case FacingDirection.Left:
                return leftWalkSprites;
            case FacingDirection.Right:
                return rightWalkSprites;
            default:
                return frontWalkSprites;
        }
    }

    private void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }
}
