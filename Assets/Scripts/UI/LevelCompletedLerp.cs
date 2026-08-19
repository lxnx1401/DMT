using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class LevelCompletedLerp : MonoBehaviour
{
    [Header("UI Elemente")]
    [SerializeField] private RectTransform levelCompletedPanel;

    [FormerlySerializedAs("text")]
    [SerializeField] private TMP_Text scoreText;

    [SerializeField] private TMP_Text highscoreText;
    [SerializeField] private GameObject newHighscoreBanner;

    [SerializeField] private GameObject lostImage;
    [SerializeField] private GameObject completedTitle;
    [SerializeField] private GameObject backToMenuButton;
    [SerializeField] private GameObject scoreLabel;

    [Header("Endlos Modus Ergebnis")]
    [SerializeField] private GameObject endlessResultPanel;
    [SerializeField] private TMP_Text endlessScoreText;
    [SerializeField] private GameObject endlessNewHighscoreBanner;

    [Header("Animationseinstellungen")]
    [SerializeField] private float animationDuration = 0.4f;

    private float screenRightPos;
    private float customOpenedPos;

    private bool isLevelCompleted = false;
    private Coroutine activeAnimation;

    public bool IsLevelCompleted => isLevelCompleted;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button nextButton;

    [Header("Audio Einstellungen")]
    [SerializeField] private GameObject musicGameObject;

    void Awake()
    {
        if (newHighscoreBanner != null)
            newHighscoreBanner.SetActive(false);

        if (endlessResultPanel != null)
            endlessResultPanel.SetActive(false);

        if (endlessNewHighscoreBanner != null)
            endlessNewHighscoreBanner.SetActive(false);

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
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);

        if (nextButton != null)
            nextButton.onClick.AddListener(LoadNextLevel);
    }

#if UNITY_EDITOR
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame && !isLevelCompleted)
            TriggerLevelCompleted();
    }
#endif

    public void TriggerLevelCompleted()
    {
        ShowResult(false);
    }

    public void TriggerLevelLost()
    {
        ShowResult(true);
    }

    private void ShowResult(bool hasLost)
    {
        if (isLevelCompleted)
            return;

        isLevelCompleted = true;

        int activeParticles = ParticleSimulation.Instance != null
            ? ParticleSimulation.Instance.ActiveParticles
            : 0;

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(!hasLost);
            scoreText.text = activeParticles.ToString("000");
        }
        else
            Debug.LogWarning("Level completed score text is not assigned.", this);

        if (scoreLabel != null)
            scoreLabel.SetActive(!hasLost);

        if (!hasLost && LevelManager.Instance != null)
        {
            int levelIndex = LevelManager.Instance.currentLevel;
            bool isNewHighscore = HighscoreManager.TrySubmitScore(levelIndex, activeParticles);
            int highscore = HighscoreManager.GetHighscore(levelIndex);

            if (highscoreText != null)
            {
                highscoreText.gameObject.SetActive(true);
                highscoreText.text = "Best: " + highscore.ToString("000");
            }

            if (newHighscoreBanner != null)
                newHighscoreBanner.SetActive(isNewHighscore);
        }
        else
        {
            if (highscoreText != null)
                highscoreText.gameObject.SetActive(false);

            if (newHighscoreBanner != null)
                newHighscoreBanner.SetActive(false);
        }

        if (completedTitle != null)
            completedTitle.SetActive(!hasLost);
        
        if (lostImage != null)
        {
            lostImage.SetActive(hasLost);
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(!hasLost);
            nextButton.interactable = !hasLost && LevelManager.Instance != null &&
                LevelManager.Instance.HasNextLevel;
        }

        if (backToMenuButton != null)
            backToMenuButton.SetActive(true);

        if (levelCompletedPanel == null)
        {
            Debug.LogError("Level completed panel is not assigned.", this);
            Time.timeScale = 0f;
            return;
        }

        if (activeAnimation != null)
        {
            StopCoroutine(activeAnimation);
            activeAnimation = null;
        }

        activeAnimation =
            StartCoroutine(AnimateMenu(customOpenedPos));

        Time.timeScale = 0f;
    }

    public void TriggerEndlessResult(int score, bool isNewHighscore)
    {
        if (isLevelCompleted)
            return;

        isLevelCompleted = true;

        if (endlessScoreText != null)
            endlessScoreText.text = score.ToString("000");

        if (endlessNewHighscoreBanner != null)
            endlessNewHighscoreBanner.SetActive(isNewHighscore);

        if (endlessResultPanel != null)
            endlessResultPanel.SetActive(true);

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

        if (MusicManager.Instance != null)
        {
            Destroy(MusicManager.Instance.gameObject);
        }

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

    private void RestartLevel()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.RestartLevel();
    }

    private void LoadNextLevel()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadNextLevel();
    }
}