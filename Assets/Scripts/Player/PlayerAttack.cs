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

    private PlayerController playerController;
    private float facingDirection = 1f;
    private float nextAttackTime;
    private float attackVisualUntil;
    private float bufferedAttackUntil;
    private static Sprite fallbackHitboxSprite;

    public bool IsAttacking => Time.time < attackVisualUntil;
    public float FacingDirection => facingDirection;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        UpdateFacingDirection();

        if (Input.GetKeyDown(KeyCode.J))
        {
            bufferedAttackUntil = Time.time + Mathf.Max(0f, attackInputBufferTime);
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

        float activeDuration = Mathf.Max(0.01f, attackDuration);
        float visualDuration = Mathf.Max(activeDuration, attackVisualDuration);
        nextAttackTime = Time.time + attackCooldown;
        attackVisualUntil = Time.time + Mathf.Max(visualDuration, attackCooldown);
        GameAudio.PlayPlayerAttack();

        GameObject hitboxObject = new GameObject("PlayerAttackHitbox");
        hitboxObject.transform.position = transform.position + new Vector3(facingDirection * attackOffset, 0f, 0f);
        hitboxObject.transform.localScale = new Vector3(hitboxSize.x, hitboxSize.y, 1f);
        CombatFeedbackEffects.SpawnAttackStart(hitboxObject.transform.position, new Vector2(facingDirection, 0f));

        Sprite effectSprite = hitboxSprite != null ? hitboxSprite : GetFallbackHitboxSprite();

        if (effectSprite != null)
        {
            GameObject visualObject = new GameObject("AttackEffectVisual");
            visualObject.transform.SetParent(hitboxObject.transform, false);

            SpriteRenderer spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = effectSprite;
            spriteRenderer.color = hitboxSprite != null ? hitboxColor : fallbackEffectColor;
            spriteRenderer.flipX = hitboxSpriteFacesRight ? facingDirection < 0f : facingDirection > 0f;
            spriteRenderer.sortingOrder = 6;

            AttackEffectVisual effectVisual = visualObject.AddComponent<AttackEffectVisual>();
            effectVisual.Play(visualDuration, effectStartScale, effectEndScale, facingDirection, effectTravelDistance);
        }

        Rigidbody2D body = hitboxObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        BoxCollider2D hitboxCollider = hitboxObject.AddComponent<BoxCollider2D>();
        hitboxCollider.isTrigger = true;

        AttackHitbox hitbox = hitboxObject.AddComponent<AttackHitbox>();
        hitbox.Initialize(activeDuration, damage, transform.position, visualDuration);
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
