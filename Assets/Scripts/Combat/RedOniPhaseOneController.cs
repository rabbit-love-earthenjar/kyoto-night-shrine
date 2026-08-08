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

    [Header("Visual lane alignment")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float highVisualOffsetY = -1.45f;
    [SerializeField] private float middleVisualOffsetY = -3.2f;
    [SerializeField] private float lowVisualOffsetY = -3.2f;
    [SerializeField, Min(0.01f)] private float visualMoveDuration = 0.18f;
    [SerializeField, Min(0.01f)] private float visualReturnDuration = 0.16f;
    [SerializeField, Min(0f)] private float middleGroundPauseDuration = 0.18f;

    [Header("Directional attack smoke")]
    [SerializeField] private ParticleSystem highJumpCloud;
    [SerializeField] private Vector3 highJumpCloudOffset = new Vector3(0f, 0.12f, -0.1f);
    [SerializeField] private ParticleSystem lowAttackSmoke;
    [SerializeField] private Vector3 lowAttackSmokeOffset = new Vector3(0f, 1.15f, -0.1f);

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
    private Vector3 visualRestLocalPosition;
    private bool visualRestPositionCaptured;
    private bool highJumpCloudActive;
    private bool lowAttackSmokeActive;

    private static Sprite runtimeSquareSprite;
    private static Material runtimeSmokeMaterial;

    public AttackLane CurrentLane { get; private set; } = AttackLane.Middle;
    public int CompletedAttackCount => completedAttackCount;
    public bool IsAttacking { get; private set; }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        ResolveVisualRoot();
        EnsureHighJumpCloud();
        EnsureLowAttackSmoke();
        ResolvePlayer();
        EnsureTelegraphs();
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            PlaceVisualOnGround();

            if (attackRoutine == null)
            {
                attackRoutine = StartCoroutine(AttackLoop());
            }
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
        StopHighJumpCloud(true);
        StopLowAttackSmoke(true);
        RestoreVisualPosition();
    }

    private void LateUpdate()
    {
        if (highJumpCloudActive && highJumpCloud != null && visualRoot != null)
        {
            highJumpCloud.transform.position = visualRoot.position + highJumpCloudOffset;
        }

        if (lowAttackSmokeActive && lowAttackSmoke != null && visualRoot != null)
        {
            lowAttackSmoke.transform.position = visualRoot.position + lowAttackSmokeOffset;
        }
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
        ResolveVisualRoot();
    }

    public void SetEncounterActive(bool active)
    {
        encounterActive = active;

        if (!active)
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            IsAttacking = false;
            HideAllTelegraphs();
            StopHighJumpCloud(true);
            StopLowAttackSmoke(true);
            PlaceVisualOnGround();
            return;
        }

        if (Application.isPlaying && isActiveAndEnabled && attackRoutine == null)
        {
            attackRoutine = StartCoroutine(AttackLoop());
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

        if (lane == AttackLane.High)
        {
            PlayHighJumpCloud();
            StopLowAttackSmoke(true);
        }
        else if (lane == AttackLane.Low)
        {
            StopHighJumpCloud(true);
            PlayLowAttackSmoke();
        }
        else
        {
            StopHighJumpCloud(true);
            StopLowAttackSmoke(true);
        }

        if (telegraph != null)
        {
            telegraph.enabled = true;
        }

        float elapsed = 0f;
        float safeTelegraphDuration = Mathf.Max(0.12f, telegraphDuration);
        Vector3 visualStartPosition = visualRoot != null
            ? visualRoot.localPosition
            : Vector3.zero;
        Vector3 visualAttackPosition = GetVisualAttackPosition(lane);

        while (elapsed < safeTelegraphDuration)
        {
            if (telegraph != null)
            {
                float pulse = 0.72f + Mathf.PingPong(elapsed * 3.6f, 0.28f);
                Color color = telegraphColor;
                color.a *= pulse;
                telegraph.color = color;
            }

            MoveVisualDuringTelegraph(
                visualStartPosition,
                visualAttackPosition,
                elapsed);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (visualRoot != null)
        {
            visualRoot.localPosition = visualAttackPosition;
        }

        if (lane == AttackLane.Middle && middleGroundPauseDuration > 0f)
        {
            yield return new WaitForSeconds(middleGroundPauseDuration);
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
        StopHighJumpCloud(false);
        StopLowAttackSmoke(false);
        yield return MoveVisualTo(GetVisualAttackPosition(AttackLane.Middle), visualReturnDuration);

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

    private float GetVisualOffsetY(AttackLane lane)
    {
        switch (lane)
        {
            case AttackLane.High:
                return highVisualOffsetY;
            case AttackLane.Middle:
                return middleVisualOffsetY;
            default:
                return lowVisualOffsetY;
        }
    }

    private Vector3 GetVisualAttackPosition(AttackLane lane)
    {
        ResolveVisualRoot();
        return visualRestLocalPosition + Vector3.up * GetVisualOffsetY(lane);
    }

    private void MoveVisualDuringTelegraph(
        Vector3 startPosition,
        Vector3 targetPosition,
        float elapsed)
    {
        if (visualRoot == null)
        {
            return;
        }

        float duration = Mathf.Max(0.01f, visualMoveDuration);
        float progress = Mathf.Clamp01(elapsed / duration);
        progress = progress * progress * (3f - 2f * progress);
        visualRoot.localPosition = Vector3.LerpUnclamped(startPosition, targetPosition, progress);
    }

    private IEnumerator MoveVisualTo(Vector3 targetPosition, float duration)
    {
        if (visualRoot == null)
        {
            yield break;
        }

        Vector3 startPosition = visualRoot.localPosition;
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            float progress = Mathf.Clamp01(elapsed / safeDuration);
            progress = progress * progress * (3f - 2f * progress);
            visualRoot.localPosition = Vector3.LerpUnclamped(
                startPosition,
                targetPosition,
                progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        visualRoot.localPosition = targetPosition;
    }

    private void ResolveVisualRoot()
    {
        if (visualRoot == null && animator != null)
        {
            visualRoot = animator.transform;
        }

        if (visualRoot != null && !visualRestPositionCaptured)
        {
            visualRestLocalPosition = visualRoot.localPosition;
            visualRestPositionCaptured = true;
        }
    }

    private void RestoreVisualPosition()
    {
        if (visualRoot != null && visualRestPositionCaptured)
        {
            visualRoot.localPosition = visualRestLocalPosition;
        }
    }

    private void PlaceVisualOnGround()
    {
        ResolveVisualRoot();

        if (visualRoot != null && visualRestPositionCaptured)
        {
            visualRoot.localPosition = GetVisualAttackPosition(AttackLane.Middle);
        }
    }

    private void EnsureHighJumpCloud()
    {
        if (visualRoot == null)
        {
            return;
        }

        if (highJumpCloud == null)
        {
            highJumpCloud = CreateDirectionalSmoke("HighJumpCloud");
        }

        ConfigureDirectionalSmoke(highJumpCloud, 0.62f);
        highJumpCloud.transform.position = visualRoot.position + highJumpCloudOffset;
        highJumpCloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void EnsureLowAttackSmoke()
    {
        if (visualRoot == null)
        {
            return;
        }

        if (lowAttackSmoke == null)
        {
            lowAttackSmoke = CreateDirectionalSmoke("LowAttackSmoke");
        }

        ConfigureDirectionalSmoke(lowAttackSmoke, -0.72f);
        lowAttackSmoke.transform.position = visualRoot.position + lowAttackSmokeOffset;
        lowAttackSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private ParticleSystem CreateDirectionalSmoke(string objectName)
    {
        GameObject smokeObject = new GameObject(objectName);
        smokeObject.transform.SetParent(transform, false);
        return smokeObject.AddComponent<ParticleSystem>();
    }

    private void ConfigureDirectionalSmoke(ParticleSystem smoke, float verticalVelocity)
    {
        if (smoke == null)
        {
            return;
        }

        ParticleSystem.MainModule main = smoke.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.72f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.38f, 0.82f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 0.72f),
            new Color(0.72f, 0.72f, 0.72f, 0.38f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 36;

        ParticleSystem.EmissionModule emission = smoke.emission;
        emission.rateOverTime = 22f;

        ParticleSystem.ShapeModule shape = smoke.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.64f;
        shape.radiusThickness = 1f;

        ParticleSystem.VelocityOverLifetimeModule velocity = smoke.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.38f, 0.38f);
        velocity.y = new ParticleSystem.MinMaxCurve(verticalVelocity * 0.75f, verticalVelocity * 1.25f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.NoiseModule noise = smoke.noise;
        noise.enabled = true;
        noise.strength = 0.18f;
        noise.frequency = 0.55f;
        noise.scrollSpeed = 0.12f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = smoke.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fadeGradient = new Gradient();
        fadeGradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.76f, 0.76f, 0.76f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.72f, 0.18f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = fadeGradient;

        ParticleSystemRenderer particleRenderer = smoke.GetComponent<ParticleSystemRenderer>();
        SpriteRenderer bossRenderer = visualRoot.GetComponentInChildren<SpriteRenderer>(true);
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;

        if (bossRenderer != null)
        {
            particleRenderer.sortingLayerID = bossRenderer.sortingLayerID;
            particleRenderer.sortingOrder = bossRenderer.sortingOrder + 1;
        }

        Material smokeMaterial = GetRuntimeSmokeMaterial();

        if (smokeMaterial != null)
        {
            particleRenderer.sharedMaterial = smokeMaterial;
        }
    }

    private static Material GetRuntimeSmokeMaterial()
    {
        if (runtimeSmokeMaterial != null)
        {
            return runtimeSmokeMaterial;
        }

        Shader spriteShader = Shader.Find("Sprites/Default");

        if (spriteShader == null)
        {
            return null;
        }

        const int textureSize = 16;
        Texture2D smokeTexture = new Texture2D(
            textureSize,
            textureSize,
            TextureFormat.RGBA32,
            false);
        smokeTexture.name = "RuntimeSmokeTexture";
        smokeTexture.filterMode = FilterMode.Bilinear;
        smokeTexture.wrapMode = TextureWrapMode.Clamp;
        smokeTexture.hideFlags = HideFlags.HideAndDontSave;

        Color[] pixels = new Color[textureSize * textureSize];
        Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float radius = textureSize * 0.5f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = 1f - Mathf.SmoothStep(0.42f, 1f, distance);
                pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        smokeTexture.SetPixels(pixels);
        smokeTexture.Apply(false, true);

        runtimeSmokeMaterial = new Material(spriteShader)
        {
            name = "RuntimeRedOniSmokeMaterial",
            mainTexture = smokeTexture,
            hideFlags = HideFlags.HideAndDontSave
        };
        return runtimeSmokeMaterial;
    }

    private void PlayHighJumpCloud()
    {
        EnsureHighJumpCloud();

        if (highJumpCloud == null)
        {
            return;
        }

        highJumpCloudActive = true;
        highJumpCloud.transform.position = visualRoot.position + highJumpCloudOffset;
        highJumpCloud.Play(true);
        highJumpCloud.Emit(12);
    }

    private void StopHighJumpCloud(bool clear)
    {
        highJumpCloudActive = false;

        if (highJumpCloud == null)
        {
            return;
        }

        highJumpCloud.Stop(
            true,
            clear
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting);
    }

    private void PlayLowAttackSmoke()
    {
        EnsureLowAttackSmoke();

        if (lowAttackSmoke == null)
        {
            return;
        }

        lowAttackSmokeActive = true;
        lowAttackSmoke.transform.position = visualRoot.position + lowAttackSmokeOffset;
        lowAttackSmoke.Play(true);
        lowAttackSmoke.Emit(12);
    }

    private void StopLowAttackSmoke(bool clear)
    {
        lowAttackSmokeActive = false;

        if (lowAttackSmoke == null)
        {
            return;
        }

        lowAttackSmoke.Stop(
            true,
            clear
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting);
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
