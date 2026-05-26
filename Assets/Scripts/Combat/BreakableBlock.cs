using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BreakableBlock : MonoBehaviour
{
    [SerializeField] private int maxHP = 1;
    [SerializeField] private int faithPointReward = 1;
    [SerializeField] private int healReward;
    [SerializeField] private GameManager gameManager;

    private int currentHP;
    private bool broken;

    private void Awake()
    {
        currentHP = Mathf.Max(1, maxHP);

        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }
    }

    public void TakeDamage(int damage)
    {
        if (broken)
        {
            return;
        }

        currentHP -= Mathf.Max(1, damage);

        if (currentHP <= 0)
        {
            Break();
        }
    }

    private void Break()
    {
        broken = true;

        if (gameManager != null && faithPointReward > 0)
        {
            gameManager.AddFaithPoints(faithPointReward);
        }

        if (healReward > 0)
        {
            PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.Heal(healReward);
            }
        }

        Destroy(gameObject);
    }
}
