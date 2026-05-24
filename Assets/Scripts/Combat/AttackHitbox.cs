using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.12f;

    private bool initialized;

    private void Start()
    {
        if (!initialized)
        {
            Destroy(gameObject, lifetime);
        }
    }

    public void Initialize(float activeLifetime)
    {
        initialized = true;
        lifetime = activeLifetime;
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
        GhostEnemy ghost = other.GetComponentInParent<GhostEnemy>();

        if (ghost != null)
        {
            ghost.TakeHit();
        }
    }
}
