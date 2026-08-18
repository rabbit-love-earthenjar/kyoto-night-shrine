using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class FaithBeanProjectile : MonoBehaviour
{
    private int damage;
    private float expiresAt;
    private bool resolved;
    private Rigidbody2D body;
    private Vector2 arcStart;
    private Vector2 arcEnd;
    private float arcHeight;
    private float travelDuration;
    private float travelElapsed;
    private bool travelComplete;

    public void InitializeArc(
        Vector2 start,
        Vector2 target,
        float height,
        float speed,
        int beanDamage,
        float lifetime)
    {
        damage = Mathf.Max(1, beanDamage);
        expiresAt = Time.time + Mathf.Max(0.1f, lifetime);
        arcStart = start;
        arcEnd = target;
        arcHeight = Mathf.Max(0f, height);
        travelDuration = Mathf.Max(0.08f, Vector2.Distance(start, target) / Mathf.Max(0.1f, speed));
        travelElapsed = 0f;

        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.linearVelocity = Vector2.zero;
        body.position = start;

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

    private void FixedUpdate()
    {
        if (resolved || travelComplete || body == null)
        {
            return;
        }

        travelElapsed += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(travelElapsed / travelDuration);
        Vector2 nextPosition = EvaluateArc(arcStart, arcEnd, arcHeight, t);
        float tangentT = Mathf.Min(1f, t + 0.02f);
        Vector2 tangent = EvaluateArc(arcStart, arcEnd, arcHeight, tangentT) - nextPosition;

        body.MovePosition(nextPosition);

        if (tangent.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
        }

        if (t >= 1f)
        {
            travelComplete = true;
            expiresAt = Mathf.Min(expiresAt, Time.time + 0.08f);
        }
    }

    public static Vector2 EvaluateArc(
        Vector2 start,
        Vector2 end,
        float height,
        float t)
    {
        float clampedT = Mathf.Clamp01(t);
        Vector2 linearPosition = Vector2.Lerp(start, end, clampedT);
        Vector2 chord = end - start;
        Vector2 arcNormal = chord.sqrMagnitude > 0.0001f
            ? new Vector2(-chord.y, chord.x).normalized
            : Vector2.up;

        if (arcNormal.y < 0f)
        {
            arcNormal = -arcNormal;
        }

        float verticalOffset = 4f * clampedT * (1f - clampedT) * Mathf.Max(0f, height);
        return linearPosition + arcNormal * verticalOffset;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (resolved || other == null)
        {
            return;
        }

        RedOniBossHealth bossHealth = other.GetComponentInParent<RedOniBossHealth>();

        if (bossHealth != null)
        {
            resolved = true;
            FaithBeanVfx.SpawnImpact(transform.position, 1.15f);
            bossHealth.TakeDamage(damage, transform.position);
            Destroy(gameObject);
            return;
        }

        GhostHealth ghostHealth = other.GetComponentInParent<GhostHealth>();

        if (ghostHealth == null)
        {
            return;
        }

        resolved = true;
        FaithBeanVfx.SpawnImpact(transform.position, 0.85f);
        ghostHealth.TakeDamage(damage, transform.position);
        Destroy(gameObject);
    }
}
