using UnityEngine;

public class HazardDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null && !playerHealth.IsInvincible)
        {
            GameAudio.PlayHazardSpike();
            playerHealth.TakeDamage(damage, transform.position);
        }
    }
}
