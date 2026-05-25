using UnityEngine;

public class EndGateTrigger : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    private bool triggered;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCompleteLevel(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCompleteLevel(other);
    }

    private void TryCompleteLevel(Collider2D other)
    {
        if (triggered || other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        triggered = true;

        if (gameManager != null)
        {
            gameManager.ShowStageClear();
        }
    }
}
