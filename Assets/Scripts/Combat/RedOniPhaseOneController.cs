using System.Collections;
using UnityEngine;

public class RedOniPhaseOneController : MonoBehaviour
{
    public enum AttackLane
    {
        Low,
        Middle,
        High
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform player;

    [Header("Attack rhythm")]
    [SerializeField] private float initialDelay = 1.2f;
    [SerializeField] private Vector2 cooldownRange = new Vector2(1.35f, 1.9f);
    [SerializeField] private float telegraphDuration = 0.75f;
    [SerializeField] private float hitboxDuration = 0.16f;
    [SerializeField] private int damage = 1;

    [Header("Lane hitboxes")]
    [SerializeField] private float lowLaneY = -2f;
    [SerializeField] private float middleLaneY = 1.1f;
    [SerializeField] private float highLaneY = 4.2f;
    [SerializeField] private float arenaCenterX;
    [SerializeField] private float attackWidth = 17f;
    [SerializeField] private float attackHeight = 1.7f;

    [Header("Animation impact timing")]
    [SerializeField] private float highImpactDelay = 0.68f;
    [SerializeField] private float middleImpactDelay = 0.82f;
    [SerializeField] private float lowImpactDelay = 0.72f;

    [Header("Telegraph")]
    [SerializeField] private Color telegraphColor = new Color(1f, 0.16f, 0.08f, 0.22f);
    [SerializeField] private Color impactColor = new Color(1f, 0.72f, 0.18f, 0.42f);
    [SerializeField] private int telegraphSortingOrder = 1;

    private SpriteRenderer[] telegraphRenderers;
    private Coroutine attackRoutine;
    private AttackLane previousLane = AttackLane.Middle;
    private int repeatedLaneCount;
    private int completedAttackCount;
    private bool encounterActive = true;

    private static Sprite runtimeSquareSprite;

    public AttackLane CurrentLane { get; private set; } = AttackLane.Middle;
    public int CompletedAttackCount => completedAttackCount;
    public bool IsAttacking { get; private set; }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        ResolvePlayer();
        EnsureTelegraphs();
    }

    private void OnEnable()
    {
        if (Application.isPlaying && attackRoutine == null)
        {
            attackRoutine = StartCoroutine(AttackLoop());
        }
    }

    private void OnDisable()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        IsAttacking = false;
        HideAllTelegraphs();
    }

    public void ConfigureArena(
        Transform playerTarget,
        float lowY,
        float middleY,
        float highY,
        float centerX,
        float width)
    {
        player = playerTarget;
        lowLaneY = lowY;
        middleLaneY = middleY;
        highLaneY = highY;
        arenaCenterX = centerX;
        attackWidth = Mathf.Max(1f, width);
        EnsureTelegraphs();
        RefreshTelegraphTransforms();
    }

    public void ConfigureAnimator(Animator targetAnimator)
    {
        animator = targetAnimator;
    }

    public void SetEncounterActive(bool active)
    {
        encounterActive = active;

        if (!active)
        {
            IsAttacking = false;
            HideAllTelegraphs();
        }
    }

    private IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, initialDelay));

        while (enabled)
        {
            if (!encounterActive || IsBlockingUiVisible())
            {
                HideAllTelegraphs();
                yield return null;
                continue;
            }

            ResolvePlayer();
            AttackLane lane = ChooseLane();
            yield return PerformAttack(lane);

            float minimum = Mathf.Max(0.1f, Mathf.Min(cooldownRange.x, cooldownRange.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(cooldownRange.x, cooldownRange.y));
            yield return new WaitForSeconds(Random.Range(minimum, maximum));
        }
    }

    private IEnumerator PerformAttack(AttackLane lane)
    {
        IsAttacking = true;
        CurrentLane = lane;
        SpriteRenderer telegraph = GetTelegraph(lane);

        if (telegraph != null)
        {
            telegraph.enabled = true;
        }

        float elapsed = 0f;
        float safeTelegraphDuration = Mathf.Max(0.12f, telegraphDuration);

        while (elapsed < safeTelegraphDuration)
        {
            if (telegraph != null)
            {
                float pulse = 0.72f + Mathf.PingPong(elapsed * 3.6f, 0.28f);
                Color color = telegraphColor;
                color.a *= pulse;
                telegraph.color = color;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        TriggerAnimation(lane);
        yield return new WaitForSeconds(GetImpactDelay(lane));

        if (telegraph != null)
        {
            telegraph.color = impactColor;
        }

        TryDamagePlayer(lane);
        yield return new WaitForSeconds(Mathf.Max(0.04f, hitboxDuration));
        HideAllTelegraphs();

        completedAttackCount++;
        IsAttacking = false;
    }

    private AttackLane ChooseLane()
    {
        AttackLane lane = (AttackLane)Random.Range(0, 3);

        if (repeatedLaneCount >= 1 && lane == previousLane)
        {
            lane = (AttackLane)(((int)lane + Random.Range(1, 3)) % 3);
        }

        if (lane == previousLane)
        {
            repeatedLaneCount++;
        }
        else
        {
            repeatedLaneCount = 0;
        }

        previousLane = lane;
        return lane;
    }

    private void TriggerAnimation(AttackLane lane)
    {
        if (animator == null)
        {
            return;
        }

        switch (lane)
        {
            case AttackLane.High:
                animator.Play("AttackHigh", 0, 0f);
                break;
            case AttackLane.Middle:
                animator.Play("AttackMiddle", 0, 0f);
                break;
            default:
                animator.Play("AttackLow", 0, 0f);
                break;
        }
    }

    private void TryDamagePlayer(AttackLane lane)
    {
        if (player == null)
        {
            return;
        }

        PlayerHealth health = player.GetComponent<PlayerHealth>();

        if (health == null)
        {
            health = player.GetComponentInParent<PlayerHealth>();
        }

        if (health == null || health.IsInvincible)
        {
            return;
        }

        Bounds attackBounds = new Bounds(
            new Vector3(arenaCenterX, GetLaneY(lane), 0f),
            new Vector3(attackWidth, Mathf.Max(0.2f, attackHeight), 2f));
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        bool intersects = playerCollider != null
            ? attackBounds.Intersects(playerCollider.bounds)
            : attackBounds.Contains(player.position);

        if (intersects)
        {
            health.TakeDamage(Mathf.Max(1, damage), transform.position);
        }
    }

    private float GetImpactDelay(AttackLane lane)
    {
        switch (lane)
        {
            case AttackLane.High:
                return Mathf.Max(0f, highImpactDelay);
            case AttackLane.Middle:
                return Mathf.Max(0f, middleImpactDelay);
            default:
                return Mathf.Max(0f, lowImpactDelay);
        }
    }

    private float GetLaneY(AttackLane lane)
    {
        switch (lane)
        {
            case AttackLane.High:
                return highLaneY;
            case AttackLane.Middle:
                return middleLaneY;
            default:
                return lowLaneY;
        }
    }

    private SpriteRenderer GetTelegraph(AttackLane lane)
    {
        EnsureTelegraphs();
        return telegraphRenderers != null ? telegraphRenderers[(int)lane] : null;
    }

    private void EnsureTelegraphs()
    {
        if (telegraphRenderers != null && telegraphRenderers.Length == 3)
        {
            RefreshTelegraphTransforms();
            return;
        }

        telegraphRenderers = new SpriteRenderer[3];

        for (int index = 0; index < telegraphRenderers.Length; index++)
        {
            AttackLane lane = (AttackLane)index;
            Transform existing = transform.Find($"Telegraph_{lane}");
            GameObject telegraphObject = existing != null
                ? existing.gameObject
                : new GameObject($"Telegraph_{lane}");

            telegraphObject.transform.SetParent(transform, true);
            SpriteRenderer renderer = telegraphObject.GetComponent<SpriteRenderer>();

            if (renderer == null)
            {
                renderer = telegraphObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = GetRuntimeSquareSprite();
            renderer.color = telegraphColor;
            renderer.sortingOrder = telegraphSortingOrder;
            renderer.enabled = false;
            telegraphRenderers[index] = renderer;
        }

        RefreshTelegraphTransforms();
    }

    private void RefreshTelegraphTransforms()
    {
        if (telegraphRenderers == null)
        {
            return;
        }

        for (int index = 0; index < telegraphRenderers.Length; index++)
        {
            SpriteRenderer renderer = telegraphRenderers[index];

            if (renderer == null)
            {
                continue;
            }

            renderer.transform.position = new Vector3(arenaCenterX, GetLaneY((AttackLane)index), 0f);
            Vector3 parentScale = transform.lossyScale;
            renderer.transform.localScale = new Vector3(
                attackWidth / Mathf.Max(0.01f, Mathf.Abs(parentScale.x)),
                attackHeight / Mathf.Max(0.01f, Mathf.Abs(parentScale.y)),
                1f);
        }
    }

    private void HideAllTelegraphs()
    {
        if (telegraphRenderers == null)
        {
            return;
        }

        foreach (SpriteRenderer renderer in telegraphRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = false;
            renderer.color = telegraphColor;
        }
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        PlayerController controller = FindAnyObjectByType<PlayerController>();
        player = controller != null ? controller.transform : null;
    }

    private static bool IsBlockingUiVisible()
    {
        return GameManager.Instance != null && GameManager.Instance.IsBlockingUiVisible;
    }

    private static Sprite GetRuntimeSquareSprite()
    {
        if (runtimeSquareSprite != null)
        {
            return runtimeSquareSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "RedOniTelegraphPixel";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        runtimeSquareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        runtimeSquareSprite.name = "RedOniTelegraphSquare";
        return runtimeSquareSprite;
    }
}
