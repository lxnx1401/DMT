using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public int currentLevel = 0;

    private readonly Dictionary<int, LevelData> generatedLevels = new Dictionary<int, LevelData>();

    public LevelData CurrentLevel
    {
        get
        {
            if (currentLevel < 0)
            {
                Debug.LogError("Ungültiger Level-Index " + currentLevel);
                return null;
            }

            if (!generatedLevels.TryGetValue(currentLevel, out LevelData data))
            {
                data = LevelGenerator.Generate(currentLevel);
                generatedLevels[currentLevel] = data;
            }

            return data;
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

    public bool HasNextLevel => true;

    public bool TryNextLevel()
    {
        currentLevel++;
        Debug.Log("Neues Level: " + currentLevel);
        return true;
    }

    public void ResetToFirstLevel()
    {
        currentLevel = 0;
    }
}
