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
    private GhostHealth preferredGhostTarget;
    private RedOniBossHealth pendingBossTarget;
    private float mouseAttackSpeedAtLaunch;

    public void InitializeArc(
        Vector2 start,
        Vector2 target,
        float height,
        float speed,
        int beanDamage,
        float lifetime,
        GhostHealth preferredGhost = null,
        float mouseAttackSpeed = 0f)
    {
        damage = Mathf.Max(1, beanDamage);
        expiresAt = Time.time + Mathf.Max(0.1f, lifetime);
        arcStart = start;
        arcEnd = target;
        arcHeight = Mathf.Max(0f, height);
        travelDuration = Mathf.Max(0.08f, Vector2.Distance(start, target) / Mathf.Max(0.1f, speed));
        travelElapsed = 0f;
        preferredGhostTarget = preferredGhost;
        mouseAttackSpeedAtLaunch = Mathf.Max(0f, mouseAttackSpeed);

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

        if (preferredGhostTarget != null)
        {
            Collider2D targetCollider = preferredGhostTarget.GetComponent<Collider2D>();
            arcEnd = targetCollider != null
                ? targetCollider.bounds.center
                : (Vector2)preferredGhostTarget.transform.position;
        }

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
            if (preferredGhostTarget != null)
            {
                ResolveGhostHit(preferredGhostTarget, 1f);
                return;
            }

            if (pendingBossTarget != null)
            {
                ResolveBossHit(pendingBossTarget);
                return;
            }

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
            // A bean deliberately aimed at a Phase 3 add must pass through the
            // Red Oni's very large trigger instead of being consumed first.
            if (preferredGhostTarget != null)
            {
                return;
            }

            // The Red Oni uses a large trigger that can surround the bean near
            // launch. Remember the hit, but let the visible projectile finish
            // its arc before damage and impact feedback are resolved.
            pendingBossTarget = bossHealth;
            return;
        }

        GhostHealth ghostHealth = other.GetComponentInParent<GhostHealth>();

        if (ghostHealth == null)
        {
            return;
        }

        if (preferredGhostTarget != null && ghostHealth != preferredGhostTarget)
        {
            return;
        }

        ResolveGhostHit(ghostHealth, 0.85f);
    }

    private void ResolveGhostHit(GhostHealth ghostHealth, float effectScale)
    {
        if (resolved || ghostHealth == null)
        {
            return;
        }

        resolved = true;
        FaithBeanVfx.SpawnImpact(ghostHealth.transform.position, effectScale);
        ghostHealth.TakeDamage(damage, transform.position);
        Destroy(gameObject);
    }

    private void ResolveBossHit(RedOniBossHealth bossHealth)
    {
        if (resolved || bossHealth == null)
        {
            return;
        }

        resolved = true;
        FaithBeanVfx.SpawnImpact(transform.position, 1.15f);
        bossHealth.TakeFaithBeanDamage(damage, transform.position, mouseAttackSpeedAtLaunch);
        Destroy(gameObject);
    }
}
