using UnityEngine;

public class CaveRewardReveal : MonoBehaviour
{
    [SerializeField] private GameObject hiddenReward;
    [SerializeField] private GameObject[] blockingObjects;
    [SerializeField] private float revealDistance = 7f;

    private Transform playerTransform;
    private bool revealed;

    public bool IsConfigured => hiddenReward != null
        && blockingObjects != null
        && blockingObjects.Length > 0;

    private void Awake()
    {
        HideReward();
        ResolvePlayer();
    }

    private void Update()
    {
        if (revealed || hiddenReward == null || !AreAllBlockersBroken())
        {
            return;
        }

        if (playerTransform == null)
        {
            ResolvePlayer();
        }

        if (playerTransform == null)
        {
            return;
        }

        float distanceSqr = ((Vector2)playerTransform.position - (Vector2)transform.position).sqrMagnitude;

        if (distanceSqr <= revealDistance * revealDistance)
        {
            revealed = true;
            hiddenReward.SetActive(true);
            enabled = false;
        }
    }

    public void Configure(GameObject reward, GameObject[] blockers, float distance)
    {
        hiddenReward = reward;
        blockingObjects = blockers;
        revealDistance = Mathf.Max(0.5f, distance);
        revealed = false;
        HideReward();
    }

    private bool AreAllBlockersBroken()
    {
        if (blockingObjects == null || blockingObjects.Length == 0)
        {
            return false;
        }

        foreach (GameObject blocker in blockingObjects)
        {
            if (blocker != null)
            {
                return false;
            }
        }

        return true;
    }

    private void HideReward()
    {
        if (hiddenReward != null)
        {
            hiddenReward.SetActive(false);
        }
    }

    private void ResolvePlayer()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        playerTransform = player != null ? player.transform : null;
    }
}
