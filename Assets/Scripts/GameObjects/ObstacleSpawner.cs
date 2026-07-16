using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab;

    public int amount;


    void Start()
    {
        amount = LevelManager.Instance.CurrentLevel.obstacleAmount;
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
