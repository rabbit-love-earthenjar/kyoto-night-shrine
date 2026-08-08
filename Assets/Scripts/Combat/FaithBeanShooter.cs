using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class FaithBeanShooter : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode keyboardFireKey = KeyCode.K;
    [SerializeField] private bool allowMouseFire = true;

    [Header("Faith Bean")]
    [SerializeField, Min(0.05f)] private float fireCooldown = 0.28f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 13f;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 2.4f;
    [SerializeField] private float spawnDistance = 0.58f;
    [SerializeField] private float spawnHeight = 0.34f;
    [SerializeField] private Color beanColor = new Color(1f, 0.82f, 0.28f, 1f);
    [SerializeField] private Color beanCoreColor = new Color(1f, 1f, 0.82f, 1f);

    [Header("Aim Guide")]
    [SerializeField] private bool showAimGuide = true;
    [SerializeField, Min(0.5f)] private float aimGuideLength = 2.2f;

    private PlayerController playerController;
    private PlayerAttack playerAttack;
    private Camera aimCamera;
    private LineRenderer aimGuide;
    private float nextFireTime;

    private static Sprite beanSprite;
    private static Material spriteMaterial;

    public int ShotsFired { get; private set; }

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerAttack = GetComponent<PlayerAttack>();
        aimCamera = Camera.main;
        EnsureAimGuide();
    }

    private void OnEnable()
    {
        GameManager.RetryCompleted += ResetAfterRetry;
    }

    private void OnDisable()
    {
        GameManager.RetryCompleted -= ResetAfterRetry;

        if (aimGuide != null)
        {
            aimGuide.enabled = false;
        }
    }

    private void Update()
    {
        if (!CanAimOrFire())
        {
            SetAimGuideVisible(false);
            return;
        }

        Vector2 aimPoint = GetAimWorldPoint();
        Vector2 direction = GetAimDirection(aimPoint);
        UpdateAimGuide(direction);

        bool firePressed = (allowMouseFire && Input.GetMouseButtonDown(0))
            || Input.GetKeyDown(keyboardFireKey);

        if (firePressed)
        {
            TryFireAt(aimPoint);
        }
    }

    public bool TryFireAt(Vector2 worldPoint)
    {
        if (!CanAimOrFire() || Time.time < nextFireTime)
        {
            return false;
        }

        Vector2 direction = GetAimDirection(worldPoint);
        Vector3 origin = transform.position
            + Vector3.up * spawnHeight
            + (Vector3)(direction * spawnDistance);

        GameObject beanObject = new GameObject("FaithBeanProjectile");
        beanObject.transform.position = origin;
        beanObject.transform.localScale = Vector3.one * 0.42f;

        SpriteRenderer renderer = beanObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetBeanSprite();
        renderer.color = beanColor;
        renderer.sortingOrder = 24;

        GameObject coreObject = new GameObject("BeanCore");
        coreObject.transform.SetParent(beanObject.transform, false);
        coreObject.transform.localScale = Vector3.one * 0.48f;
        SpriteRenderer coreRenderer = coreObject.AddComponent<SpriteRenderer>();
        coreRenderer.sprite = renderer.sprite;
        coreRenderer.color = beanCoreColor;
        coreRenderer.sortingOrder = renderer.sortingOrder + 1;

        TrailRenderer trail = beanObject.AddComponent<TrailRenderer>();
        trail.time = 0.12f;
        trail.startWidth = 0.14f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.04f;
        trail.material = GetSpriteMaterial();
        trail.startColor = new Color(beanColor.r, beanColor.g, beanColor.b, 0.7f);
        trail.endColor = new Color(beanColor.r, beanColor.g, beanColor.b, 0f);
        trail.sortingOrder = renderer.sortingOrder - 1;

        Rigidbody2D body = beanObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        beanObject.AddComponent<CircleCollider2D>();
        FaithBeanProjectile projectile = beanObject.AddComponent<FaithBeanProjectile>();
        projectile.Initialize(direction, projectileSpeed, damage, projectileLifetime);

        nextFireTime = Time.time + fireCooldown;
        ShotsFired++;
        return true;
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
        aimGuide.positionCount = 2;
        aimGuide.startWidth = 0.025f;
        aimGuide.endWidth = 0.01f;
        aimGuide.material = GetSpriteMaterial();
        aimGuide.startColor = new Color(1f, 0.92f, 0.55f, 0.7f);
        aimGuide.endColor = new Color(1f, 0.92f, 0.55f, 0.08f);
        aimGuide.sortingOrder = 23;
    }

    private void UpdateAimGuide(Vector2 direction)
    {
        EnsureAimGuide();

        if (aimGuide == null)
        {
            return;
        }

        Vector3 start = transform.position + Vector3.up * spawnHeight;
        aimGuide.enabled = true;
        aimGuide.SetPosition(0, start);
        aimGuide.SetPosition(1, start + (Vector3)(direction * aimGuideLength));
    }

    private void SetAimGuideVisible(bool visible)
    {
        if (aimGuide != null)
        {
            aimGuide.enabled = visible;
        }
    }

    private void ResetAfterRetry()
    {
        nextFireTime = 0f;
        SetAimGuideVisible(true);
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
}
