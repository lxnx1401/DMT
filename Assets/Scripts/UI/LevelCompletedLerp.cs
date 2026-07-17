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

    private TMP_Text lostTitleText;
    private GameObject completedTitle;
    private GameObject backToMenuButton;
    private GameObject scoreLabel;

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
        ResolveReferences();

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
        ResolveReferences();

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

        if (completedTitle != null)
            completedTitle.SetActive(!hasLost);
        if (lostTitleText != null)
        {
            lostTitleText.gameObject.SetActive(hasLost);
            lostTitleText.text = "You lost";
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

    private void ResolveReferences()
    {
        if (scoreText == null && levelCompletedPanel != null)
            scoreText = levelCompletedPanel.GetComponentInChildren<TMP_Text>(true);

        if (levelCompletedPanel == null)
            return;

        if (completedTitle == null)
        {
            Transform title = levelCompletedPanel.Find("Titel");
            completedTitle = title != null ? title.gameObject : null;
        }

        if (backToMenuButton == null)
        {
            Transform backButton = levelCompletedPanel.Find("Back to menu");
            backToMenuButton = backButton != null ? backButton.gameObject : null;
        }

        if (scoreLabel == null)
        {
            Transform label = levelCompletedPanel.Find("ScoreTEXT");
            scoreLabel = label != null ? label.gameObject : null;
        }

        if (lostTitleText == null)
            lostTitleText = CreateLostTitle();
    }

    private TMP_Text CreateLostTitle()
    {
        GameObject titleObject = new GameObject(
            "Lost Title",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        titleObject.layer = levelCompletedPanel.gameObject.layer;
        titleObject.transform.SetParent(levelCompletedPanel, false);

        RectTransform rect = titleObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 100f);
        rect.sizeDelta = new Vector2(260f, 60f);

        TextMeshProUGUI title = titleObject.GetComponent<TextMeshProUGUI>();
        title.text = "You lost";
        title.fontSize = 32f;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        title.raycastTarget = false;
        titleObject.SetActive(false);
        return title;
    }
}
