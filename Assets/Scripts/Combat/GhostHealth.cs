using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class GhostHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 1;
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private float knockbackDistance = 0.25f;
    [SerializeField] private float deathDelay = 0.08f;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private int faithPointReward = 1;
    [SerializeField] private GameManager gameManager;

    private SpriteRenderer spriteRenderer;
    private Collider2D ghostCollider;
    private GhostEnemy ghostMovement;
    private Color originalColor;
    private int currentHP;
    private bool isDead;
    private Coroutine flashRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ghostCollider = GetComponent<Collider2D>();
        ghostMovement = GetComponent<GhostEnemy>();
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        currentHP = Mathf.Max(1, maxHP);

        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(int damage, Vector2 attackerPosition)
    {
        if (isDead)
        {
            return;
        }

        currentHP -= Mathf.Max(1, damage);
        ApplyHitFeedback(attackerPosition);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        AwardFaithPoints();

        if (ghostCollider != null)
        {
            ghostCollider.enabled = false;
        }

        Destroy(gameObject, deathDelay);
    }

    private void AwardFaithPoints()
    {
        if (gameManager != null && faithPointReward > 0)
        {
            gameManager.AddFaithPoints(faithPointReward);
        }
    }

    private void ApplyHitFeedback(Vector2 attackerPosition)
    {
        Vector2 knockbackDirection = ((Vector2)transform.position - attackerPosition).normalized;

        if (knockbackDirection.sqrMagnitude < 0.01f)
        {
            knockbackDirection = Vector2.right;
        }

        if (ghostMovement != null)
        {
            ghostMovement.ApplyKnockback(knockbackDirection, knockbackDistance);
        }

        if (spriteRenderer == null)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);

        if (!isDead && spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}
