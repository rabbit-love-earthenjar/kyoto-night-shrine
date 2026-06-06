using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private Sprite hitboxSprite;
    [SerializeField] private Vector2 hitboxSize = new Vector2(0.9f, 0.65f);
    [SerializeField] private float attackOffset = 0.85f;
    [SerializeField] private float attackDuration = 0.12f;
    [SerializeField] private float attackCooldown = 0.18f;
    [SerializeField] private float attackInputBufferTime = 0.08f;
    [SerializeField] private float attackVisualDuration = 0.2f;
    [SerializeField] private float effectStartScale = 0.85f;
    [SerializeField] private float effectEndScale = 1.12f;
    [SerializeField] private float effectTravelDistance = 0.12f;
    [SerializeField] private Color fallbackEffectColor = new Color(0.88f, 0.98f, 1f, 0.92f);
    [SerializeField] private int damage = 1;
    [SerializeField] private Color hitboxColor = Color.white;
    [SerializeField] private bool hitboxSpriteFacesRight;
    [SerializeField] private bool enableCombo = true;
    [SerializeField] private float comboInputBufferTime = 0.16f;
    [SerializeField] private float comboResetTime = 0.55f;
    [SerializeField] private Vector2[] comboHitboxSizes =
    {
        new Vector2(0.85f, 0.62f),
        new Vector2(1f, 0.68f),
        new Vector2(1.18f, 0.76f)
    };
    [SerializeField] private float[] comboAttackOffsets = { 0.78f, 0.88f, 1f };
    [SerializeField] private float[] comboAttackDurations = { 0.1f, 0.11f, 0.14f };
    [SerializeField] private float[] comboAttackCooldowns = { 0.15f, 0.17f, 0.24f };
    [SerializeField] private float[] comboVisualDurations = { 0.16f, 0.18f, 0.24f };
    [SerializeField] private int[] comboDamage = { 1, 1, 2 };
    [SerializeField] private float[] comboEffectEndScales = { 1.02f, 1.12f, 1.26f };
    [SerializeField] private float[] comboEffectTravelDistances = { 0.08f, 0.12f, 0.18f };
    [SerializeField] private Color[] comboEffectColors =
    {
        new Color(0.88f, 0.98f, 1f, 0.9f),
        new Color(0.72f, 0.94f, 1f, 0.95f),
        new Color(1f, 0.92f, 0.68f, 1f)
    };

    private PlayerController playerController;
    private float facingDirection = 1f;
    private float nextAttackTime;
    private float attackVisualUntil;
    private float bufferedAttackUntil;
    private float comboExpiresAt;
    private int currentComboIndex;
    private int nextComboIndex;
    private static Sprite fallbackHitboxSprite;

    public bool IsAttacking => Time.time < attackVisualUntil;
    public float FacingDirection => facingDirection;
    public int CurrentComboStep => enableCombo ? currentComboIndex + 1 : 1;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        UpdateFacingDirection();
        ResetComboIfExpired();

        if (Input.GetKeyDown(KeyCode.J))
        {
            float inputBufferTime = enableCombo
                ? Mathf.Max(attackInputBufferTime, comboInputBufferTime)
                : attackInputBufferTime;
            bufferedAttackUntil = Time.time + Mathf.Max(0f, inputBufferTime);
        }

        if (bufferedAttackUntil > 0f && Time.time <= bufferedAttackUntil && Time.time >= nextAttackTime && CanAttack())
        {
            Attack();
        }
    }

    private void UpdateFacingDirection()
    {
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            facingDirection = -1f;
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            facingDirection = 1f;
        }
    }

    private bool CanAttack()
    {
        return playerController == null || playerController.ControlsEnabled;
    }

    private void Attack()
    {
        bufferedAttackUntil = 0f;

        int comboIndex = GetAttackComboIndex();
        currentComboIndex = comboIndex;
        Vector2 currentHitboxSize = GetComboVector(comboHitboxSizes, comboIndex, hitboxSize);
        float currentAttackOffset = GetComboFloat(comboAttackOffsets, comboIndex, attackOffset);
        float activeDuration = Mathf.Max(0.01f, GetComboFloat(comboAttackDurations, comboIndex, attackDuration));
        float currentCooldown = Mathf.Max(0.01f, GetComboFloat(comboAttackCooldowns, comboIndex, attackCooldown));
        float visualDuration = Mathf.Max(activeDuration, GetComboFloat(comboVisualDurations, comboIndex, attackVisualDuration));
        float currentEffectEndScale = GetComboFloat(comboEffectEndScales, comboIndex, effectEndScale);
        float currentEffectTravelDistance = GetComboFloat(comboEffectTravelDistances, comboIndex, effectTravelDistance);
        Color currentEffectColor = GetComboColor(comboEffectColors, comboIndex, hitboxSprite != null ? hitboxColor : fallbackEffectColor);
        int currentDamage = GetComboInt(comboDamage, comboIndex, damage);

        nextAttackTime = Time.time + currentCooldown;
        attackVisualUntil = Time.time + Mathf.Max(visualDuration, currentCooldown);
        AdvanceCombo(comboIndex);
        GameAudio.PlayPlayerAttack(comboIndex + 1);

        GameObject hitboxObject = new GameObject($"PlayerAttackHitbox_{comboIndex + 1}");
        hitboxObject.transform.position = transform.position + new Vector3(facingDirection * currentAttackOffset, 0f, 0f);
        hitboxObject.transform.localScale = new Vector3(currentHitboxSize.x, currentHitboxSize.y, 1f);
        CombatFeedbackEffects.SpawnAttackStart(hitboxObject.transform.position, new Vector2(facingDirection, 0f), comboIndex + 1);

        Sprite effectSprite = hitboxSprite != null ? hitboxSprite : GetFallbackHitboxSprite();

        if (effectSprite != null)
        {
            GameObject visualObject = new GameObject("AttackEffectVisual");
            visualObject.transform.SetParent(hitboxObject.transform, false);

            SpriteRenderer spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = effectSprite;
            spriteRenderer.color = currentEffectColor;
            spriteRenderer.flipX = hitboxSpriteFacesRight ? facingDirection < 0f : facingDirection > 0f;
            spriteRenderer.sortingOrder = 6;

            AttackEffectVisual effectVisual = visualObject.AddComponent<AttackEffectVisual>();
            effectVisual.Play(visualDuration, effectStartScale, currentEffectEndScale, facingDirection, currentEffectTravelDistance);
        }

        Rigidbody2D body = hitboxObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        BoxCollider2D hitboxCollider = hitboxObject.AddComponent<BoxCollider2D>();
        hitboxCollider.isTrigger = true;

        AttackHitbox hitbox = hitboxObject.AddComponent<AttackHitbox>();
        hitbox.Initialize(activeDuration, currentDamage, transform.position, visualDuration);
    }

    private int GetAttackComboIndex()
    {
        if (!enableCombo)
        {
            return 0;
        }

        if (Time.time > comboExpiresAt)
        {
            nextComboIndex = 0;
        }

        return Mathf.Clamp(nextComboIndex, 0, GetMaxComboIndex());
    }

    private void AdvanceCombo(int usedComboIndex)
    {
        if (!enableCombo)
        {
            nextComboIndex = 0;
            comboExpiresAt = 0f;
            return;
        }

        int maxComboIndex = GetMaxComboIndex();
        nextComboIndex = usedComboIndex >= maxComboIndex ? 0 : usedComboIndex + 1;
        comboExpiresAt = Time.time + Mathf.Max(0.05f, comboResetTime);
    }

    private void ResetComboIfExpired()
    {
        if (enableCombo && nextComboIndex != 0 && Time.time > comboExpiresAt)
        {
            nextComboIndex = 0;
        }
    }

    private int GetMaxComboIndex()
    {
        int minLength = GetUsableLength(comboHitboxSizes);
        minLength = Mathf.Min(minLength, GetUsableLength(comboAttackOffsets));
        minLength = Mathf.Min(minLength, GetUsableLength(comboAttackDurations));
        minLength = Mathf.Min(minLength, GetUsableLength(comboAttackCooldowns));
        minLength = Mathf.Min(minLength, GetUsableLength(comboVisualDurations));
        minLength = Mathf.Min(minLength, GetUsableLength(comboDamage));
        minLength = Mathf.Min(minLength, GetUsableLength(comboEffectEndScales));
        minLength = Mathf.Min(minLength, GetUsableLength(comboEffectTravelDistances));
        minLength = Mathf.Min(minLength, GetUsableLength(comboEffectColors));
        return Mathf.Max(0, minLength - 1);
    }

    private static int GetUsableLength<T>(T[] values)
    {
        return values != null && values.Length > 0 ? values.Length : 1;
    }

    private static float GetComboFloat(float[] values, int index, float fallback)
    {
        if (values == null || values.Length == 0)
        {
            return fallback;
        }

        return values[Mathf.Clamp(index, 0, values.Length - 1)];
    }

    private static int GetComboInt(int[] values, int index, int fallback)
    {
        if (values == null || values.Length == 0)
        {
            return fallback;
        }

        return Mathf.Max(1, values[Mathf.Clamp(index, 0, values.Length - 1)]);
    }

    private static Vector2 GetComboVector(Vector2[] values, int index, Vector2 fallback)
    {
        if (values == null || values.Length == 0)
        {
            return fallback;
        }

        return values[Mathf.Clamp(index, 0, values.Length - 1)];
    }

    private static Color GetComboColor(Color[] values, int index, Color fallback)
    {
        if (values == null || values.Length == 0)
        {
            return fallback;
        }

        return values[Mathf.Clamp(index, 0, values.Length - 1)];
    }

    private static Sprite GetFallbackHitboxSprite()
    {
        if (fallbackHitboxSprite != null)
        {
            return fallbackHitboxSprite;
        }

        Texture2D texture = new Texture2D(24, 10, TextureFormat.RGBA32, false);
        texture.name = "RuntimePurifySlash";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color clear = Color.clear;
        Color white = Color.white;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        for (int x = 1; x < texture.width - 1; x++)
        {
            int centerY = Mathf.RoundToInt(Mathf.Lerp(2f, 7f, x / (float)(texture.width - 1)));

            for (int y = centerY - 1; y <= centerY + 1; y++)
            {
                if (y >= 0 && y < texture.height)
                {
                    texture.SetPixel(x, y, white);
                }
            }
        }

        texture.Apply();
        fallbackHitboxSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 16f);
        fallbackHitboxSprite.name = "RuntimePurifySlashSprite";
        return fallbackHitboxSprite;
    }
}
