using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void StartGame(string gameplaySceneName)
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Spiel wird beendet..."); 
        Application.Quit(); 
    }
}