using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; 
using UnityEngine.SceneManagement; 

public class PauseMenuLerp : MonoBehaviour
{
    [Header("UI Elemente")]
    [SerializeField] private RectTransform menuPanel; 
    
    [Header("Abhängigkeiten")]
    [SerializeField] private LevelCompletedLerp levelCompletedScript; 

    [Header("Animationseinstellungen")]
    [SerializeField] private float animationDuration = 0.4f; 
    
    private float screenRightPos; 
    private float customOpenedPos; 

    private bool isPaused = false;
    private Coroutine activeAnimation;

    void Awake()
    {
        if (menuPanel != null)
        {
            customOpenedPos = menuPanel.anchoredPosition.x;
            screenRightPos = customOpenedPos + Screen.width + menuPanel.rect.width;
            
            Vector2 startPos = menuPanel.anchoredPosition;
            startPos.x = screenRightPos;
            menuPanel.anchoredPosition = startPos;
        }

        if (levelCompletedScript == null)
        {
            levelCompletedScript = FindAnyObjectByType<LevelCompletedLerp>();
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (levelCompletedScript != null && levelCompletedScript.IsLevelCompleted)
            {
                return; 
            }

            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;

        if (activeAnimation != null) StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(AnimateMenu(customOpenedPos));

        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;

        if (activeAnimation != null) StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(AnimateMenu(screenRightPos));

        Time.timeScale = 1f;
    }

    public void OnContinuePressed()
    {
        if (isPaused)
        {
            ResumeGame();
        }
    }

    public void OnBackToMenuPressed(string menuSceneName)
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(menuSceneName);
    }

    private IEnumerator AnimateMenu(float targetXValue)
    {
        float elapsedTime = 0f;
        Vector2 startPosition = menuPanel.anchoredPosition;
        Vector2 targetPosition = new Vector2(targetXValue, startPosition.y);

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float percentageComplete = elapsedTime / animationDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, percentageComplete);

            menuPanel.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, smoothT);
            yield return null;
        }

        menuPanel.anchoredPosition = targetPosition;
    }
}