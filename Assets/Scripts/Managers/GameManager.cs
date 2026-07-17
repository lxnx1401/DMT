using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }


    public int requiredCrowns;

    private int collectedCrowns = 0;
    private LevelCompletedLerp levelCompletedMenu;

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
        if (LevelManager.Instance?.CurrentLevel != null)
            requiredCrowns = LevelManager.Instance.CurrentLevel.crownAmount;
        collectedCrowns = 0;
    }


    public void CollectCrown()
    {
        Debug.Log(
        "CollectCrown von: "
        + gameObject.GetInstanceID()
    );
        collectedCrowns++;

        Debug.Log(
            "Kronen: "
            + collectedCrowns
            + "/"
            + requiredCrowns
        );


        if (collectedCrowns >= requiredCrowns)
        {
            Win();
        }
    }

    void Win()
    {
        levelCompletedMenu = FindFirstObjectByType<LevelCompletedLerp>();
        Debug.Log("GEWONNEN!");

        if (levelCompletedMenu != null)
            levelCompletedMenu.TriggerLevelCompleted();
        else
            Debug.LogError("Kein LevelCompletedLerp in der aktiven Szene gefunden.", this);
    }
}
