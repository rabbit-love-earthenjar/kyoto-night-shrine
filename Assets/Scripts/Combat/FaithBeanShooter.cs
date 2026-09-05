using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class FaithBeanShooter : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode keyboardFireKey = KeyCode.K;
    [SerializeField] private bool allowMouseFire = true;
    [SerializeField, Min(0.25f)] private float mouseSpeedSampleWindow = 1.5f;

    [Header("Faith Bean")]
    [SerializeField, Min(0.05f)] private float fireCooldown = 0.28f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 7f;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 5.2f;
    [SerializeField] private float spawnDistance = 0.58f;
    [SerializeField] private float spawnHeight = 0.34f;
    [SerializeField] private Color beanColor = new Color(1f, 0.82f, 0.28f, 1f);
    [SerializeField] private Color beanCoreColor = new Color(1f, 1f, 0.82f, 1f);
    [SerializeField] private Sprite projectileVisualSprite;
    [SerializeField, Min(0.1f)] private float projectileVisualWidth = 1.3f;
    [SerializeField, Min(0.1f)] private float enemyAimAssistRadius = 1.25f;

    [Header("Audio")]
    [SerializeField] private AudioClip throwSfx;
    [SerializeField, Range(0f, 1f)] private float throwSfxVolume = 0.72f;

    [Header("Aim Guide")]
    [SerializeField] private bool showAimGuide = true;
    [SerializeField, Min(1f)] private float aimGuideLength = 30f;
    [SerializeField] private Sprite trajectorySprite;
    [SerializeField] private Sprite aimIconSprite;
    [SerializeField, Range(0.05f, 0.5f)] private float trajectoryHeightRatio = 0.22f;
    [SerializeField, Min(0.1f)] private float aimIconSize = 0.48f;
    [SerializeField, Range(8, 40)] private int trajectorySegments = 24;
    [SerializeField, Min(0.01f)] private float trajectoryWidth = 0.22f;
    [SerializeField, Range(6, 48)] private int maximumTrajectoryPieces = 36;
    [SerializeField, Min(0.05f)] private float trajectoryPieceSpacing = 0.72f;
    [SerializeField, Min(0.05f)] private float trajectoryPieceWidth = 0.52f;
    [SerializeField] private Color trajectoryColor = new Color(1f, 0.9f, 0.32f, 1f);
    [SerializeField] private Color trajectoryGlowColor = new Color(1f, 0.55f, 0.08f, 0.42f);
    [SerializeField] private Rect trajectoryTileUv = new Rect(0.043f, 0.875f, 0.167f, 0.085f);

    private PlayerController playerController;
    private PlayerAttack playerAttack;
    private Camera aimCamera;
    private LineRenderer aimGuide;
    private LineRenderer aimGuideGlow;
    private Sprite trajectoryTileSprite;
    private readonly List<SpriteRenderer> trajectoryPieces = new List<SpriteRenderer>();
    private Texture2D aimCursorTexture;
    private AudioSource sfxAudioSource;
    private bool aimCursorApplied;
    private float nextFireTime;
    private readonly List<float> acceptedMouseFireTimes = new List<float>();

    private struct AimSolution
    {
        public Vector2 Start;
        public Vector2 End;
        public float ArcHeight;
        public GhostHealth PreferredGhost;
    }

    private static Sprite beanSprite;
    private static Material spriteMaterial;

    public int ShotsFired { get; private set; }
    public float CurrentMouseAttackSpeed => CalculateMouseAttackSpeed();
    public bool HasCustomVisuals => projectileVisualSprite != null
        && trajectorySprite != null
        && aimIconSprite != null;

    public void ConfigureVisuals(Sprite beanVisual, Sprite trajectoryVisual, Sprite aimVisual)
    {
        projectileVisualSprite = beanVisual;
        trajectorySprite = trajectoryVisual;
        aimIconSprite = aimVisual;
    }

    public void ConfigureAimPresentation(float maximumDistance, float heightRatio)
    {
        aimGuideLength = Mathf.Max(1f, maximumDistance);
        trajectoryHeightRatio = Mathf.Clamp(heightRatio, 0.05f, 0.5f);
        trajectoryWidth = 0.22f;
    }

    public void ConfigureAudio(AudioClip clip)
    {
        throwSfx = clip;
    }

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerAttack = GetComponent<PlayerAttack>();
        aimCamera = Camera.main;
        EnsureSfxAudioSource();
        EnsureAimGuide();
    }

    private void OnEnable()
    {
        GameManager.RetryCompleted += ResetAfterRetry;
        SetAimCursorVisible(true);
    }

    private void OnDisable()
    {
        GameManager.RetryCompleted -= ResetAfterRetry;

        if (aimGuide != null)
        {
            aimGuide.enabled = false;
        }

        if (aimGuideGlow != null)
        {
            aimGuideGlow.enabled = false;
        }

        SetAimCursorVisible(false);
    }

    private void Update()
    {
        if (!CanAimOrFire())
        {
            SetAimGuideVisible(false);
            SetAimCursorVisible(false);
            return;
        }

        Vector2 aimPoint = GetAimWorldPoint();
        UpdateAimGuide(aimPoint);
        SetAimCursorVisible(true);

        if (allowMouseFire && Input.GetMouseButtonDown(0))
        {
            TryFireAtInternal(aimPoint, true);
        }
        else if (Input.GetKeyDown(keyboardFireKey))
        {
            TryFireAtInternal(aimPoint, false);
        }
    }

    public bool TryFireAt(Vector2 worldPoint)
    {
        return TryFireAtInternal(worldPoint, false);
    }

    private bool TryFireAtInternal(Vector2 worldPoint, bool firedByMouse)
    {
        if (!CanAimOrFire() || Time.time < nextFireTime)
        {
            return false;
        }

        AimSolution aim = CalculateAimSolution(worldPoint);
        Vector2 direction = (aim.End - aim.Start).normalized;

        GameObject beanObject = new GameObject("FaithBeanProjectile");
        beanObject.transform.position = aim.Start;
        beanObject.transform.rotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        CreateProjectileVisual(beanObject.transform);
        FaithBeanVfx.SpawnLaunch(aim.Start, direction);
        PlayThrowSfx();

        Rigidbody2D body = beanObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        beanObject.AddComponent<CircleCollider2D>();
        FaithBeanProjectile projectile = beanObject.AddComponent<FaithBeanProjectile>();

        if (firedByMouse)
        {
            RecordAcceptedMouseFire();
        }

        projectile.InitializeArc(
            aim.Start,
            aim.End,
            aim.ArcHeight,
            projectileSpeed,
            damage,
            projectileLifetime,
            aim.PreferredGhost,
            firedByMouse ? CurrentMouseAttackSpeed : 0f);

        nextFireTime = Time.time + fireCooldown;
        ShotsFired++;
        return true;
    }

    private void CreateProjectileVisual(Transform projectileRoot)
    {
        Sprite visualSprite = projectileVisualSprite != null
            ? projectileVisualSprite
            : GetBeanSprite();

        GameObject visualObject = new GameObject("FaithBeanVisual");
        visualObject.transform.SetParent(projectileRoot, false);
        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = visualSprite;
        renderer.color = projectileVisualSprite != null ? Color.white : beanColor;
        renderer.sortingOrder = 32;
        ScaleSpriteToWidth(visualObject.transform, visualSprite, projectileVisualWidth);

        GameObject coreObject = new GameObject("FaithBeanGlowCore");
        coreObject.transform.SetParent(projectileRoot, false);
        SpriteRenderer coreRenderer = coreObject.AddComponent<SpriteRenderer>();
        coreRenderer.sprite = GetBeanSprite();
        coreRenderer.color = new Color(beanCoreColor.r, beanCoreColor.g, beanCoreColor.b, 0.88f);
        coreRenderer.sortingOrder = renderer.sortingOrder + 1;
        ScaleSpriteToWidth(coreObject.transform, coreRenderer.sprite, 0.48f);

        TrailRenderer trail = projectileRoot.gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.46f;
        trail.startWidth = 0.42f;
        trail.endWidth = 0.06f;
        trail.minVertexDistance = 0.025f;
        trail.material = GetSpriteMaterial();
        trail.startColor = new Color(1f, 0.96f, 0.62f, 0.95f);
        trail.endColor = new Color(1f, 0.48f, 0.08f, 0f);
        trail.sortingOrder = renderer.sortingOrder - 1;
    }

    private bool CanAimOrFire()
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f)
        {
            return false;
        }

        if (playerController != null && !playerController.ControlsEnabled)
        {
            return false;
        }

        return GameManager.Instance == null || !GameManager.Instance.IsBlockingUiVisible;
    }

    private Vector2 GetAimWorldPoint()
    {
        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (aimCamera == null)
        {
            float facing = playerAttack != null ? playerAttack.FacingDirection : 1f;
            return (Vector2)transform.position + Vector2.right * facing;
        }

        Vector3 mouseWorld = aimCamera.ScreenToWorldPoint(Input.mousePosition);
        return new Vector2(mouseWorld.x, mouseWorld.y);
    }

    private Vector2 GetAimDirection(Vector2 worldPoint)
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * spawnHeight;
        Vector2 direction = worldPoint - origin;

        if (direction.sqrMagnitude < 0.01f)
        {
            float facing = playerAttack != null ? playerAttack.FacingDirection : 1f;
            direction = Vector2.right * facing;
        }

        return direction.normalized;
    }

    private void EnsureAimGuide()
    {
        if (!showAimGuide || aimGuide != null)
        {
            return;
        }

        GameObject guideObject = new GameObject("FaithBeanAimGuide");
        guideObject.transform.SetParent(transform, false);
        aimGuide = guideObject.AddComponent<LineRenderer>();
        aimGuide.useWorldSpace = true;
        aimGuide.positionCount = Mathf.Max(8, trajectorySegments) + 1;
        aimGuide.startWidth = trajectoryWidth;
        aimGuide.endWidth = trajectoryWidth;
        // Keep a guaranteed-visible curve under the decorative sheet pieces.
        // The supplied sheet is still used by UpdateTrajectoryPieces, but a bad
        // crop must never make the complete aiming guide disappear.
        aimGuide.material = GetSpriteMaterial();
        aimGuide.textureMode = LineTextureMode.Tile;
        aimGuide.alignment = LineAlignment.View;
        aimGuide.numCapVertices = 0;
        aimGuide.numCornerVertices = 2;
        aimGuide.startColor = trajectoryColor;
        aimGuide.endColor = trajectoryColor;
        aimGuide.sortingOrder = 30;

        GameObject glowObject = new GameObject("FaithBeanAimGuideGlow");
        glowObject.transform.SetParent(transform, false);
        aimGuideGlow = glowObject.AddComponent<LineRenderer>();
        aimGuideGlow.useWorldSpace = true;
        aimGuideGlow.positionCount = aimGuide.positionCount;
        aimGuideGlow.startWidth = trajectoryWidth * 1.8f;
        aimGuideGlow.endWidth = trajectoryWidth * 1.55f;
        aimGuideGlow.material = GetSpriteMaterial();
        aimGuideGlow.alignment = LineAlignment.View;
        aimGuideGlow.numCapVertices = 2;
        aimGuideGlow.numCornerVertices = 3;
        aimGuideGlow.startColor = trajectoryGlowColor;
        aimGuideGlow.endColor = new Color(
            trajectoryGlowColor.r,
            trajectoryGlowColor.g,
            trajectoryGlowColor.b,
            trajectoryGlowColor.a * 0.65f);
        aimGuideGlow.sortingOrder = 29;
    }

    private void UpdateAimGuide(Vector2 worldPoint)
    {
        EnsureAimGuide();

        if (aimGuide == null)
        {
            return;
        }

        AimSolution aim = CalculateAimSolution(worldPoint);
        int segmentCount = Mathf.Max(8, trajectorySegments);

        if (aimGuide.positionCount != segmentCount + 1)
        {
            aimGuide.positionCount = segmentCount + 1;
        }

        if (aimGuideGlow != null && aimGuideGlow.positionCount != segmentCount + 1)
        {
            aimGuideGlow.positionCount = segmentCount + 1;
        }

        float curveLength = 0f;
        Vector2 previousPoint = aim.Start;

        for (int index = 0; index <= segmentCount; index++)
        {
            float t = index / (float)segmentCount;
            Vector2 point = FaithBeanProjectile.EvaluateArc(
                aim.Start,
                aim.End,
                aim.ArcHeight,
                t);
            aimGuide.SetPosition(index, point);
            aimGuideGlow?.SetPosition(index, point);

            if (index > 0)
            {
                curveLength += Vector2.Distance(previousPoint, point);
            }

            previousPoint = point;
        }

        UpdateTrajectoryPieces(aim, curveLength);

        aimGuide.enabled = true;
        if (aimGuideGlow != null)
        {
            aimGuideGlow.enabled = true;
        }
    }

    private void SetAimGuideVisible(bool visible)
    {
        if (aimGuide != null)
        {
            aimGuide.enabled = visible;
        }

        if (aimGuideGlow != null)
        {
            aimGuideGlow.enabled = visible;
        }


        for (int index = 0; index < trajectoryPieces.Count; index++)
        {
            trajectoryPieces[index].enabled = visible;
        }
    }

    private AimSolution CalculateAimSolution(Vector2 requestedPoint)
    {
        GhostHealth preferredGhost = FindGhostTarget(requestedPoint);

        if (preferredGhost != null)
        {
            Collider2D targetCollider = preferredGhost.GetComponent<Collider2D>();
            requestedPoint = targetCollider != null
                ? targetCollider.bounds.center
                : preferredGhost.transform.position;
        }

        Vector2 direction = GetAimDirection(requestedPoint);
        Vector2 start = (Vector2)transform.position
            + Vector2.up * spawnHeight
            + direction * spawnDistance;
        Vector2 end = ClampAimPoint(start, requestedPoint);

        if ((end - start).sqrMagnitude < 0.01f)
        {
            end = start + direction;
        }

        return new AimSolution
        {
            Start = start,
            End = end,
            ArcHeight = CalculateArcHeight(start, end),
            PreferredGhost = preferredGhost
        };
    }

    private GhostHealth FindGhostTarget(Vector2 requestedPoint)
    {
        float assistRadius = Mathf.Max(0.1f, enemyAimAssistRadius);
        Collider2D[] candidates = Physics2D.OverlapCircleAll(
            requestedPoint,
            assistRadius);
        GhostHealth closest = null;
        float closestDistance = float.PositiveInfinity;

        foreach (Collider2D candidate in candidates)
        {
            GhostHealth health = candidate != null
                ? candidate.GetComponentInParent<GhostHealth>()
                : null;

            if (health == null)
            {
                continue;
            }

            Vector2 targetPoint = candidate.bounds.ClosestPoint(requestedPoint);
            float distance = Vector2.SqrMagnitude(targetPoint - requestedPoint);

            if (distance < closestDistance)
            {
                closest = health;
                closestDistance = distance;
            }
        }

        if (closest != null)
        {
            return closest;
        }

        // Existing scene instances can briefly have stale collider poses after
        // a runner moves or spawns. Fall back to nearby health roots so aiming
        // at the visible add remains forgiving without globally homing shots.
        GhostHealth[] healthTargets = Object.FindObjectsByType<GhostHealth>(
            FindObjectsSortMode.None);
        float maximumDistance = assistRadius * assistRadius;

        foreach (GhostHealth health in healthTargets)
        {
            if (health == null || !health.isActiveAndEnabled)
            {
                continue;
            }

            Collider2D targetCollider = health.GetComponent<Collider2D>();
            Vector2 targetPoint = targetCollider != null
                ? targetCollider.bounds.ClosestPoint(requestedPoint)
                : (Vector2)health.transform.position;
            float distance = Vector2.SqrMagnitude(targetPoint - requestedPoint);

            if (distance <= maximumDistance && distance < closestDistance)
            {
                closest = health;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private void UpdateTrajectoryPieces(AimSolution aim, float curveLength)
    {
        Sprite pieceSprite = GetTrajectoryTileSprite();

        if (pieceSprite == null)
        {
            SetTrajectoryPieceCount(0);
            return;
        }

        int pieceCount = Mathf.Clamp(
            Mathf.CeilToInt(curveLength / Mathf.Max(0.05f, trajectoryPieceSpacing)),
            2,
            maximumTrajectoryPieces);
        EnsureTrajectoryPiecePool(pieceCount, pieceSprite);

        for (int index = 0; index < trajectoryPieces.Count; index++)
        {
            SpriteRenderer piece = trajectoryPieces[index];
            bool active = index < pieceCount;
            piece.enabled = active;

            if (!active)
            {
                continue;
            }

            float t = pieceCount <= 1 ? 0f : index / (pieceCount - 1f);
            float tangentStep = 1f / Mathf.Max(16f, trajectorySegments * 2f);
            float nextT = Mathf.Min(1f, t + tangentStep);
            Vector2 point = FaithBeanProjectile.EvaluateArc(
                aim.Start,
                aim.End,
                aim.ArcHeight,
                t);
            Vector2 nextPoint = FaithBeanProjectile.EvaluateArc(
                aim.Start,
                aim.End,
                aim.ArcHeight,
                nextT);
            Vector2 tangent = nextPoint - point;

            piece.transform.position = point;

            if (tangent.sqrMagnitude > 0.0001f)
            {
                piece.transform.rotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
            }

            ScaleSpriteToWidth(piece.transform, pieceSprite, trajectoryPieceWidth);
        }
    }

    private void EnsureTrajectoryPiecePool(int requiredCount, Sprite pieceSprite)
    {
        while (trajectoryPieces.Count < requiredCount)
        {
            GameObject pieceObject = new GameObject(
                $"FaithBeanTrajectoryPiece_{trajectoryPieces.Count + 1:00}");
            pieceObject.transform.SetParent(transform, false);
            SpriteRenderer renderer = pieceObject.AddComponent<SpriteRenderer>();
            renderer.sprite = pieceSprite;
            renderer.color = new Color(1f, 1f, 1f, trajectoryColor.a);
            renderer.sortingOrder = 31;
            trajectoryPieces.Add(renderer);
        }
    }

    private void SetTrajectoryPieceCount(int visibleCount)
    {
        for (int index = 0; index < trajectoryPieces.Count; index++)
        {
            trajectoryPieces[index].enabled = index < visibleCount;
        }
    }

    private void ResetAfterRetry()
    {
        nextFireTime = 0f;
        acceptedMouseFireTimes.Clear();
        SetAimGuideVisible(true);
        SetAimCursorVisible(true);
    }

    private void RecordAcceptedMouseFire()
    {
        acceptedMouseFireTimes.Add(Time.unscaledTime);
        PruneMouseFireSamples(Time.unscaledTime);
    }

    private float CalculateMouseAttackSpeed()
    {
        float now = Time.unscaledTime;
        PruneMouseFireSamples(now);

        if (acceptedMouseFireTimes.Count < 2)
        {
            return 0f;
        }

        float elapsed = acceptedMouseFireTimes[acceptedMouseFireTimes.Count - 1]
            - acceptedMouseFireTimes[0];

        if (elapsed <= 0.001f)
        {
            return 0f;
        }

        // Express the recent accepted-shot cadence as clicks per minute.
        return (acceptedMouseFireTimes.Count - 1) * 60f / elapsed;
    }

    private void PruneMouseFireSamples(float now)
    {
        float oldestAllowed = now - Mathf.Max(0.25f, mouseSpeedSampleWindow);

        while (acceptedMouseFireTimes.Count > 0
            && acceptedMouseFireTimes[0] < oldestAllowed)
        {
            acceptedMouseFireTimes.RemoveAt(0);
        }
    }

    private Vector2 ClampAimPoint(Vector2 origin, Vector2 requestedPoint)
    {
        Vector2 offset = requestedPoint - origin;
        float maxDistance = Mathf.Max(1f, aimGuideLength);

        if (offset.sqrMagnitude > maxDistance * maxDistance)
        {
            offset = offset.normalized * maxDistance;
        }

        return origin + offset;
    }

    private float CalculateArcHeight(Vector2 start, Vector2 end)
    {
        float distance = Vector2.Distance(start, end);
        return Mathf.Clamp(distance * trajectoryHeightRatio, 0.18f, 2.6f);
    }

    private void SetAimCursorVisible(bool visible)
    {
        if (Application.isBatchMode)
        {
            return;
        }

        bool shouldShow = visible && aimIconSprite != null;

        if (shouldShow == aimCursorApplied)
        {
            return;
        }

        if (!shouldShow)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            aimCursorApplied = false;
            return;
        }

        if (aimCursorTexture == null)
        {
            aimCursorTexture = CreateCursorTexture(aimIconSprite, aimIconSize);
        }

        if (aimCursorTexture == null)
        {
            return;
        }

        Vector2 hotspot = new Vector2(
            aimCursorTexture.width * 0.5f,
            aimCursorTexture.height * 0.5f);
        Cursor.SetCursor(aimCursorTexture, hotspot, CursorMode.ForceSoftware);
        aimCursorApplied = true;
    }

    private static Sprite GetBeanSprite()
    {
        if (beanSprite != null)
        {
            return beanSprite;
        }

        const int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "RuntimeFaithBean";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float normalizedDistance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                texture.SetPixel(x, y, normalizedDistance <= 0.82f ? Color.white : Color.clear);
            }
        }

        texture.Apply(false, true);
        beanSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            16f);
        beanSprite.name = "RuntimeFaithBeanSprite";
        return beanSprite;
    }

    private static Texture2D CreateCursorTexture(Sprite sprite, float normalizedSize)
    {
        if (sprite == null || sprite.texture == null)
        {
            return null;
        }

        int cursorSize = Mathf.RoundToInt(Mathf.Lerp(40f, 80f, Mathf.Clamp01(normalizedSize)));
        RenderTexture temporary = RenderTexture.GetTemporary(
            cursorSize,
            cursorSize,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB);
        RenderTexture previous = RenderTexture.active;

        try
        {
            Graphics.Blit(sprite.texture, temporary);
            RenderTexture.active = temporary;
            Texture2D cursorTexture = new Texture2D(
                cursorSize,
                cursorSize,
                TextureFormat.RGBA32,
                false)
            {
                name = "RuntimeFaithBeanAimCursor",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            cursorTexture.ReadPixels(new Rect(0f, 0f, cursorSize, cursorSize), 0, 0, false);
            cursorTexture.Apply(false, false);
            return cursorTexture;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
        }
    }

    private static void ScaleSpriteToWidth(Transform target, Sprite sprite, float width)
    {
        if (target == null || sprite == null || sprite.bounds.size.x <= 0.001f)
        {
            return;
        }

        float uniformScale = Mathf.Max(0.01f, width) / sprite.bounds.size.x;
        target.localScale = Vector3.one * uniformScale;
    }

    private static Material GetSpriteMaterial()
    {
        if (spriteMaterial != null)
        {
            return spriteMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");

        if (shader != null)
        {
            spriteMaterial = new Material(shader)
            {
                name = "RuntimeFaithBeanMaterial",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        return spriteMaterial;
    }

    private void PlayThrowSfx()
    {
        EnsureSfxAudioSource();

        if (sfxAudioSource != null && throwSfx != null)
        {
            sfxAudioSource.pitch = 1f;
            sfxAudioSource.PlayOneShot(
                throwSfx,
                throwSfxVolume * GameSettings.SfxVolume);
        }
    }

    private void EnsureSfxAudioSource()
    {
        if (sfxAudioSource != null)
        {
            return;
        }

        Transform audioTransform = transform.Find("FaithBeanSfx");
        GameObject audioObject;

        if (audioTransform == null)
        {
            audioObject = new GameObject("FaithBeanSfx");
            audioObject.transform.SetParent(transform, false);
        }
        else
        {
            audioObject = audioTransform.gameObject;
        }

        sfxAudioSource = audioObject.GetComponent<AudioSource>();

        if (sfxAudioSource == null)
        {
            sfxAudioSource = audioObject.AddComponent<AudioSource>();
        }

        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.loop = false;
        sfxAudioSource.spatialBlend = 0f;
    }

    private Sprite GetTrajectoryTileSprite()
    {
        if (trajectoryTileSprite != null)
        {
            return trajectoryTileSprite;
        }

        if (trajectorySprite == null || trajectorySprite.texture == null)
        {
            return null;
        }

        Texture2D source = trajectorySprite.texture;
        Rect sourceRect = new Rect(
            Mathf.Clamp01(trajectoryTileUv.x) * source.width,
            Mathf.Clamp01(trajectoryTileUv.y) * source.height,
            Mathf.Clamp01(trajectoryTileUv.width) * source.width,
            Mathf.Clamp01(trajectoryTileUv.height) * source.height);

        trajectoryTileSprite = Sprite.Create(
            source,
            sourceRect,
            new Vector2(0.5f, 0.5f),
            160f);
        trajectoryTileSprite.name = "RuntimeFaithBeanTrajectoryPiece";
        return trajectoryTileSprite;
    }
}
