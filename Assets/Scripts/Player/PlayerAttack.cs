using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private Sprite hitboxSprite;
    [SerializeField] private Vector2 hitboxSize = new Vector2(0.9f, 0.65f);
    [SerializeField] private float attackOffset = 0.85f;
    [SerializeField] private float attackDuration = 0.12f;
    [SerializeField] private float attackCooldown = 0.18f;
    [SerializeField] private int damage = 1;
    [SerializeField] private Color hitboxColor = Color.white;
    [SerializeField] private bool hitboxSpriteFacesRight;

    private PlayerController playerController;
    private float facingDirection = 1f;
    private float nextAttackTime;
    private float attackVisualUntil;

    public bool IsAttacking => Time.time < attackVisualUntil;
    public float FacingDirection => facingDirection;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        UpdateFacingDirection();

        if (Input.GetKeyDown(KeyCode.J) && Time.time >= nextAttackTime && CanAttack())
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
        nextAttackTime = Time.time + attackCooldown;
        attackVisualUntil = Time.time + Mathf.Max(attackDuration, attackCooldown);

        GameObject hitboxObject = new GameObject("PlayerAttackHitbox");
        hitboxObject.transform.position = transform.position + new Vector3(facingDirection * attackOffset, 0f, 0f);
        hitboxObject.transform.localScale = new Vector3(hitboxSize.x, hitboxSize.y, 1f);

        if (hitboxSprite != null)
        {
            SpriteRenderer spriteRenderer = hitboxObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = hitboxSprite;
            spriteRenderer.color = hitboxColor;
            spriteRenderer.flipX = hitboxSpriteFacesRight ? facingDirection < 0f : facingDirection > 0f;
            spriteRenderer.sortingOrder = 6;
        }

        Rigidbody2D body = hitboxObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        BoxCollider2D hitboxCollider = hitboxObject.AddComponent<BoxCollider2D>();
        hitboxCollider.isTrigger = true;

        AttackHitbox hitbox = hitboxObject.AddComponent<AttackHitbox>();
        hitbox.Initialize(attackDuration, damage, transform.position);
    }
}
