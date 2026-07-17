using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class CrownSpawner : MonoBehaviour
{
    [SerializeField] private GameObject crownPrefab;
    [SerializeField] private LineRenderer worldBoundary;
    [SerializeField, Min(0f)] private float additionalBoundaryPadding = 0.75f;
    [SerializeField, Min(0.1f)] private float minimumCrownSpacing = 3f;
    [SerializeField, Min(0f)] private float minimumObstacleClearance = 0.5f;
    [SerializeField, Min(0f)] private float playerStartClearance = 3f;
    [SerializeField, Range(20, 300)] private int candidatesPerCrown = 150;

    public int Amount { get; private set; }

    private IEnumerator Start()
    {
        // Obstacles must exist before crown positions can be validated.
        yield return null;

        LevelData level = LevelManager.Instance?.CurrentLevel;
        if (level == null || crownPrefab == null)
        {
            Debug.LogWarning("CrownSpawner requires level data and a crown prefab.", this);
            yield break;
        }

        Amount = Mathf.Max(0, level.crownAmount);
        if (Amount == 0)
            yield break;

        ResolveBoundary();
        Obstacle[] obstacles = FindObjectsByType<Obstacle>(FindObjectsSortMode.None);
        float desiredObstacleClearance = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.CurrentTuning.coinObstacleClearance
            : 3f;
        float crownRadius = GetCrownRadius();
        Bounds bounds = GetSpawnBounds(crownRadius);

        SpawnCrowns(bounds, obstacles, crownRadius, desiredObstacleClearance);
    }

    private void SpawnCrowns(
        Bounds bounds,
        Obstacle[] obstacles,
        float crownRadius,
        float desiredObstacleClearance)
    {
        List<Vector2> positions = new List<Vector2>(Amount);
        Vector2 playerStart = ParticleSimulation.Instance != null
            ? ParticleSimulation.Instance.PlayerPosition
            : Vector2.zero;
        int levelIndex = LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 0;
        int seed = 947 + levelIndex * 3571 + Mathf.RoundToInt(
            (DifficultyManager.Instance?.CurrentTuning.difficulty ?? 0.5f) * 100f);
        System.Random random = new System.Random(seed);

        for (int i = 0; i < Amount; i++)
        {
            if (!TryFindCrownPosition(
                    bounds,
                    obstacles,
                    positions,
                    playerStart,
                    crownRadius,
                    desiredObstacleClearance,
                    random,
                    out Vector2 position))
            {
                Debug.LogWarning(
                    $"Only {positions.Count} of {Amount} crowns fit safely inside the world boundary.",
                    this);
                break;
            }

            positions.Add(position);
            Instantiate(crownPrefab, position, Quaternion.identity);
        }

        Amount = positions.Count;
    }

    private bool TryFindCrownPosition(
        Bounds bounds,
        Obstacle[] obstacles,
        List<Vector2> existingCrowns,
        Vector2 playerStart,
        float crownRadius,
        float desiredObstacleClearance,
        System.Random random,
        out Vector2 bestPosition)
    {
        bestPosition = default;
        float bestScore = float.NegativeInfinity;

        for (int attempt = 0; attempt < candidatesPerCrown; attempt++)
        {
            Vector2 candidate = new Vector2(
                Mathf.Lerp(bounds.min.x, bounds.max.x, (float)random.NextDouble()),
                Mathf.Lerp(bounds.min.y, bounds.max.y, (float)random.NextDouble()));

            if (Vector2.Distance(candidate, playerStart) < playerStartClearance)
                continue;

            float nearestCrownDistance = float.MaxValue;
            foreach (Vector2 existing in existingCrowns)
                nearestCrownDistance = Mathf.Min(
                    nearestCrownDistance,
                    Vector2.Distance(candidate, existing));

            if (nearestCrownDistance < minimumCrownSpacing)
                continue;

            float nearestSurfaceClearance = float.MaxValue;
            bool overlapsObstacle = false;

            foreach (Obstacle obstacle in obstacles)
            {
                if (obstacle == null)
                    continue;

                float surfaceClearance = Vector2.Distance(candidate, obstacle.transform.position) -
                    obstacle.Radius - crownRadius;
                if (surfaceClearance < minimumObstacleClearance)
                {
                    overlapsObstacle = true;
                    break;
                }

                nearestSurfaceClearance = Mathf.Min(nearestSurfaceClearance, surfaceClearance);
            }

            if (overlapsObstacle)
                continue;

            // Prefer the requested adaptive distance while still spreading crowns.
            float proximityScore = nearestSurfaceClearance < float.MaxValue
                ? -Mathf.Abs(nearestSurfaceClearance - desiredObstacleClearance)
                : 0f;
            float spacingScore = existingCrowns.Count > 0
                ? Mathf.Min(nearestCrownDistance, 10f) * 0.1f
                : 0f;
            float score = proximityScore + spacingScore;

            if (score <= bestScore)
                continue;

            bestScore = score;
            bestPosition = candidate;
        }

        return !float.IsNegativeInfinity(bestScore);
    }

    private float GetCrownRadius()
    {
        CircleCollider2D collider = crownPrefab.GetComponent<CircleCollider2D>();
        if (collider == null)
            return 1f;

        float scale = Mathf.Max(
            Mathf.Abs(crownPrefab.transform.localScale.x),
            Mathf.Abs(crownPrefab.transform.localScale.y));
        return collider.radius * scale;
    }

    private void ResolveBoundary()
    {
        if (worldBoundary != null)
            return;

        GameObject boundaryObject = GameObject.Find("WorldBoundary");
        if (boundaryObject != null)
            worldBoundary = boundaryObject.GetComponent<LineRenderer>();
    }

    private Bounds GetSpawnBounds(float crownRadius)
    {
        Vector2 minimum = new Vector2(-20f, -20f);
        Vector2 maximum = new Vector2(20f, 20f);

        if (worldBoundary != null && worldBoundary.positionCount > 0)
        {
            Vector3 first = GetWorldBoundaryPoint(0);
            minimum = first;
            maximum = first;

            for (int i = 1; i < worldBoundary.positionCount; i++)
            {
                Vector3 point = GetWorldBoundaryPoint(i);
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }
        }

        float padding = crownRadius + additionalBoundaryPadding;
        minimum += Vector2.one * padding;
        maximum -= Vector2.one * padding;

        if (minimum.x >= maximum.x || minimum.y >= maximum.y)
        {
            Debug.LogError("World boundary is too small for crown placement.", this);
            return new Bounds(Vector3.zero, Vector3.one * 2f);
        }

        Vector2 size = maximum - minimum;
        return new Bounds((minimum + maximum) * 0.5f, new Vector3(size.x, size.y, 0f));
    }

    private Vector3 GetWorldBoundaryPoint(int index)
    {
        Vector3 point = worldBoundary.GetPosition(index);
        return worldBoundary.useWorldSpace
            ? point
            : worldBoundary.transform.TransformPoint(point);
    }
}
