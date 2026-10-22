using System.Collections.Generic;
using UnityEngine;

// Finite purification encounters used only by the two experimental scenes.
// Templates stay inactive; every failed attempt receives fresh, healthy clones.
public sealed class DesignLabEncounter : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform retryPoint;
    [SerializeField] private Vector3 checkpointPosition;
    [SerializeField] private Vector2 triggerMinimum;
    [SerializeField] private Vector2 triggerMaximum;
    [SerializeField] private GameObject[] firstWave = new GameObject[0];
    [SerializeField] private GameObject[] secondWave = new GameObject[0];
    [SerializeField] private GameObject exitBarrier;
    [SerializeField, Min(0.1f)] private float waveBreak = 1.2f;
    [SerializeField, Min(0)] private int completionFaith = 6;

    private readonly List<GhostHealth> living = new List<GhostHealth>();
    private readonly List<GameObject> spawned = new List<GameObject>();
    private bool running;
    private bool waiting;
    private int wave;
    private float nextWaveAt;
    private float attemptStart;
    private string spawnPrefix;
    public bool Completed { get; private set; }
    public bool Running => running;
    public int CurrentWave => wave;

    private void Awake()
    {
        spawnPrefix = name + "_Encounter_";
        if (!gameObject.scene.name.EndsWith("_DesignLab"))
        {
            if (exitBarrier != null) exitBarrier.SetActive(false);
            enabled = false;
            return;
        }
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (!ValidTemplates(firstWave) || !ValidTemplates(secondWave))
        {
            Debug.LogError("DesignLab encounter has invalid enemy templates: " + name, this);
            if (exitBarrier != null) exitBarrier.SetActive(false);
            enabled = false;
            return;
        }
        SetTemplatesInactive(firstWave);
        SetTemplatesInactive(secondWave);
        if (exitBarrier != null) exitBarrier.SetActive(true);
    }

    private void OnEnable() => GameManager.RetryCompleted += ResetAttempt;
    private void OnDisable()
    {
        GameManager.RetryCompleted -= ResetAttempt;
        CleanupAttempt();
        if (exitBarrier != null) exitBarrier.SetActive(false);
    }

    private static bool ValidTemplates(GameObject[] templates)
    {
        if (templates == null || templates.Length == 0 || templates.Length > 3) return false;
        foreach (GameObject template in templates)
            if (template == null || template.GetComponent<GhostHealth>() == null) return false;
        return true;
    }

    private static void SetTemplatesInactive(GameObject[] templates)
    {
        foreach (GameObject template in templates) template.SetActive(false);
    }

    private void Update()
    {
        if (Completed || player == null || !player.ControlsEnabled) return;
        if (!running)
        {
            Vector2 position = player.transform.position;
            if (position.x < triggerMinimum.x || position.x > triggerMaximum.x
                || position.y < triggerMinimum.y || position.y > triggerMaximum.y) return;
            running = true;
            attemptStart = Time.unscaledTime;
            // This changes only the in-memory StartPoint referenced by GameManager.Retry.
            if (retryPoint != null) retryPoint.position = checkpointPosition;
            SpawnWave(firstWave, 1);
            return;
        }

        if (waiting)
        {
            if (Time.time >= nextWaveAt)
            {
                waiting = false;
                SpawnWave(secondWave, 2);
            }
            return;
        }
        foreach (GhostHealth enemy in living)
            if (enemy != null && !enemy.IsDead) return;

        if (wave == 1)
        {
            waiting = true;
            nextWaveAt = Time.time + Mathf.Max(0.1f, waveBreak);
        }
        else
        {
            Completed = true;
            running = false;
            if (exitBarrier != null) exitBarrier.SetActive(false);
            // Individual clones award zero; the room reward cannot be farmed by retrying.
            GameManager.Instance?.AddFaithPoints(completionFaith);
            Debug.Log($"DesignLab completed {name}; attemptSeconds={Time.unscaledTime - attemptStart:F2}", this);
            CleanupAttempt();
        }
    }

    private void SpawnWave(GameObject[] templates, int number)
    {
        living.Clear();
        wave = number;
        foreach (GameObject template in templates)
        {
            GameObject actor = Instantiate(template, template.transform.position,
                template.transform.rotation, transform);
            actor.name = spawnPrefix + number + "_" + spawned.Count;
            spawned.Add(actor);
            actor.SetActive(true);
            living.Add(actor.GetComponent<GhostHealth>());
        }
        Debug.Log($"DesignLab wave {name}/{wave}; actors={living.Count}", this);
    }

    private void ResetAttempt()
    {
        if (Completed) return;
        if (running) Debug.Log("DesignLab retry " + name, this);
        CleanupAttempt();
        running = false;
        waiting = false;
        wave = 0;
        if (exitBarrier != null) exitBarrier.SetActive(true);
    }

    private void CleanupAttempt()
    {
        foreach (GameObject actor in spawned)
        {
            if (actor == null) continue;
            actor.SetActive(false);
            Destroy(actor);
        }
        spawned.Clear();
        living.Clear();
        // Ranged projectiles are not parented to their owners by the existing component.
        if (string.IsNullOrEmpty(spawnPrefix)) return;
        foreach (RangedSpiritProjectile shot in FindObjectsByType<RangedSpiritProjectile>())
        {
            if (shot.gameObject.scene == gameObject.scene && shot.name.StartsWith(spawnPrefix))
            {
                shot.gameObject.SetActive(false);
                Destroy(shot.gameObject);
            }
        }
    }

    private void OnGUI()
    {
        if (!running || Completed) return;
        GUI.Box(new Rect(Screen.width * 0.5f - 100f, 14f, 200f, 28f),
            waiting ? "Purification - next wave" : $"Purification {wave} / 2");
    }
}
