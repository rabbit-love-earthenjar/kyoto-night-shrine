using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.12f;
    [SerializeField] private int damage = 1;

    private bool initialized;
    private Vector2 attackerPosition;
    private readonly HashSet<GhostHealth> hitGhosts = new HashSet<GhostHealth>();

    private void Start()
    {
        if (!initialized)
        {
            Destroy(gameObject, lifetime);
        }
    }

    public void Initialize(float activeLifetime)
    {
        Initialize(activeLifetime, damage, transform.position);
    }

    public void Initialize(float activeLifetime, int attackDamage, Vector2 attackOrigin)
    {
        initialized = true;
        lifetime = activeLifetime;
        damage = Mathf.Max(1, attackDamage);
        attackerPosition = attackOrigin;
        Destroy(gameObject, lifetime);
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
        }
    }
}
