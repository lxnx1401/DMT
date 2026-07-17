using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    private void Awake()
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

    public void RestartLevel()
    {
        Debug.Log("Restart requested");
        GameManager.Instance?.LevelRestart();
        Time.timeScale = 1f;

        CenterMouseCursor();

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    public void LoadNextLevel()
    {
        if (LevelManager.Instance == null || !LevelManager.Instance.TryNextLevel())
        {
            Debug.LogWarning("Kein weiteres Level verfügbar.", this);
            return;
        }

        Time.timeScale = 1f;
        GameManager.Instance?.LevelRestart();
        CenterMouseCursor();

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    private static void CenterMouseCursor()
    {
        if (Mouse.current != null)
            Mouse.current.WarpCursorPosition(
                new Vector2(Screen.width / 2f, Screen.height / 2f));
    }
}
