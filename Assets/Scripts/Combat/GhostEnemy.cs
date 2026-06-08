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
    [SerializeField] private float chaseMemoryDuration = 0.65f;
    [SerializeField] private float chaseLeashBuffer = 1.2f;
    [SerializeField] private float closePressureRange = 1.35f;
    [SerializeField] private float closePressureSpeedMultiplier = 1.35f;
    [SerializeField] private float attackRange = 0.85f;
    [SerializeField] private float attackCooldown = 0.75f;
    [SerializeField] private float attackPauseDuration = 0.14f;
    [SerializeField] private float attackCommitRangeBuffer = 0.18f;
    [SerializeField] private float attackLungeSpeed = 2.2f;
    [SerializeField] private float attackLungeDuration = 0.08f;
    [SerializeField] private Color chaseAlertColor = new Color(1f, 0.86f, 0.72f, 1f);
    [SerializeField] private Color attackWarningColor = new Color(1f, 0.72f, 0.9f, 1f);
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
    private Color baseColor;
    private bool attackDamagePending;
    private bool attackWarningActive;
    private bool chaseAlertActive;
    private float hitPauseUntil;
    private float lastPlayerSeenTime = -999f;
    private float attackLungeUntil;
    private Vector3 lastKnownPlayerPosition;
    private Vector2 attackDirection = Vector2.right;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ghostCollider = GetComponent<Collider2D>();
        baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        startPosition = transform.position;
        patrolCenter = startPosition;
        lastKnownPlayerPosition = startPosition;

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

        if (Time.time < hitPauseUntil)
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
        RefreshPlayerSighting();

        if (currentState == EnemyState.Hit)
        {
            ClearAttackWarning();
            ClearChaseAlert();

            if (Time.time < stateUntil)
            {
                return;
            }

            currentState = EnemyState.Patrol;
        }

        if (currentState == EnemyState.Attack)
        {
            ClearChaseAlert();
            ApplyAttackLungeMotion();

            if (Time.time < stateUntil)
            {
                return;
            }

            ResolvePendingAttack();
            ClearAttackWarning();
            currentState = ShouldChasePlayer() ? EnemyState.Chase : EnemyState.Patrol;
            return;
        }

        if (currentState == EnemyState.Idle)
        {
            ClearChaseAlert();

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

    public void ApplyHitStun(float duration)
    {
        if (fallbackDefeated || movementPaused || duration <= 0f)
        {
            return;
        }

        if (useStateMachine)
        {
            EnterHitStun(duration);
            return;
        }

        hitPauseUntil = Mathf.Max(hitPauseUntil, Time.time + duration);
    }

    public void PauseMovement()
    {
        movementPaused = true;
        currentState = EnemyState.Dead;
        ClearAttackWarning();
        ClearChaseAlert();
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
        ClearAttackWarning();
        ClearChaseAlert();

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

        if (useStateMachine)
        {
            PlayerHealth contactedPlayerHealth = other.GetComponentInParent<PlayerHealth>();

            if (contactedPlayerHealth != null && !contactedPlayerHealth.IsInvincible)
            {
                TryAttackPlayer();
            }

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
        ClearChaseAlert();
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
        FaceDirection(patrolDirection);
        transform.position = ApplyVerticalMotion(position);
    }

    private void MoveTowardPlayer()
    {
        if (playerTarget == null || !ShouldChasePlayer())
        {
            Patrol();
            return;
        }

        Vector3 position = transform.position;
        Vector3 targetPosition = lastKnownPlayerPosition;
        float direction = Mathf.Sign(targetPosition.x - position.x);

        if (Mathf.Approximately(direction, 0f))
        {
            direction = patrolDirection;
        }

        patrolDirection = direction;
        float speed = GetPressureChaseSpeed(position, targetPosition);
        position.x += direction * speed * Time.deltaTime;
        float leashDistance = GetSafeChaseLeashDistance();
        position.x = Mathf.Clamp(position.x, patrolCenter.x - leashDistance, patrolCenter.x + leashDistance);
        FaceDirection(direction);
        ShowChaseAlert();
        transform.position = ApplyVerticalMotion(position);
    }

    private bool ShouldChasePlayer()
    {
        if (playerTarget == null || GetSafeDetectRange() <= 0f)
        {
            return false;
        }

        if (CanSeePlayer())
        {
            return true;
        }

        if (currentState != EnemyState.Chase && currentState != EnemyState.Attack)
        {
            return false;
        }

        float safeMemoryDuration = Mathf.Max(0f, chaseMemoryDuration);
        bool remembersPlayer = Time.time <= lastPlayerSeenTime + safeMemoryDuration;
        bool targetStillLeashed = Mathf.Abs(lastKnownPlayerPosition.x - patrolCenter.x) <= GetSafeChaseLeashDistance();

        return remembersPlayer && targetStillLeashed;
    }

    private bool CanSeePlayer()
    {
        if (playerTarget == null)
        {
            return false;
        }

        float safeDetectRange = GetSafeDetectRange();
        return Mathf.Abs(playerTarget.position.x - transform.position.x) <= safeDetectRange
            && Mathf.Abs(playerTarget.position.y - transform.position.y) <= Mathf.Max(1.25f, safeDetectRange * 0.65f);
    }

    private void RefreshPlayerSighting()
    {
        if (!CanSeePlayer())
        {
            return;
        }

        lastKnownPlayerPosition = playerTarget.position;
        lastPlayerSeenTime = Time.time;
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
        if (currentState == EnemyState.Attack)
        {
            ApplyModeVerticalMotion();
            return;
        }

        if (Time.time < nextContactDamageTime || playerTarget == null)
        {
            ApplyModeVerticalMotion();
            return;
        }

        PlayerHealth playerHealth = playerTarget.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null || playerHealth.IsInvincible)
        {
            ApplyModeVerticalMotion();
            return;
        }

        currentState = EnemyState.Attack;
        stateUntil = Time.time + Mathf.Max(0.01f, attackPauseDuration);
        attackDamagePending = true;
        lastKnownPlayerPosition = playerTarget.position;
        lastPlayerSeenTime = Time.time;
        attackDirection = GetHorizontalAttackDirection();
        attackLungeUntil = Time.time + Mathf.Max(0f, attackLungeDuration);
        nextContactDamageTime = Time.time + GetSafeAttackCooldown();
        ShowAttackWarning();
        ApplyModeVerticalMotion();
    }

    private void ResolvePendingAttack()
    {
        if (!attackDamagePending || playerTarget == null)
        {
            return;
        }

        attackDamagePending = false;
        PlayerHealth playerHealth = playerTarget.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null || playerHealth.IsInvincible)
        {
            return;
        }

        if (!IsPlayerInAttackCommitRange())
        {
            return;
        }

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
        EnterHitStun(hitStunDuration);
    }

    private void EnterHitStun(float duration)
    {
        attackDamagePending = false;
        ClearAttackWarning();
        ClearChaseAlert();
        currentState = EnemyState.Hit;
        stateUntil = Time.time + Mathf.Max(0.01f, duration);
    }

    private bool IsPlayerInAttackCommitRange()
    {
        if (playerTarget == null)
        {
            return false;
        }

        float safeAttackRange = Mathf.Max(0.05f, attackRange);
        float safeBuffer = Mathf.Max(0f, attackCommitRangeBuffer);
        return Vector2.Distance(transform.position, playerTarget.position) <= safeAttackRange + safeBuffer;
    }

    private void ShowAttackWarning()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        ClearChaseAlert();
        spriteRenderer.color = attackWarningColor;
        attackWarningActive = true;
    }

    private void ClearAttackWarning()
    {
        attackDamagePending = false;

        if (!attackWarningActive || spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = baseColor;
        attackWarningActive = false;
    }

    private void ShowChaseAlert()
    {
        if (spriteRenderer == null || attackWarningActive)
        {
            return;
        }

        spriteRenderer.color = chaseAlertColor;
        chaseAlertActive = true;
    }

    private void ClearChaseAlert()
    {
        if (!chaseAlertActive || spriteRenderer == null || attackWarningActive)
        {
            return;
        }

        spriteRenderer.color = baseColor;
        chaseAlertActive = false;
    }

    private void ApplyAttackLungeMotion()
    {
        if (Time.time >= attackLungeUntil || attackLungeSpeed <= 0f)
        {
            ApplyModeVerticalMotion();
            return;
        }

        Vector3 position = transform.position;
        position.x += attackDirection.x * Mathf.Max(0f, attackLungeSpeed) * Time.deltaTime;
        float leashDistance = GetSafeChaseLeashDistance();
        position.x = Mathf.Clamp(position.x, patrolCenter.x - leashDistance, patrolCenter.x + leashDistance);
        FaceDirection(attackDirection.x);
        transform.position = ApplyVerticalMotion(position);
    }

    private Vector2 GetHorizontalAttackDirection()
    {
        if (playerTarget == null)
        {
            return new Vector2(patrolDirection >= 0f ? 1f : -1f, 0f);
        }

        float direction = Mathf.Sign(playerTarget.position.x - transform.position.x);

        if (Mathf.Approximately(direction, 0f))
        {
            direction = patrolDirection;
        }

        return new Vector2(direction >= 0f ? 1f : -1f, 0f);
    }

    private float GetPressureChaseSpeed(Vector3 position, Vector3 targetPosition)
    {
        float speed = Mathf.Max(0f, chaseSpeed);
        float pressureRange = Mathf.Max(0f, closePressureRange);

        if (pressureRange > 0f && Mathf.Abs(targetPosition.x - position.x) <= pressureRange)
        {
            speed *= Mathf.Max(1f, closePressureSpeedMultiplier);
        }

        return speed;
    }

    private float GetSafeChaseLeashDistance()
    {
        float baseLeash = Mathf.Max(Mathf.Max(0.05f, patrolDistance), Mathf.Max(0.05f, chaseRange));
        return baseLeash + Mathf.Max(0f, chaseLeashBuffer);
    }

    private void FaceDirection(float direction)
    {
        if (spriteRenderer == null || Mathf.Abs(direction) < 0.01f)
        {
            return;
        }

        spriteRenderer.flipX = direction < 0f;
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
