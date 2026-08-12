using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class RangedEnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject rangedEnemyPrefab;
    [SerializeField, Min(0f)] private float additionalBoundaryPadding = 1f;
    [SerializeField, Min(0.1f)] private float minimumSpacing = 10f;
    [SerializeField, Min(0f)] private float playerStartClearance = 9f;
    [SerializeField, Range(5, 100)] private int candidatesPerEnemy = 40;

    public int Amount { get; private set; }

    private void Start()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.IsEndlessMode)
            return;

        int levelIndex = LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 0;
        SpawnFor(LevelManager.Instance?.CurrentLevel, levelIndex);
    }

    public void ClearSpawned()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        Amount = 0;
    }

    public void SpawnFor(LevelData level, int seedIndex)
    {
        if (level == null || rangedEnemyPrefab == null)
            return;

        Amount = Mathf.Max(0, level.rangedEnemyAmount);
        if (Amount == 0)
            return;

        Bounds spawnBounds = GetSpawnBounds(level.PlayAreaBounds);
        SpawnEnemies(spawnBounds, seedIndex);
    }

    private void SpawnEnemies(Bounds bounds, int seedIndex)
    {
        List<Vector2> positions = new List<Vector2>(Amount);
        Vector2 playerStart = ParticleSimulation.Instance != null
            ? ParticleSimulation.Instance.PlayerPosition
            : Vector2.zero;

        int seed = 5813 + seedIndex * 2749;
        System.Random random = new System.Random(seed);

        for (int i = 0; i < Amount; i++)
        {
            if (!TryFindPosition(bounds, positions, playerStart, random, out Vector2 position))
            {
                Debug.LogWarning(
                    $"Only {positions.Count} of {Amount} ranged enemies fit inside the world boundary.",
                    this);
                break;
            }

            positions.Add(position);
            Instantiate(rangedEnemyPrefab, position, Quaternion.identity, transform);
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

        for (int attempt = 0; attempt < candidatesPerEnemy; attempt++)
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

            if (closestDistance < minimumSpacing || closestDistance <= bestDistance)
                continue;

            bestDistance = closestDistance;
            bestPosition = candidate;
        }

        return bestDistance >= 0f;
    }

    private Bounds GetSpawnBounds(Bounds playAreaBounds)
    {
        Vector2 minimum = playAreaBounds.min;
        Vector2 maximum = playAreaBounds.max;
        minimum += Vector2.one * additionalBoundaryPadding;
        maximum -= Vector2.one * additionalBoundaryPadding;

        if (minimum.x >= maximum.x || minimum.y >= maximum.y)
        {
            Debug.LogError("World boundary is too small for ranged enemy placement.", this);
            return new Bounds(Vector3.zero, Vector3.one * 2f);
        }

        Vector2 size = maximum - minimum;
        return new Bounds((minimum + maximum) * 0.5f, new Vector3(size.x, size.y, 0f));
    }
}
