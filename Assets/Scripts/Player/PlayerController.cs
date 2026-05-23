using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.75f, 0.12f);
    [SerializeField] private LayerMask groundLayer = ~0;

    private Rigidbody2D body;
    private BoxCollider2D playerCollider;
    private float moveInput;
    private bool jumpQueued;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        ReadMovementInput();
    }

    private void FixedUpdate()
    {
        bool isGrounded = CheckGrounded();
        Vector2 velocity = body.linearVelocity;
        velocity.x = moveInput * moveSpeed;

        // Queue the jump in Update, then apply it during physics for consistent movement.
        if (jumpQueued && isGrounded)
        {
            velocity.y = jumpForce;
        }

        body.linearVelocity = velocity;
        jumpQueued = false;
    }

    private void ReadMovementInput()
    {
        moveInput = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            moveInput -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            moveInput += 1f;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            jumpQueued = true;
        }
    }

    private bool CheckGrounded()
    {
        if (groundCheck == null)
        {
            return false;
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll(groundCheck.position, groundCheckSize, 0f, groundLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit != playerCollider && !hit.isTrigger)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}
