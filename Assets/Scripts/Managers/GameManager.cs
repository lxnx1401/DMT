using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;


    public int requiredCrowns = 5;

    private int collectedCrowns = 0;
    [SerializeField] private LevelCompletedLerp levelCompletedMenu;


    void Awake()
    {
        Instance = this;
    }


    public void CollectCrown()
    {
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
        Debug.Log("GEWONNEN!");
        levelCompletedMenu.TriggerLevelCompleted();
    }
}