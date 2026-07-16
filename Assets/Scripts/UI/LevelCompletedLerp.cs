using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;


public class LevelCompletedLerp : MonoBehaviour
{
    [Header("UI Elemente")]
    [SerializeField] private RectTransform levelCompletedPanel;

    [SerializeField] private TMP_Text text;

    [Header("Animationseinstellungen")]
    [SerializeField] private float animationDuration = 0.4f;

    private float screenRightPos;
    private float customOpenedPos;

    private bool isLevelCompleted = false;
    private Coroutine activeAnimation;

    public bool IsLevelCompleted => isLevelCompleted;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button nextButton;


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

    void Start()
    {
        Debug.Log("UI Start");

        Debug.Log(SceneLoader.Instance);

        restartButton.onClick.AddListener(
            () =>
            {
                Debug.Log("Restart gedrückt");
                SceneLoader.Instance.RestartLevel();
            }
        );
        nextButton.onClick.AddListener(
            () =>
            {
                Debug.Log("Next Level gedrückt");
                SceneLoader.Instance.LoadNextLevel();
            }
        );
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
        text.text = ParticleSimulation.Instance.ActiveParticles.ToString("000");

        if (activeAnimation != null)
        {
            StopCoroutine(activeAnimation);
            activeAnimation = null;
        }

        activeAnimation =
            StartCoroutine(AnimateMenu(customOpenedPos));

        Time.timeScale = 0f;
    }

    public void HideLevelCompleted()
    {
        isLevelCompleted = false;

        if (activeAnimation != null)
        {
            StopCoroutine(activeAnimation);
            activeAnimation = null;
        }

        activeAnimation =
            StartCoroutine(AnimateMenu(screenRightPos));

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