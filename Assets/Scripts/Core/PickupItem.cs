using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public enum PickupType
    {
        SpiritShard,
        Heart
    }

    [SerializeField] private PickupType pickupType;
    [SerializeField] private int amount = 1;
    [SerializeField] private GameManager gameManager;

    private bool collected;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider2D other)
    {
        if (collected)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null)
        {
            return;
        }

        collected = true;

        if (pickupType == PickupType.SpiritShard)
        {
            if (gameManager != null)
            {
                gameManager.AddSpiritShard(amount);
            }
        }
        else
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.Heal(amount);
            }
        }

        Destroy(gameObject);
    }
}
