using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SimpleOneWayPlatform : MonoBehaviour
{
    [SerializeField] private float topTolerance = 0.08f;

    private Collider2D platformCollider;
    private Collider2D playerCollider;
    private Rigidbody2D playerBody;

    private void Awake()
    {
        platformCollider = GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        EnsurePlayerReferences();

        if (platformCollider == null || playerCollider == null || playerBody == null)
        {
            return;
        }

        float playerBottom = playerCollider.bounds.min.y;
        float platformTop = platformCollider.bounds.max.y;
        bool shouldIgnore = playerBottom < platformTop - topTolerance || playerBody.linearVelocity.y > 0.01f;

        Physics2D.IgnoreCollision(playerCollider, platformCollider, shouldIgnore);
    }

    private void EnsurePlayerReferences()
    {
        if (playerCollider != null && playerBody != null)
        {
            return;
        }

        PlayerController player = FindAnyObjectByType<PlayerController>();

        if (player == null)
        {
            return;
        }

        playerCollider = player.GetComponent<Collider2D>();
        playerBody = player.GetComponent<Rigidbody2D>();
    }
}
