using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public void RestartLevel()
    {
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
}