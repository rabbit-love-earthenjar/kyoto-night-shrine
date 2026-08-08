using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class FaithBeanProjectile : MonoBehaviour
{
    private int damage;
    private float expiresAt;
    private bool resolved;

    public void Initialize(Vector2 direction, float speed, int beanDamage, float lifetime)
    {
        damage = Mathf.Max(1, beanDamage);
        expiresAt = Time.time + Mathf.Max(0.1f, lifetime);

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.linearVelocity = direction.normalized * Mathf.Max(0.1f, speed);

        CircleCollider2D beanCollider = GetComponent<CircleCollider2D>();
        beanCollider.isTrigger = true;
        beanCollider.radius = 0.42f;
    }

    private void Update()
    {
        if (Time.time >= expiresAt)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (resolved || other == null)
        {
            return;
        }

        RedOniBossHealth bossHealth = other.GetComponentInParent<RedOniBossHealth>();

        if (bossHealth == null)
        {
            return;
        }

        resolved = true;
        bossHealth.TakeDamage(damage, transform.position);
        Destroy(gameObject);
    }
}
