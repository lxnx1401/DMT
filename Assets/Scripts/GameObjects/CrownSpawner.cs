using UnityEngine;


public class CrownSpawner : MonoBehaviour
{
    public GameObject crownPrefab;

    public int amount;


    void Start()
    {
        amount = LevelManager.Instance.CurrentLevel.crownAmount;
        for (int i = 0; i < amount; i++)
        {
            Vector2 position =
                new Vector2(
                    (i + 0f) * 5f,
                    0
                );


            Instantiate(
                crownPrefab,
                position,
                Quaternion.identity
            );
        }
    }
}