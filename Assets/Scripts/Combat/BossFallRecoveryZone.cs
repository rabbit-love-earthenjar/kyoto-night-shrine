using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossFallRecoveryZone : MonoBehaviour
{
    [SerializeField] private Transform[] recoveryPoints;
    [SerializeField, Min(0f)] private float controlLockSeconds = 0.18f;
    [SerializeField, Min(0)] private int fallDamage = 1;

    private bool recovering;

    public void Configure(Transform[] points, float lockSeconds = 0.18f, int damage = 1)
    {
        recoveryPoints = points;
        controlLockSeconds = Mathf.Max(0f, lockSeconds);
        fallDamage = Mathf.Max(0, damage);
    }

    private void Awake()
    {
        Collider2D zoneCollider = GetComponent<Collider2D>();
        zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (recovering)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null || (GameManager.Instance != null && GameManager.Instance.IsBlockingUiVisible))
        {
            return;
        }

        StartCoroutine(RecoverPlayer(player));
    }

    private IEnumerator RecoverPlayer(PlayerController player)
    {
        recovering = true;
        player.SetControlEnabled(false);

        PlayerHealth health = player.GetComponent<PlayerHealth>();

        // A boss hit already starts invincibility. Do not charge a second HP
        // when that same hit knocks the player into the recovery zone.
        if (health != null && fallDamage > 0 && !health.IsInvincible)
        {
            health.TakeDamage(fallDamage, player.transform.position);
        }

        if (health != null && health.CurrentHP <= 0)
        {
            recovering = false;
            yield break;
        }

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.simulated = true;
            body.linearVelocity = Vector2.zero;
        }

        Transform destination = ChooseRecoveryPoint();

        if (destination != null)
        {
            player.transform.position = destination.position;
        }

        player.ResetMotion();

        if (controlLockSeconds > 0f)
        {
            yield return new WaitForSeconds(controlLockSeconds);
        }

        player.SetControlEnabled(true);
        recovering = false;
    }

    private Transform ChooseRecoveryPoint()
    {
        if (recoveryPoints == null || recoveryPoints.Length == 0)
        {
            Debug.LogWarning("Boss fall recovery has no safe destination configured.", this);
            return null;
        }

        int startIndex = Random.Range(0, recoveryPoints.Length);

        for (int offset = 0; offset < recoveryPoints.Length; offset++)
        {
            Transform candidate = recoveryPoints[(startIndex + offset) % recoveryPoints.Length];

            if (candidate != null)
            {
                return candidate;
            }
        }

        return null;
    }
}
