using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedOniPhaseThreeAddsController : MonoBehaviour
{
    [Header("Phase 3 enemies")]
    [SerializeField] private GameObject enemyVisualPrefab;
    [SerializeField] private Sprite projectileSprite;
    [SerializeField] private Transform leftSpawnPoint;
    [SerializeField] private Transform rightSpawnPoint;
    [SerializeField] private Vector2 leftPatrolBounds = new Vector2(-3.8f, -1.2f);
    [SerializeField] private Vector2 rightPatrolBounds = new Vector2(1.2f, 3.8f);
    [SerializeField, Min(1)] private int maxConcurrentEnemies = 2;
    [SerializeField, Min(1)] private int totalSpawnLimit = 4;
    [SerializeField, Min(0f)] private float initialSpawnDelay = 0.45f;
    [SerializeField, Min(0.1f)] private float refillDelay = 3.2f;
    [SerializeField, Min(0.1f)] private float enemyVisualHeight = 1.15f;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private Coroutine spawnRoutine;
    private bool phaseActive;
    private int totalSpawned;
    private int nextSpawnSide;

    public bool IsPhaseActive => phaseActive;
    public int ActiveEnemyCount
    {
        get
        {
            RemoveDestroyedEnemies();
            return activeEnemies.Count;
        }
    }
    public int TotalSpawned => totalSpawned;
    public int MaxConcurrentEnemies => maxConcurrentEnemies;
    public bool IsConfigured => enemyVisualPrefab != null
        && leftSpawnPoint != null
        && rightSpawnPoint != null;

    private void OnEnable()
    {
        GameManager.RetryCompleted += ResetEncounter;
    }

    private void OnDisable()
    {
        GameManager.RetryCompleted -= ResetEncounter;
        StopSpawning();
    }

    public void Configure(
        GameObject visualPrefab,
        Sprite shotSprite,
        Transform leftPoint,
        Transform rightPoint,
        Vector2 leftBounds,
        Vector2 rightBounds)
    {
        enemyVisualPrefab = visualPrefab;
        projectileSprite = shotSprite;
        leftSpawnPoint = leftPoint;
        rightSpawnPoint = rightPoint;
        leftPatrolBounds = SortBounds(leftBounds);
        rightPatrolBounds = SortBounds(rightBounds);
    }

    public void BeginPhaseThree()
    {
        ResetEncounter();
        phaseActive = true;
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void EndPhaseThree()
    {
        phaseActive = false;
        StopSpawning();
        DestroyActiveEnemies();
    }

    public void ResetEncounter()
    {
        phaseActive = false;
        StopSpawning();
        DestroyActiveEnemies();
        totalSpawned = 0;
        nextSpawnSide = 0;
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, initialSpawnDelay));

        while (phaseActive)
        {
            RemoveDestroyedEnemies();

            if (activeEnemies.Count < Mathf.Max(1, maxConcurrentEnemies)
                && totalSpawned < Mathf.Max(maxConcurrentEnemies, totalSpawnLimit))
            {
                SpawnNextEnemy();
                yield return new WaitForSeconds(0.28f);
                continue;
            }

            yield return new WaitForSeconds(Mathf.Max(0.1f, refillDelay));
        }
    }

    private void SpawnNextEnemy()
    {
        if (enemyVisualPrefab == null)
        {
            Debug.LogWarning("Red Oni Phase 3 add spawn skipped: enemy visual prefab is missing.", this);
            phaseActive = false;
            return;
        }

        bool useLeft = nextSpawnSide % 2 == 0;
        Transform spawnPoint = useLeft ? leftSpawnPoint : rightSpawnPoint;
        Vector2 patrolBounds = useLeft ? leftPatrolBounds : rightPatrolBounds;

        if (spawnPoint == null)
        {
            Debug.LogWarning("Red Oni Phase 3 add spawn skipped: a spawn point is missing.", this);
            phaseActive = false;
            return;
        }

        GameObject enemy = Instantiate(
            enemyVisualPrefab,
            spawnPoint.position + Vector3.up * 0.8f,
            Quaternion.identity,
            transform);
        enemy.name = $"Phase3_RangedRunner_{totalSpawned + 1:00}";
        enemy.SetActive(true);
        NormalizeVisualHeight(enemy, enemyVisualHeight);

        SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
        BoxCollider2D collider = enemy.GetComponent<BoxCollider2D>();

        if (collider == null)
        {
            collider = enemy.AddComponent<BoxCollider2D>();
        }

        if (renderer != null && renderer.sprite != null)
        {
            Vector2 spriteSize = renderer.sprite.bounds.size;
            collider.size = new Vector2(spriteSize.x * 0.58f, spriteSize.y * 0.72f);
        }

        collider.isTrigger = true;
        AlignVisualBottomToSurface(enemy, spawnPoint.position.y);

        RangedRunnerEnemy runner = enemy.GetComponent<RangedRunnerEnemy>();

        if (runner == null)
        {
            runner = enemy.AddComponent<RangedRunnerEnemy>();
        }

        if (enemy.GetComponent<GhostHealth>() == null)
        {
            enemy.AddComponent<GhostHealth>();
        }

        runner.ConfigureRoute(
            patrolBounds.x,
            patrolBounds.y,
            enemy.transform.position.y,
            projectileSprite);

        activeEnemies.Add(enemy);
        totalSpawned++;
        nextSpawnSide++;
    }

    private void StopSpawning()
    {
        if (spawnRoutine == null)
        {
            return;
        }

        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    private void DestroyActiveEnemies()
    {
        for (int index = activeEnemies.Count - 1; index >= 0; index--)
        {
            if (activeEnemies[index] != null)
            {
                Destroy(activeEnemies[index]);
            }
        }

        activeEnemies.Clear();
    }

    private void RemoveDestroyedEnemies()
    {
        activeEnemies.RemoveAll(enemy => enemy == null);
    }

    private static Vector2 SortBounds(Vector2 bounds)
    {
        return new Vector2(Mathf.Min(bounds.x, bounds.y), Mathf.Max(bounds.x, bounds.y));
    }

    private static void NormalizeVisualHeight(GameObject target, float targetHeight)
    {
        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer.sprite == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds || bounds.size.y <= 0.01f)
        {
            return;
        }

        float scale = Mathf.Max(0.01f, targetHeight) / bounds.size.y;
        target.transform.localScale *= scale;
    }

    private static void AlignVisualBottomToSurface(GameObject target, float surfaceY)
    {
        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer.sprite == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
        {
            return;
        }

        Vector3 position = target.transform.position;
        position.y += surfaceY - bounds.min.y;
        target.transform.position = position;
    }
}
