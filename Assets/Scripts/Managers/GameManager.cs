using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;


    public int requiredCrowns;

    private int collectedCrowns = 0;
    private LevelCompletedLerp levelCompletedMenu;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void LevelRestart()
    {
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
        levelCompletedMenu.TriggerLevelCompleted();
    }
}