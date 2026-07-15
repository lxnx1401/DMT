using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; 
using UnityEngine.SceneManagement; 


public class LevelCompletedLerp : MonoBehaviour
{
    [Header("UI Elemente")]
    [SerializeField] private RectTransform levelCompletedPanel; 

    [Header("Animationseinstellungen")]
    [SerializeField] private float animationDuration = 0.4f; 
    
    private float screenRightPos; 
    private float customOpenedPos; 

    private bool isLevelCompleted = false;
    private Coroutine activeAnimation;

    public bool IsLevelCompleted => isLevelCompleted; 

    void Awake()
    {
        if (levelCompletedPanel != null)
        {
            customOpenedPos = levelCompletedPanel.anchoredPosition.x;
            screenRightPos = customOpenedPos + Screen.width + levelCompletedPanel.rect.width;
            
            Vector2 startPos = levelCompletedPanel.anchoredPosition;
            startPos.x = screenRightPos;
            levelCompletedPanel.anchoredPosition = startPos;
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame)
        {
            if (!isLevelCompleted)
            {
                TriggerLevelCompleted();
            }
        }
    }

    public void TriggerLevelCompleted()
    {
        isLevelCompleted = true;

        if (activeAnimation != null) StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(AnimateMenu(customOpenedPos));

        Time.timeScale = 0f; 
    }

    public void HideLevelCompleted()
    {
        isLevelCompleted = false;

        if (activeAnimation != null) StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(AnimateMenu(screenRightPos));

        Time.timeScale = 1f;
    }

    public void OnBackToMenuPressed(string menuSceneName)
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(menuSceneName);
    }
    private IEnumerator AnimateMenu(float targetXValue)
    {
        float elapsedTime = 0f;
        Vector2 startPosition = levelCompletedPanel.anchoredPosition;
        Vector2 targetPosition = new Vector2(targetXValue, startPosition.y);

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float percentageComplete = elapsedTime / animationDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, percentageComplete);

            levelCompletedPanel.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, smoothT);
            yield return null;
        }

        levelCompletedPanel.anchoredPosition = targetPosition;
    }
}