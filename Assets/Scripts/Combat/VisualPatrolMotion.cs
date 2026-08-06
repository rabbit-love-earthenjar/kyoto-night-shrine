using UnityEngine;

public class VisualPatrolMotion : MonoBehaviour
{
    [SerializeField] private float leftBoundX;
    [SerializeField] private float rightBoundX;
    [SerializeField, Min(0f)] private float moveSpeed = 0.75f;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private bool startsMovingLeft = true;
    [SerializeField] private bool spriteFacesRightByDefault = true;

    private int moveDirection;

    public float LeftBoundX => leftBoundX;
    public float RightBoundX => rightBoundX;
    public float MoveSpeed => moveSpeed;
    public bool IsFacingLeft => moveDirection < 0;
    public bool SpriteFacesRightByDefault => spriteFacesRightByDefault;

    private void OnEnable()
    {
        ResetDirection();
    }

    private void Update()
    {
        if (moveSpeed <= 0f || rightBoundX <= leftBoundX)
        {
            return;
        }

        float targetX = moveDirection < 0 ? leftBoundX : rightBoundX;
        Vector3 position = transform.position;
        position.x = Mathf.MoveTowards(position.x, targetX, moveSpeed * Time.deltaTime);
        transform.position = position;

        if (Mathf.Abs(position.x - targetX) <= 0.001f)
        {
            moveDirection *= -1;
            ApplyFacing();
        }
    }

    public void Configure(
        float minimumX,
        float maximumX,
        float speed,
        SpriteRenderer renderer,
        bool moveLeftFirst = true,
        bool facesRightByDefault = true)
    {
        leftBoundX = Mathf.Min(minimumX, maximumX);
        rightBoundX = Mathf.Max(minimumX, maximumX);
        moveSpeed = Mathf.Max(0f, speed);
        visualRenderer = renderer;
        startsMovingLeft = moveLeftFirst;
        spriteFacesRightByDefault = facesRightByDefault;
        ResetDirection();
    }

    private void ResetDirection()
    {
        moveDirection = startsMovingLeft ? -1 : 1;
        ApplyFacing();
    }

    private void ApplyFacing()
    {
        if (visualRenderer != null)
        {
            bool movingLeft = moveDirection < 0;
            visualRenderer.flipX = spriteFacesRightByDefault ? movingLeft : !movingLeft;
        }
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);

        if (rightBoundX < leftBoundX)
        {
            (leftBoundX, rightBoundX) = (rightBoundX, leftBoundX);
        }
    }
}
