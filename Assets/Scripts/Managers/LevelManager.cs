using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public LevelData[] levels;

    public int currentLevel = 0;


    public LevelData CurrentLevel
    {
        get
        {
            if (levels == null || currentLevel < 0 || levels.Length <= currentLevel)
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
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool HasNextLevel => levels != null && currentLevel + 1 < levels.Length;

    public bool TryNextLevel()
    {
        if (!HasNextLevel)
            return false;

        currentLevel++;
        Debug.Log("Neues Level: " + currentLevel);
        return true;
    }
}
