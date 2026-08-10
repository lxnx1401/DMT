using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class LaserEnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject laserPrefab;
    [SerializeField, Min(0f)] private float additionalBoundaryPadding = 1f;
    [SerializeField, Min(0.1f)] private float minimumLaserSpacing = 8f;
    [SerializeField, Min(0f)] private float playerStartClearance = 8f;
    [SerializeField, Range(5, 100)] private int candidatesPerLaser = 40;

    public int Amount { get; private set; }

    private void Start()
    {
        LevelData level = LevelManager.Instance?.CurrentLevel;
        if (level == null || laserPrefab == null)
            return;

        Amount = Mathf.Max(0, level.laserAmount);
        if (Amount == 0)
            return;

        Bounds spawnBounds = GetSpawnBounds(level.PlayAreaBounds);
        SpawnLasers(spawnBounds);
    }

    private void SpawnLasers(Bounds bounds)
    {
        List<Vector2> positions = new List<Vector2>(Amount);
        Vector2 playerStart = ParticleSimulation.Instance != null
            ? ParticleSimulation.Instance.PlayerPosition
            : Vector2.zero;

        int levelIndex = LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 0;
        int seed = 6151 + levelIndex * 1543;
        System.Random random = new System.Random(seed);

        for (int i = 0; i < Amount; i++)
        {
            if (!TryFindPosition(bounds, positions, playerStart, random, out Vector2 position))
            {
                Debug.LogWarning(
                    $"Only {positions.Count} of {Amount} laser enemies fit inside the world boundary.",
                    this);
                break;
            }

            positions.Add(position);
            Instantiate(laserPrefab, position, Quaternion.identity);
        }

        Amount = positions.Count;
    }

    private bool TryFindPosition(
        Bounds bounds,
        List<Vector2> existingPositions,
        Vector2 playerStart,
        System.Random random,
        out Vector2 bestPosition)
    {
        bestPosition = default;
        float bestDistance = -1f;

        for (int attempt = 0; attempt < candidatesPerLaser; attempt++)
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

            if (closestDistance < minimumLaserSpacing || closestDistance <= bestDistance)
                continue;

            bestDistance = closestDistance;
            bestPosition = candidate;
        }

        return bestDistance >= 0f;
    }

    private Bounds GetSpawnBounds(Bounds playAreaBounds)
    {
        Vector2 minimum = playAreaBounds.min + Vector2.one * additionalBoundaryPadding;
        Vector2 maximum = playAreaBounds.max - Vector2.one * additionalBoundaryPadding;

        if (minimum.x >= maximum.x || minimum.y >= maximum.y)
        {
            Debug.LogError("World boundary is too small for laser enemy placement.", this);
            return new Bounds(Vector3.zero, Vector3.one * 2f);
        }

        Vector2 size = maximum - minimum;
        return new Bounds((minimum + maximum) * 0.5f, new Vector3(size.x, size.y, 0f));
    }
}
