using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class RangedRunnerEnemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 1.55f;
    [SerializeField] private float combatMoveSpeed = 2.05f;
    [SerializeField] private float detectRange = 7f;
    [SerializeField] private float preferredRange = 4.2f;

    [Header("Ranged attack")]
    [SerializeField] private float attackCooldown = 1.8f;
    [SerializeField] private float windupDuration = 0.42f;
    [SerializeField] private float projectileSpeed = 5.6f;
    [SerializeField] private float projectileLifetime = 2.6f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private Color warningColor = new Color(1f, 0.72f, 0.9f, 1f);
    [SerializeField] private Sprite projectileSprite;

    private SpriteRenderer spriteRenderer;
    private Collider2D bodyCollider;
    private Transform playerTarget;
    private Color baseColor;
    [SerializeField] private float minimumX;
    [SerializeField] private float maximumX;
    [SerializeField] private float surfaceY;
    private float moveDirection = 1f;
    private float nextAttackTime;
    private float hitStunUntil;
    private bool attacking;
    private bool movementPaused;

    public int ShotsFired { get; private set; }
    public float PatrolWidth => maximumX - minimumX;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
        baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        FindPlayer();
    }

    private void Update()
    {
        if (movementPaused || Time.time < hitStunUntil)
        {
            return;
        }

        if (playerTarget == null)
        {
            FindPlayer();
        }

        if (attacking)
        {
            return;
        }

        if (CanEngagePlayer())
        {
            UpdateCombatMovement();

            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(AttackRoutine());
            }

            return;
        }

        Patrol();
    }

    public void ConfigureRoute(float routeMinimumX, float routeMaximumX, float routeSurfaceY, Sprite shotSprite)
    {
        minimumX = Mathf.Min(routeMinimumX, routeMaximumX);
        maximumX = Mathf.Max(routeMinimumX, routeMaximumX);
        // The scene builder aligns the sprite's feet before configuration. Preserve that
        // root height so movement does not pull the visual down into the platform.
        surfaceY = transform.position.y;
        projectileSprite = shotSprite;
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, minimumX, maximumX),
            surfaceY,
            transform.position.z);
        nextAttackTime = Time.time + 0.65f;
    }

    public void PauseMovement()
    {
        movementPaused = true;
        StopAllCoroutines();
        attacking = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
    }

    public void ApplyHitStun(float duration)
    {
        hitStunUntil = Mathf.Max(hitStunUntil, Time.time + Mathf.Max(0f, duration));
    }

    public void ApplyKnockback(Vector2 direction, float distance)
    {
        float targetX = Mathf.Clamp(transform.position.x + Mathf.Sign(direction.x) * Mathf.Max(0f, distance), minimumX, maximumX);
        transform.position = new Vector3(targetX, surfaceY, transform.position.z);
    }

    private void Patrol()
    {
        if (transform.position.x <= minimumX + 0.05f)
        {
            moveDirection = 1f;
        }
        else if (transform.position.x >= maximumX - 0.05f)
        {
            moveDirection = -1f;
        }

        Move(moveDirection, patrolSpeed);
    }

    private void UpdateCombatMovement()
    {
        float deltaX = playerTarget.position.x - transform.position.x;
        float distance = Mathf.Abs(deltaX);

        if (distance < preferredRange - 0.55f)
        {
            moveDirection = -Mathf.Sign(deltaX);
        }
        else if (distance > preferredRange + 0.85f)
        {
            moveDirection = Mathf.Sign(deltaX);
        }

        if (Mathf.Abs(moveDirection) < 0.01f)
        {
            moveDirection = 1f;
        }

        Move(moveDirection, combatMoveSpeed);
    }

    private void Move(float direction, float speed)
    {
        float previousX = transform.position.x;
        float targetX = Mathf.Clamp(previousX + direction * Mathf.Max(0f, speed) * Time.deltaTime, minimumX, maximumX);

        if (WouldOverlapBreakableCrate(targetX))
        {
            targetX = previousX;
            moveDirection *= -1f;
        }

        transform.position = new Vector3(targetX, surfaceY, transform.position.z);

        if (spriteRenderer != null && Mathf.Abs(direction) > 0.01f)
        {
            spriteRenderer.flipX = direction < 0f;
        }
    }

    private bool WouldOverlapBreakableCrate(float targetX)
    {
        if (bodyCollider == null)
        {
            return false;
        }

        Bounds bounds = bodyCollider.bounds;
        Vector2 center = new Vector2(targetX + (bounds.center.x - transform.position.x), bounds.center.y);
        Vector2 size = new Vector2(
            Mathf.Max(0.1f, bounds.size.x * 0.92f),
            Mathf.Max(0.1f, bounds.size.y * 0.86f));

        foreach (Collider2D overlap in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            if (overlap != null
                && overlap != bodyCollider
                && !overlap.isTrigger
                && overlap.GetComponentInParent<BreakableBlock>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator AttackRoutine()
    {
        attacking = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = warningColor;
        }

        yield return new WaitForSeconds(Mathf.Max(0.05f, windupDuration));

        if (!movementPaused && playerTarget != null)
        {
            FireProjectile();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }

        nextAttackTime = Time.time + Mathf.Max(0.2f, attackCooldown);
        attacking = false;
    }

    private void FireProjectile()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.35f;
        Vector2 target = (Vector2)playerTarget.position + Vector2.up * 0.45f;
        Vector2 direction = (target - origin).normalized;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = spriteRenderer != null && spriteRenderer.flipX ? Vector2.left : Vector2.right;
        }

        GameObject projectileObject = new GameObject($"{name}_SpiritShot");
        projectileObject.transform.position = origin + direction * 0.55f;
        projectileObject.transform.localScale = Vector3.one * 0.28f;

        SpriteRenderer shotRenderer = projectileObject.AddComponent<SpriteRenderer>();
        shotRenderer.sprite = projectileSprite != null ? projectileSprite : spriteRenderer.sprite;
        shotRenderer.color = new Color(0.62f, 0.9f, 1f, 1f);
        shotRenderer.sortingOrder = 7;

        CircleCollider2D shotCollider = projectileObject.AddComponent<CircleCollider2D>();
        shotCollider.isTrigger = true;
        shotCollider.radius = 0.45f;

        Rigidbody2D shotBody = projectileObject.AddComponent<Rigidbody2D>();
        shotBody.gravityScale = 0f;
        shotBody.freezeRotation = true;
        shotBody.linearVelocity = direction * Mathf.Max(0.1f, projectileSpeed);

        RangedSpiritProjectile projectile = projectileObject.AddComponent<RangedSpiritProjectile>();
        projectile.Initialize(gameObject, Mathf.Max(1, projectileDamage), Mathf.Max(0.2f, projectileLifetime));
        ShotsFired++;
    }

    private bool CanEngagePlayer()
    {
        if (playerTarget == null)
        {
            return false;
        }

        Vector2 delta = playerTarget.position - transform.position;
        return Mathf.Abs(delta.y) <= 3.2f && delta.sqrMagnitude <= detectRange * detectRange;
    }

    private void FindPlayer()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        playerTarget = player != null ? player.transform : null;
    }
}

public class RangedSpiritProjectile : MonoBehaviour
{
    private GameObject owner;
    private int damage;

    public void Initialize(GameObject projectileOwner, int projectileDamage, float lifetime)
    {
        owner = projectileOwner;
        damage = Mathf.Max(1, projectileDamage);
        Destroy(gameObject, Mathf.Max(0.2f, lifetime));
    }

    private void Update()
    {
        if (owner == null)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || owner == null)
        {
            return;
        }

        if (other.gameObject == owner || other.transform.IsChildOf(owner.transform))
        {
            return;
        }

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, transform.position);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger && other.GetComponentInParent<GhostHealth>() == null)
        {
            Destroy(gameObject);
        }
    }
}
