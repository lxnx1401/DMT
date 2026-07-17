using System.Collections;
using UnityEngine;


[DefaultExecutionOrder(200)]
public class CrownSpawner : MonoBehaviour
{
    public GameObject crownPrefab;

    public int amount;


    private IEnumerator Start()
    {
        // ObstacleSpawner runs first. Waiting one frame also makes this robust if
        // obstacles are later created by another Start method.
        yield return null;

        amount = LevelManager.Instance.CurrentLevel.crownAmount;
        Obstacle[] obstacles = FindObjectsByType<Obstacle>(FindObjectsSortMode.None);
        float clearance = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.CurrentTuning.coinObstacleClearance
            : 3f;

        for (int i = 0; i < amount; i++)
        {
            Vector2 basePosition = new Vector2(i * 5f, 0f);
            Vector2 position = FindSafePositionNearObstacle(basePosition, obstacles, clearance);
            Instantiate(crownPrefab, position, Quaternion.identity);
        }
    }

    private static Vector2 FindSafePositionNearObstacle(
        Vector2 basePosition,
        Obstacle[] obstacles,
        float clearance)
    {
        if (obstacles == null || obstacles.Length == 0)
            return basePosition;

        Obstacle nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Obstacle obstacle in obstacles)
        {
            if (obstacle == null)
                continue;

            float distance = Vector2.Distance(basePosition, obstacle.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = obstacle;
            }
        }

        if (nearest == null)
            return basePosition;

        Vector2 obstaclePosition = nearest.transform.position;
        Vector2 away = basePosition - obstaclePosition;
        if (away.sqrMagnitude < 0.001f)
            away = Vector2.down;

        // At high difficulty clearance becomes smaller, but the coin always stays
        // outside every obstacle collision radius with an additional pickup margin.
        const float coinSafetyMargin = 1.2f;
        float safeDistance = nearest.Radius + Mathf.Max(0.75f, clearance) + coinSafetyMargin;
        Vector2 candidate = obstaclePosition + away.normalized * safeDistance;

        // Overlapping obstacle influence radii are possible. A few relaxation
        // passes push the coin out without allowing an endless placement loop.
        for (int pass = 0; pass < 4; pass++)
        {
            foreach (Obstacle obstacle in obstacles)
            {
                if (obstacle == null)
                    continue;

                Vector2 center = obstacle.transform.position;
                Vector2 delta = candidate - center;
                float requiredDistance = obstacle.Radius + Mathf.Max(0.75f, clearance) + coinSafetyMargin;
                if (delta.sqrMagnitude >= requiredDistance * requiredDistance)
                    continue;

                if (delta.sqrMagnitude < 0.001f)
                    delta = Vector2.down;
                candidate = center + delta.normalized * requiredDistance;
            }
        }

        return candidate;
    }
}
