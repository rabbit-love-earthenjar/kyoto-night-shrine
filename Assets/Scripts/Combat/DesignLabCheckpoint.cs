using UnityEngine;

// One-use approach checkpoint for experimental traversal sections.
public sealed class DesignLabCheckpoint : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform retryPoint;
    [SerializeField] private Vector2 triggerMinimum;
    [SerializeField] private Vector2 triggerMaximum;
    [SerializeField] private Vector3 checkpointPosition;

    private void Awake()
    {
        enabled = gameObject.scene.name.EndsWith("_DesignLab") && retryPoint != null;
        if (player == null) player = FindAnyObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (player == null || !player.ControlsEnabled) return;
        Vector2 position = player.transform.position;
        if (position.x < triggerMinimum.x || position.x > triggerMaximum.x
            || position.y < triggerMinimum.y || position.y > triggerMaximum.y) return;

        // Leave completed encounters open; only shorten the next failure's return trip.
        retryPoint.position = checkpointPosition;
        Debug.Log("DesignLab checkpoint: " + name, this);
        enabled = false;
    }
}
