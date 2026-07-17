using UnityEngine;

[DefaultExecutionOrder(-200)]
public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab;

    public int amount;


    void Start()
    {
        int baseAmount = LevelManager.Instance.CurrentLevel.obstacleAmount;
        float multiplier = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.CurrentTuning.obstacleSpawnMultiplier
            : 1f;
        amount = baseAmount > 0
            ? Mathf.Max(1, Mathf.RoundToInt(baseAmount * multiplier))
            : 0;
        for (int i = 0; i < amount; i++)
        {
            Vector2 position =
                new Vector2(
                    (i + 0f) * 10f,
                    7f
                );


            Instantiate(
                obstaclePrefab,
                position,
                Quaternion.identity
            );
        }
    }
}
