using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class GhostEnemy : MonoBehaviour
{
    [SerializeField] private float hoverDistance = 0.45f;
    [SerializeField] private float hoverSpeed = 2f;
    [SerializeField] private float bobDistance = 0.18f;
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float destroyDelay = 0.05f;
    [SerializeField] private int contactDamage = 1;

    private SpriteRenderer spriteRenderer;
    private Collider2D ghostCollider;
    private Vector3 startPosition;
    private bool fallbackDefeated;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ghostCollider = GetComponent<Collider2D>();
        startPosition = transform.position;
    }

    private void Update()
    {
        if (fallbackDefeated)
        {
            return;
        }

        float hoverX = Mathf.Sin(Time.time * hoverSpeed) * hoverDistance;
        float bobY = Mathf.Sin(Time.time * bobSpeed) * bobDistance;
        transform.position = startPosition + new Vector3(hoverX, bobY, 0f);
    }

    public void ApplyKnockback(Vector2 direction, float distance)
    {
        if (direction.sqrMagnitude < 0.01f || distance <= 0f)
        {
            return;
        }

        startPosition += (Vector3)(direction.normalized * distance);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    public void TakeHit()
    {
        GhostHealth health = GetComponent<GhostHealth>();

        if (health != null)
        {
            health.TakeDamage(1);
            return;
        }

        if (fallbackDefeated)
        {
            return;
        }

        fallbackDefeated = true;

        if (ghostCollider != null)
        {
            ghostCollider.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        Destroy(gameObject, destroyDelay);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (fallbackDefeated)
        {
            return;
        }

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(contactDamage, transform.position);
        }
    }
}
