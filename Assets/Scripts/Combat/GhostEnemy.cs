using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class GhostEnemy : MonoBehaviour
{
    private enum EnemyMovementMode
    {
        Hover,
        GroundPatrol,
        LowHoverPatrol
    }

    private enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Hit,
        Dead
    }

    [SerializeField] private float hoverDistance = 0.45f;
    [SerializeField] private float hoverSpeed = 2f;
    [SerializeField] private float bobDistance = 0.18f;
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float destroyDelay = 0.05f;
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float contactDamageCooldown = 0.25f;
    [SerializeField] private bool useStateMachine;
    [SerializeField] private EnemyMovementMode movementMode = EnemyMovementMode.Hover;
    [SerializeField] private float idleDuration = 0.25f;
    [SerializeField] private float patrolSpeed = 0.8f;
    [SerializeField] private float patrolDistance = 1.8f;
    [SerializeField] private float detectRange = 3.2f;
    [SerializeField] private float chaseRange = 3.2f;
    [SerializeField] private float chaseSpeed = 1.15f;
    [SerializeField] private float attackRange = 0.85f;
    [SerializeField] private float attackCooldown = 0.75f;
    [SerializeField] private float attackPauseDuration = 0.14f;
    [SerializeField] private float hitStunDuration = 0.12f;
    [SerializeField] private Transform playerTarget;

    private SpriteRenderer spriteRenderer;
    private Collider2D ghostCollider;
    private Vector3 startPosition;
    private Vector3 patrolCenter;
    private bool fallbackDefeated;
    private bool movementPaused;
    private float nextContactDamageTime;
    private EnemyState currentState = EnemyState.Idle;
    private float stateUntil;
    private float patrolDirection = 1f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ghostCollider = GetComponent<Collider2D>();
        startPosition = transform.position;
        patrolCenter = startPosition;

        if (useStateMachine)
        {
            currentState = EnemyState.Idle;
            stateUntil = Time.time + Mathf.Max(0f, idleDuration);

            if (playerTarget == null)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();

                if (player != null)
                {
                    playerTarget = player.transform;
                }
            }
        }
    }

    private void Update()
    {
        if (fallbackDefeated || movementPaused)
        {
            return;
        }

        if (useStateMachine)
        {
            UpdateStateMachine();
            return;
        }

        ApplyHoverMovement();
    }

    private void ApplyHoverMovement()
    {
        float hoverX = Mathf.Sin(Time.time * hoverSpeed) * hoverDistance;
        float bobY = Mathf.Sin(Time.time * bobSpeed) * bobDistance;
        transform.position = startPosition + new Vector3(hoverX, bobY, 0f);
    }

    private void UpdateStateMachine()
    {
        if (currentState == EnemyState.Hit)
        {
            if (Time.time < stateUntil)
            {
                return;
            }

            currentState = EnemyState.Patrol;
        }

        if (currentState == EnemyState.Attack)
        {
            ApplyModeVerticalMotion();

            if (Time.time < stateUntil)
            {
                return;
            }
        }

        if (currentState == EnemyState.Idle)
        {
            if (Time.time < stateUntil)
            {
                ApplyModeVerticalMotion();
                return;
            }

            currentState = ShouldChasePlayer() ? EnemyState.Chase : EnemyState.Patrol;
        }

        if (IsPlayerInAttackRange())
        {
            TryAttackPlayer();
            return;
        }

        if (ShouldChasePlayer())
        {
            currentState = EnemyState.Chase;
        }
        else if (currentState == EnemyState.Chase)
        {
            currentState = EnemyState.Patrol;
        }

        if (currentState == EnemyState.Chase)
        {
            MoveTowardPlayer();
            return;
        }

        Patrol();
    }

    public void ApplyKnockback(Vector2 direction, float distance)
    {
        if (direction.sqrMagnitude < 0.01f || distance <= 0f)
        {
            return;
        }

        Vector2 safeDirection = direction.normalized;

        if (useStateMachine && movementMode == EnemyMovementMode.GroundPatrol)
        {
            safeDirection.y = 0f;
            safeDirection = safeDirection.sqrMagnitude > 0.01f ? safeDirection.normalized : Vector2.right;
        }

        Vector3 offset = safeDirection * distance;
        startPosition += offset;
        patrolCenter += offset;

        if (useStateMachine)
        {
            EnterHitStun();
        }
    }

    public void PauseMovement()
    {
        movementPaused = true;
        currentState = EnemyState.Dead;
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

        if (fallbackDefeated || movementPaused)
        {
            return;
        }

        fallbackDefeated = true;
        currentState = EnemyState.Dead;

        if (ghostCollider != null)
        {
            ghostCollider.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        GameAudio.PlayGhostVanish();
        Destroy(gameObject, destroyDelay);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (fallbackDefeated || Time.time < nextContactDamageTime)
        {
            return;
        }

        if (currentState == EnemyState.Hit || currentState == EnemyState.Dead)
        {
            return;
        }

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null && !playerHealth.IsInvincible)
        {
            currentState = EnemyState.Attack;
            stateUntil = Time.time + Mathf.Max(0.01f, attackPauseDuration);
            nextContactDamageTime = Time.time + GetSafeAttackCooldown();
            playerHealth.TakeDamage(contactDamage, transform.position);
        }
    }

    private void Patrol()
    {
        currentState = EnemyState.Patrol;
        Vector3 position = transform.position;
        float safePatrolDistance = Mathf.Max(0.05f, patrolDistance);
        float leftBound = patrolCenter.x - safePatrolDistance;
        float rightBound = patrolCenter.x + safePatrolDistance;

        if (position.x <= leftBound)
        {
            patrolDirection = 1f;
        }
        else if (position.x >= rightBound)
        {
            patrolDirection = -1f;
        }

        position.x += patrolDirection * Mathf.Max(0f, patrolSpeed) * Time.deltaTime;
        position.x = Mathf.Clamp(position.x, leftBound, rightBound);
        transform.position = ApplyVerticalMotion(position);
    }

    private void MoveTowardPlayer()
    {
        if (playerTarget == null)
        {
            Patrol();
            return;
        }

        Vector3 position = transform.position;
        float direction = Mathf.Sign(playerTarget.position.x - position.x);

        if (Mathf.Approximately(direction, 0f))
        {
            direction = patrolDirection;
        }

        patrolDirection = direction;
        position.x += direction * Mathf.Max(0f, chaseSpeed) * Time.deltaTime;
        float leashDistance = Mathf.Max(Mathf.Max(0.05f, patrolDistance), Mathf.Max(0.05f, chaseRange));
        position.x = Mathf.Clamp(position.x, patrolCenter.x - leashDistance, patrolCenter.x + leashDistance);
        transform.position = ApplyVerticalMotion(position);
    }

    private bool ShouldChasePlayer()
    {
        if (playerTarget == null || GetSafeDetectRange() <= 0f)
        {
            return false;
        }

        float safeDetectRange = GetSafeDetectRange();
        return Mathf.Abs(playerTarget.position.x - transform.position.x) <= safeDetectRange
            && Mathf.Abs(playerTarget.position.y - transform.position.y) <= Mathf.Max(1.25f, safeDetectRange * 0.65f);
    }

    private bool IsPlayerInAttackRange()
    {
        if (playerTarget == null)
        {
            return false;
        }

        float safeAttackRange = Mathf.Max(0.05f, attackRange);
        return Vector2.Distance(transform.position, playerTarget.position) <= safeAttackRange;
    }

    private void TryAttackPlayer()
    {
        currentState = EnemyState.Attack;
        stateUntil = Time.time + Mathf.Max(0.01f, attackPauseDuration);
        ApplyModeVerticalMotion();

        if (Time.time < nextContactDamageTime || playerTarget == null)
        {
            return;
        }

        PlayerHealth playerHealth = playerTarget.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null || playerHealth.IsInvincible)
        {
            return;
        }

        nextContactDamageTime = Time.time + GetSafeAttackCooldown();
        playerHealth.TakeDamage(contactDamage, transform.position);
    }

    private Vector3 ApplyVerticalMotion(Vector3 position)
    {
        if (movementMode == EnemyMovementMode.GroundPatrol)
        {
            position.y = patrolCenter.y;
            return position;
        }

        float bobY = Mathf.Sin(Time.time * bobSpeed) * bobDistance;
        position.y = patrolCenter.y + bobY;
        return position;
    }

    private void ApplyModeVerticalMotion()
    {
        transform.position = ApplyVerticalMotion(transform.position);
    }

    private void EnterHitStun()
    {
        currentState = EnemyState.Hit;
        stateUntil = Time.time + Mathf.Max(0.01f, hitStunDuration);
    }

    private float GetSafeDetectRange()
    {
        return detectRange > 0f ? detectRange : Mathf.Max(0f, chaseRange);
    }

    private float GetSafeAttackCooldown()
    {
        return Mathf.Max(0.01f, attackCooldown > 0f ? attackCooldown : contactDamageCooldown);
    }
}
