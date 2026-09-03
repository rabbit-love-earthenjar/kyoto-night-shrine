using System.Collections;
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
    [SerializeField, Min(0f)] private float groundVisualInset;
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
    [SerializeField] private float flyingDiveSpeed = 3.8f;
    [SerializeField] private float flyingReturnSpeed = 1.8f;
    [SerializeField] private float flyingAttackHeightOffset = 0.35f;
    [SerializeField] private Color chaseAlertColor = new Color(1f, 0.86f, 0.72f, 1f);
    [SerializeField] private Color attackWarningColor = new Color(1f, 0.72f, 0.9f, 1f);
    [SerializeField] private float hitStunDuration = 0.12f;
    [SerializeField] private float knockbackSlideDuration = 0.08f;
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
    private Coroutine knockbackRoutine;
    [SerializeField] private bool useRouteMovementBounds;
    [SerializeField] private float routeMinX;
    [SerializeField] private float routeMaxX;
    [SerializeField] private float routeSurfaceY;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ghostCollider = GetComponent<Collider2D>();
        baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        startPosition = transform.position;
        patrolCenter = startPosition;
        lastKnownPlayerPosition = startPosition;

        if (movementMode == EnemyMovementMode.GroundPatrol)
        {
            SnapGroundPatrolToSupportingSurface();
        }

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

    private void SnapGroundPatrolToSupportingSurface()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Vector2 origin = new Vector2(transform.position.x, transform.position.y + 1.5f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 5f);
        float highestSurfaceY = float.NegativeInfinity;

        foreach (RaycastHit2D hit in hits)
        {
            Collider2D candidate = hit.collider;

            if (candidate == null
                || candidate == ghostCollider
                || candidate.isTrigger
                || candidate.transform.IsChildOf(transform)
                || transform.IsChildOf(candidate.transform))
            {
                continue;
            }

            float surfaceY = candidate.bounds.max.y;
            if (surfaceY > transform.position.y + 0.5f || surfaceY <= highestSurfaceY)
            {
                continue;
            }

            highestSurfaceY = surfaceY;
        }

        if (float.IsNegativeInfinity(highestSurfaceY))
        {
            return;
        }

        float rootToVisualBottom = transform.position.y - spriteRenderer.bounds.min.y;
        Vector3 groundedPosition = transform.position;
        groundedPosition.y = highestSurfaceY
            + Mathf.Max(0.05f, rootToVisualBottom)
            + 0.02f
            - Mathf.Max(0f, groundVisualInset);
        transform.position = groundedPosition;
        startPosition = groundedPosition;
        patrolCenter = groundedPosition;
        lastKnownPlayerPosition = groundedPosition;
    }

    public void ConfigureRouteBehavior(bool flying, float minimumX, float maximumX, float anchorY, float surfaceY)
    {
        useStateMachine = true;
        movementMode = flying ? EnemyMovementMode.Hover : EnemyMovementMode.GroundPatrol;
        routeMinX = Mathf.Min(minimumX, maximumX);
        routeMaxX = Mathf.Max(minimumX, maximumX);
        useRouteMovementBounds = routeMaxX - routeMinX > 0.1f;
        routeSurfaceY = surfaceY;

        Vector3 anchoredPosition = transform.position;
        anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, routeMinX, routeMaxX);
        anchoredPosition.y = anchorY;
        transform.position = anchoredPosition;
        startPosition = anchoredPosition;
        patrolCenter = anchoredPosition;
        lastKnownPlayerPosition = anchoredPosition;

        detectRange = flying ? 5.6f : 5.2f;
        chaseRange = detectRange;
        chaseSpeed = flying ? 2f : 1.65f;
        patrolSpeed = flying ? 0.9f : 0.85f;
        patrolDistance = Mathf.Min(Mathf.Max(0.8f, (routeMaxX - routeMinX) * 0.32f), 3.2f);
        chaseMemoryDuration = flying ? 1.15f : 0.9f;
        chaseLeashBuffer = 0f;
        attackRange = flying ? 1.05f : 0.92f;
        attackCooldown = flying ? 1.05f : 0.85f;
        attackPauseDuration = flying ? 0.18f : 0.14f;
        attackLungeSpeed = flying ? 4.4f : 2.5f;
        attackLungeDuration = flying ? 0.18f : 0.1f;
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
        SlideToKnockbackPosition(transform.position + offset);

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

        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
            knockbackRoutine = null;
        }
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
        float leftBound = GetLeftMovementBound(safePatrolDistance);
        float rightBound = GetRightMovementBound(safePatrolDistance);

        if (position.x <= leftBound)
        {
            patrolDirection = 1f;
        }
        else if (position.x >= rightBound)
        {
            patrolDirection = -1f;
        }

        Vector3 previousPosition = position;
        position.x += patrolDirection * Mathf.Max(0f, patrolSpeed) * Time.deltaTime;
        position.x = Mathf.Clamp(position.x, leftBound, rightBound);
        position = ConstrainAgainstBreakableObstacle(previousPosition, position, out bool blockedByCrate);

        if (blockedByCrate)
        {
            patrolDirection *= -1f;
        }

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
        Vector3 previousPosition = position;
        position.x += direction * speed * Time.deltaTime;
        float leashDistance = GetSafeChaseLeashDistance();
        position.x = Mathf.Clamp(position.x, GetLeftMovementBound(leashDistance), GetRightMovementBound(leashDistance));
        position = ConstrainAgainstBreakableObstacle(previousPosition, position, out _);

        if (movementMode != EnemyMovementMode.GroundPatrol)
        {
            float diveTargetY = targetPosition.y + Mathf.Max(0f, flyingAttackHeightOffset);
            float minimumDiveY = patrolCenter.y - Mathf.Max(1.25f, GetSafeDetectRange() * 0.55f);
            minimumDiveY = Mathf.Max(minimumDiveY, GetMinimumFlyingCenterY());
            float maximumDiveY = patrolCenter.y + Mathf.Max(0.35f, bobDistance);
            diveTargetY = Mathf.Clamp(diveTargetY, minimumDiveY, maximumDiveY);
            position.y = Mathf.MoveTowards(
                position.y,
                diveTargetY,
                Mathf.Max(0.1f, flyingDiveSpeed) * Time.deltaTime);
        }
        FaceDirection(direction);
        ShowChaseAlert();
        transform.position = movementMode == EnemyMovementMode.GroundPatrol
            ? ApplyVerticalMotion(position)
            : position;
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
        attackDirection = GetAttackDirection();
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

        if (currentState == EnemyState.Chase || currentState == EnemyState.Attack)
        {
            return position;
        }

        float bobY = Mathf.Sin(Time.time * bobSpeed) * bobDistance;
        float hoverTargetY = patrolCenter.y + bobY;
        position.y = Mathf.MoveTowards(
            position.y,
            hoverTargetY,
            Mathf.Max(0.1f, flyingReturnSpeed) * Time.deltaTime);
        position.y = Mathf.Max(position.y, GetMinimumFlyingCenterY());
        return position;
    }

    private float GetMinimumFlyingCenterY()
    {
        float visualHalfHeight = spriteRenderer != null ? spriteRenderer.bounds.extents.y : 0.45f;
        return routeSurfaceY + Mathf.Max(0.2f, visualHalfHeight) + 0.08f;
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

        Vector3 previousPosition = transform.position;
        Vector3 position = previousPosition;
        position += (Vector3)(attackDirection * Mathf.Max(0f, attackLungeSpeed) * Time.deltaTime);
        float leashDistance = GetSafeChaseLeashDistance();
        position.x = Mathf.Clamp(position.x, GetLeftMovementBound(leashDistance), GetRightMovementBound(leashDistance));
        position = ConstrainAgainstBreakableObstacle(previousPosition, position, out _);
        FaceDirection(attackDirection.x);
        transform.position = movementMode == EnemyMovementMode.GroundPatrol
            ? ApplyVerticalMotion(position)
            : position;
    }

    private Vector3 ConstrainAgainstBreakableObstacle(Vector3 previousPosition, Vector3 proposedPosition, out bool blocked)
    {
        blocked = false;

        if (movementMode != EnemyMovementMode.GroundPatrol || ghostCollider == null)
        {
            return proposedPosition;
        }

        Vector2 movement = proposedPosition - previousPosition;

        if (Mathf.Abs(movement.x) < 0.0001f)
        {
            return proposedPosition;
        }

        Bounds bounds = ghostCollider.bounds;
        Vector2 testCenter = (Vector2)bounds.center + movement;
        Vector2 testSize = new Vector2(
            Mathf.Max(0.1f, bounds.size.x * 0.92f),
            Mathf.Max(0.1f, bounds.size.y * 0.86f));
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(testCenter, testSize, 0f);

        foreach (Collider2D overlap in overlaps)
        {
            if (overlap == null || overlap == ghostCollider || overlap.isTrigger)
            {
                continue;
            }

            if (overlap.GetComponentInParent<BreakableBlock>() == null)
            {
                continue;
            }

            blocked = true;
            proposedPosition.x = previousPosition.x;
            return proposedPosition;
        }

        return proposedPosition;
    }

    private Vector2 GetAttackDirection()
    {
        if (playerTarget == null)
        {
            return new Vector2(patrolDirection >= 0f ? 1f : -1f, 0f);
        }

        if (movementMode != EnemyMovementMode.GroundPatrol)
        {
            Vector2 directionToPlayer = (Vector2)(playerTarget.position - transform.position);
            return directionToPlayer.sqrMagnitude > 0.01f
                ? directionToPlayer.normalized
                : Vector2.down;
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

    private float GetLeftMovementBound(float fallbackDistance)
    {
        return useRouteMovementBounds ? routeMinX : patrolCenter.x - fallbackDistance;
    }

    private float GetRightMovementBound(float fallbackDistance)
    {
        return useRouteMovementBounds ? routeMaxX : patrolCenter.x + fallbackDistance;
    }

    private void FaceDirection(float direction)
    {
        if (spriteRenderer == null || Mathf.Abs(direction) < 0.01f)
        {
            return;
        }

        spriteRenderer.flipX = direction < 0f;
    }

    private void SlideToKnockbackPosition(Vector3 targetPosition)
    {
        if (!isActiveAndEnabled || knockbackSlideDuration <= 0f)
        {
            transform.position = targetPosition;
            return;
        }

        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
        }

        knockbackRoutine = StartCoroutine(KnockbackSlideRoutine(targetPosition));
    }

    private IEnumerator KnockbackSlideRoutine(Vector3 targetPosition)
    {
        Vector3 start = transform.position;
        float duration = Mathf.Max(0.01f, knockbackSlideDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            transform.position = Vector3.Lerp(start, targetPosition, eased);
            yield return null;
        }

        transform.position = targetPosition;
        knockbackRoutine = null;
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
