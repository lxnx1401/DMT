using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int RequiredCrowns => LevelManager.Instance?.CurrentLevel?.crownAmount ?? 0;

    private bool IsEndlessMode => LevelManager.Instance != null && LevelManager.Instance.IsEndlessMode;

    private int collectedCrowns = 0;

    // Gesamtzahl der Kronen im laufenden Run - im Endlos-Modus die Score, die am Ende
    // an HighscoreManager übergeben wird. Der Fortschritt der Welt läuft dort rein über
    // die Kachel-Distanz zum Spieler, nicht mehr über einen Kronen-Zähler.
    private int totalCrownsThisRun = 0;

    private LevelCompletedLerp levelCompletedMenu;

    public bool IsGameOver { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void LevelRestart()
    {
        collectedCrowns = 0;
        totalCrownsThisRun = 0;
        IsGameOver = false;
    }

    public void StartNewGame()
    {
        collectedCrowns = 0;
        totalCrownsThisRun = 0;
        IsGameOver = false;
    }

    public void CollectCrown()
    {
        if (IsGameOver)
            return;

        collectedCrowns++;
        totalCrownsThisRun++;

        if (IsEndlessMode)
            return;

        if (collectedCrowns >= RequiredCrowns)
            Win();
    }

    void Win()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        LevelManager.Instance?.UnlockNextLevel();
        levelCompletedMenu = FindFirstObjectByType<LevelCompletedLerp>();

        if (levelCompletedMenu != null)
            levelCompletedMenu.TriggerLevelCompleted();
        else
            Debug.LogError("Kein LevelCompletedLerp in der aktiven Szene gefunden.", this);
    }

    public void Lose()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;

        if (IsEndlessMode)
            HighscoreManager.TrySubmitEndlessScore(totalCrownsThisRun);

        levelCompletedMenu = FindFirstObjectByType<LevelCompletedLerp>();

        if (levelCompletedMenu != null)
            levelCompletedMenu.TriggerLevelLost();
        else
        {
            Debug.LogError("Kein LevelCompletedLerp für den Lose-Zustand gefunden.", this);
            Time.timeScale = 0f;
        }
    }
}
