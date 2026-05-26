using UnityEngine;

public class TriggerGhostSpawner : MonoBehaviour
{
    [SerializeField] private GhostSpawner ghostSpawner;
    [SerializeField] private bool triggerOnce = true;

    private bool triggered;

    private void Awake()
    {
        if (ghostSpawner == null)
        {
            ghostSpawner = GetComponentInParent<GhostSpawner>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered && triggerOnce)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        triggered = true;

        if (ghostSpawner != null)
        {
            ghostSpawner.SpawnGhosts();
        }
    }
}
