using System.Collections;
using UnityEngine;

public class GhostSpawner : MonoBehaviour
{
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private Transform[] spawnPoints = new Transform[0];
    [SerializeField] private int spawnCount = 3;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private bool spawnOnStart = true;

    private Coroutine spawnRoutine;

    private void Start()
    {
        if (spawnOnStart)
        {
            spawnRoutine = StartCoroutine(SpawnRoutine());
        }
    }

    public void SpawnGhosts()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        int count = Mathf.Max(0, spawnCount);

        for (int i = 0; i < count; i++)
        {
            SpawnAtPoint(i);

            if (spawnInterval > 0f && i < count - 1)
            {
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        spawnRoutine = null;
    }

    private void SpawnAtPoint(int index)
    {
        if (ghostPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        Transform spawnPoint = spawnPoints[index % spawnPoints.Length];

        if (spawnPoint == null)
        {
            return;
        }

        GameObject ghost = Instantiate(ghostPrefab, spawnPoint.position, spawnPoint.rotation, transform);
        ghost.name = $"SpawnedGhost_{index + 1:00}";
    }
}
