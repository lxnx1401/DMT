using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public LevelData[] levels;

    public int currentLevel = 0;


    public LevelData CurrentLevel
    {
        get
        {
            if (levels == null || levels.Length <= currentLevel)
            {
                Debug.LogError(
                    "Kein LevelData für Index " + currentLevel
                );

                return null;
            }

            return levels[currentLevel];
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void NextLevel()
    {
        currentLevel++;
        Debug.Log("Neues Level: " + currentLevel);
    }
}