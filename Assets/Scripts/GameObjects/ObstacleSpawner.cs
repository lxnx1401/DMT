using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private LineRenderer worldBoundary;
    [SerializeField, Min(0f)] private float additionalBoundaryPadding = 1f;
    [SerializeField, Min(0.1f)] private float minimumObstacleSpacing = 6f;
    [SerializeField, Min(0f)] private float playerStartClearance = 7f;
    [SerializeField, Range(5, 100)] private int candidatesPerObstacle = 40;

    public int Amount { get; private set; }

    private void Start()
    {
        LevelData level = LevelManager.Instance?.CurrentLevel;
        if (level == null || obstaclePrefab == null)
        {
            Debug.LogWarning("ObstacleSpawner requires level data and an obstacle prefab.", this);
            return;
        }

        float multiplier = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.CurrentTuning.obstacleSpawnMultiplier
            : 1f;
        Amount = level.obstacleAmount > 0
            ? Mathf.Max(1, Mathf.RoundToInt(level.obstacleAmount * multiplier))
            : 0;

        if (Amount == 0)
            return;

        ResolveBoundary();
        Bounds spawnBounds = GetSpawnBounds();
        SpawnDistributedObstacles(spawnBounds);
    }

    private void SpawnDistributedObstacles(Bounds bounds)
    {
        List<Vector2> positions = new List<Vector2>(Amount);
        Vector2 playerStart = ParticleSimulation.Instance != null
            ? ParticleSimulation.Instance.PlayerPosition
            : Vector2.zero;

        int levelIndex = LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 0;
        int seed = 173 + levelIndex * 7919 + Mathf.RoundToInt(
            (DifficultyManager.Instance?.CurrentTuning.difficulty ?? 0.5f) * 100f);
        System.Random random = new System.Random(seed);

        for (int i = 0; i < Amount; i++)
        {
            if (!TryFindBestCandidate(bounds, positions, playerStart, random, out Vector2 position))
            {
                Debug.LogWarning(
                    $"Only {positions.Count} of {Amount} obstacles fit inside the world boundary. " +
                    "Reduce obstacle count or minimum spacing.",
                    this);
                break;
            }

            positions.Add(position);
            Instantiate(obstaclePrefab, position, Quaternion.identity);
        }

        Amount = positions.Count;
    }

    private bool TryFindBestCandidate(
        Bounds bounds,
        List<Vector2> existingPositions,
        Vector2 playerStart,
        System.Random random,
        out Vector2 bestPosition)
    {
        bestPosition = default;
        float bestDistance = -1f;

        for (int attempt = 0; attempt < candidatesPerObstacle; attempt++)
        {
            Vector2 candidate = new Vector2(
                Mathf.Lerp(bounds.min.x, bounds.max.x, (float)random.NextDouble()),
                Mathf.Lerp(bounds.min.y, bounds.max.y, (float)random.NextDouble()));

            float distanceToPlayer = Vector2.Distance(candidate, playerStart);
            if (distanceToPlayer < playerStartClearance)
                continue;

            float closestDistance = distanceToPlayer;
            foreach (Vector2 existing in existingPositions)
                closestDistance = Mathf.Min(closestDistance, Vector2.Distance(candidate, existing));

            if (closestDistance < minimumObstacleSpacing || closestDistance <= bestDistance)
                continue;

            bestDistance = closestDistance;
            bestPosition = candidate;
        }

        return bestDistance >= 0f;
    }

    private void ResolveBoundary()
    {
        if (worldBoundary != null)
            return;

        GameObject boundaryObject = GameObject.Find("WorldBoundary");
        if (boundaryObject != null)
            worldBoundary = boundaryObject.GetComponent<LineRenderer>();
    }

    private Bounds GetSpawnBounds()
    {
        Vector2 minimum = new Vector2(-20f, -20f);
        Vector2 maximum = new Vector2(20f, 20f);

        if (worldBoundary != null && worldBoundary.positionCount > 0)
        {
            Vector3 first = worldBoundary.GetPosition(0);
            if (!worldBoundary.useWorldSpace)
                first = worldBoundary.transform.TransformPoint(first);

            minimum = first;
            maximum = first;

            for (int i = 1; i < worldBoundary.positionCount; i++)
            {
                Vector3 point = worldBoundary.GetPosition(i);
                if (!worldBoundary.useWorldSpace)
                    point = worldBoundary.transform.TransformPoint(point);

                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }
        }

        Obstacle prefabObstacle = obstaclePrefab.GetComponent<Obstacle>();
        float obstacleRadius = prefabObstacle != null ? prefabObstacle.Radius : 2f;
        float padding = obstacleRadius + additionalBoundaryPadding;
        minimum += Vector2.one * padding;
        maximum -= Vector2.one * padding;

        if (minimum.x >= maximum.x || minimum.y >= maximum.y)
        {
            Debug.LogError("World boundary is too small for the obstacle padding.", this);
            return new Bounds(Vector3.zero, Vector3.one * 2f);
        }

        Vector2 size = maximum - minimum;
        return new Bounds((minimum + maximum) * 0.5f, new Vector3(size.x, size.y, 0f));
    }
}
