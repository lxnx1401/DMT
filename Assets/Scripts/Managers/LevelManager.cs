using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private const string UnlockedLevelsKey = "UnlockedLevels";

    public static LevelManager Instance { get; private set; }

    public int currentLevel = 0;
    public bool IsEndlessMode { get; set; }

    private readonly Dictionary<int, LevelData> generatedLevels = new Dictionary<int, LevelData>();

    public int UnlockedLevelCount => Mathf.Max(1, PlayerPrefs.GetInt(UnlockedLevelsKey, 1));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateRuntimeServices()
    {
        if (FindFirstObjectByType<LevelManager>() != null)
            return;

        GameObject services = new GameObject("Level Services");
        services.AddComponent<LevelManager>();
        DontDestroyOnLoad(services);
    }

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

    // Kompletter Neustart: alle Level-/Endlos-Highscores und der Freischalt-Fortschritt werden
    // gelöscht, danach ist wieder nur Level 1 freigeschaltet. Rührt bewusst keine anderen
    // PlayerPrefs an (z.B. Audio-Einstellungen), im Gegensatz zu einem pauschalen DeleteAll().
    public void ResetProgress()
    {
        HighscoreManager.ResetAll(UnlockedLevelCount);
        PlayerPrefs.DeleteKey(UnlockedLevelsKey);
        PlayerPrefs.Save();
        currentLevel = 0;
    }

    public bool TrySelectLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= UnlockedLevelCount)
            return false;

        currentLevel = levelIndex;
        return true;
    }

    public void UnlockNextLevel()
    {
        int required = currentLevel + 2;
        if (required <= UnlockedLevelCount)
            return;

        PlayerPrefs.SetInt(UnlockedLevelsKey, required);
        PlayerPrefs.Save();
    }
}
