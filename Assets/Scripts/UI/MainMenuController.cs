using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void StartGame(string gameplaySceneName)
    {
        Time.timeScale = 1f;

        if (LevelManager.Instance != null)
            LevelManager.Instance.IsEndlessMode = true;
        GameManager.Instance?.StartNewGame();

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void ContinueGame(string gameplaySceneName)
    {
        Time.timeScale = 1f;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.IsEndlessMode = false;
            LevelManager.Instance.TrySelectLevel(FindNextIncompleteLevel());
        }
        GameManager.Instance?.StartNewGame();

        SceneManager.LoadScene(gameplaySceneName);
    }

    private int FindNextIncompleteLevel()
    {
        int unlockedCount = LevelManager.Instance != null
            ? LevelManager.Instance.UnlockedLevelCount
            : 1;

        for (int i = 0; i < unlockedCount; i++)
        {
            if (HighscoreManager.GetHighscore(i) == 0)
                return i;
        }

        return Mathf.Max(0, unlockedCount - 1);
    }

    public void QuitGame()
    {
        Debug.Log("Spiel wird beendet...");
        Application.Quit();
    }
}
