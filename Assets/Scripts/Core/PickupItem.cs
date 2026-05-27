using UnityEngine;

public class PickupItem : MonoBehaviour
{
    private enum PickupType
    {
        Heart,
        FaithPoint,
        StarSeal
    }

    [SerializeField] private PickupType pickupType = PickupType.Heart;
    [SerializeField] private int healAmount = 1;
    [SerializeField] private int faithPointAmount = 1;
    [SerializeField] private int starSealAmount = 1;
    [SerializeField] private GameManager gameManager;

    private bool collected;

    private void Awake()
    {
        ResolveGameManager();
    }

    public void ConfigureStarSeal(GameManager targetGameManager, int amount)
    {
        pickupType = PickupType.StarSeal;
        starSealAmount = Mathf.Max(1, amount);
        gameManager = targetGameManager;
        collected = false;

        ResolveGameManager();
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

        if (pickupType == PickupType.Heart)
        {
            collected = true;
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.Heal(healAmount);
            }

            GameAudio.PlayCollectHeart();
        }
        else if (pickupType == PickupType.FaithPoint)
        {
            GameManager targetGameManager = ResolveGameManager();

            if (targetGameManager == null)
            {
                return;
            }

            collected = true;
            targetGameManager.AddFaithPoints(faithPointAmount);
            GameAudio.PlayCollectFaithPoint();
        }
        else if (pickupType == PickupType.StarSeal)
        {
            GameManager targetGameManager = ResolveGameManager();

            if (targetGameManager == null)
            {
                return;
            }

            collected = true;
            targetGameManager.AddStarSeals(starSealAmount);
            GameAudio.PlayCollectStarSeal();
        }

        Destroy(gameObject);
    }

    private GameManager ResolveGameManager()
    {
        if (gameManager != null)
        {
            return gameManager;
        }

        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }

        return gameManager;
    }
}
