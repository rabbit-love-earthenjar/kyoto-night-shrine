using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.12f;
    [SerializeField] private float visualLifetime = 0.18f;
    [SerializeField] private int damage = 1;

    private Collider2D hitboxCollider;
    private bool initialized;
    private Vector2 attackerPosition;
    private readonly HashSet<GhostHealth> hitGhosts = new HashSet<GhostHealth>();
    private readonly HashSet<BreakableBlock> hitBreakables = new HashSet<BreakableBlock>();

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (!initialized)
        {
            Initialize(lifetime, damage, transform.position, visualLifetime);
        }
    }

    public void Initialize(float activeLifetime)
    {
        Initialize(activeLifetime, damage, transform.position);
    }

    public void Initialize(float activeLifetime, int attackDamage, Vector2 attackOrigin)
    {
        Initialize(activeLifetime, attackDamage, attackOrigin, activeLifetime);
    }

    public void Initialize(float activeLifetime, int attackDamage, Vector2 attackOrigin, float totalLifetime)
    {
        initialized = true;
        lifetime = Mathf.Max(0.01f, activeLifetime);
        visualLifetime = Mathf.Max(lifetime, totalLifetime);
        damage = Mathf.Max(1, attackDamage);
        attackerPosition = attackOrigin;
        StartCoroutine(DisableColliderAfterActiveLifetime());
        Destroy(gameObject, visualLifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        GhostHealth ghostHealth = other.GetComponentInParent<GhostHealth>();

        if (ghostHealth != null)
        {
            if (hitGhosts.Add(ghostHealth))
            {
                ghostHealth.TakeDamage(damage, attackerPosition);
            }

            return;
        }

        GhostEnemy ghost = other.GetComponentInParent<GhostEnemy>();

        if (ghost != null)
        {
            ghost.TakeHit();
            return;
        }

        BreakableBlock breakableBlock = other.GetComponentInParent<BreakableBlock>();

        if (breakableBlock != null && hitBreakables.Add(breakableBlock))
        {
            breakableBlock.TakeDamage(damage, attackerPosition);
        }
    }

    private IEnumerator DisableColliderAfterActiveLifetime()
    {
        yield return new WaitForSeconds(lifetime);

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
    }
}
