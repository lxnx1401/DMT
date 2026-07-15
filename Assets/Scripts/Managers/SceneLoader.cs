using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    void Awake()
    {
        Instance = this;
    }
    public void RestartLevel()
    {
        Debug.Log("Restart requested");
        GameManager.Instance.LevelRestart();
        Time.timeScale = 1f;

        Mouse.current.WarpCursorPosition(
            new Vector2(
                Screen.width / 2f,
                Screen.height / 2f
            )
        );

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;


        LevelManager.Instance.NextLevel();
        GameManager.Instance.LevelRestart();


        Mouse.current.WarpCursorPosition(
            new Vector2(
                Screen.width / 2f,
                Screen.height / 2f
            )
        );


        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }
}