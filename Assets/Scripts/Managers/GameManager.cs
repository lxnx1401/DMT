using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int RequiredCrowns => LevelManager.Instance?.CurrentLevel?.crownAmount ?? 0;

    private int collectedCrowns = 0;
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
        IsGameOver = false;
    }

    public void StartNewGame()
    {
        collectedCrowns = 0;
        IsGameOver = false;
    }


    public void CollectCrown()
    {
        if (IsGameOver)
            return;

        Debug.Log(
        "CollectCrown von: "
        + gameObject.GetInstanceID()
    );
        collectedCrowns++;

        Debug.Log(
            "Kronen: "
            + collectedCrowns
            + "/"
            + RequiredCrowns
        );


        if (collectedCrowns >= RequiredCrowns)
        {
            Win();
        }
    }

    void Win()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        LevelManager.Instance?.UnlockNextLevel();
        levelCompletedMenu = FindFirstObjectByType<LevelCompletedLerp>();
        Debug.Log("GEWONNEN!");

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
