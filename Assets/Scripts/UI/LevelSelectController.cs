using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelSelectController : MonoBehaviour
{
    [SerializeField] private RectTransform buttonContainer;
    [SerializeField] private Button levelButtonPrefab;
    [SerializeField] private Button endlessButton; 
    [SerializeField] private string gameplaySceneName = "Game";

    private void OnEnable()
    {
        SetupEndlessButton(); 
        Populate();
    }

    private void SetupEndlessButton()
    {
        if (endlessButton == null) return;

        TMP_Text label = endlessButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            int highscore = HighscoreManager.GetEndlessHighscore();
            label.text = $"Endless   Best: {highscore:000}";
        }

        endlessButton.onClick.RemoveAllListeners();
        endlessButton.onClick.AddListener(SelectEndless);
    }

    private void Populate()
    {
        if (buttonContainer == null || levelButtonPrefab == null || LevelManager.Instance == null)
            return;

        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);


        int unlockedCount = LevelManager.Instance.UnlockedLevelCount;
        for (int levelIndex = 0; levelIndex < unlockedCount; levelIndex++)
        {
            Button button = Instantiate(levelButtonPrefab, buttonContainer);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                int highscore = HighscoreManager.GetHighscore(levelIndex);
                label.text = $"Level {levelIndex + 1}   Best: {highscore:000}";
            }

            int capturedIndex = levelIndex;
            button.onClick.AddListener(() => SelectLevel(capturedIndex));
        }
    }

    private void SelectLevel(int levelIndex)
    {
        if (LevelManager.Instance == null || !LevelManager.Instance.TrySelectLevel(levelIndex))
            return;

        LevelManager.Instance.IsEndlessMode = false;
        Time.timeScale = 1f;
        GameManager.Instance?.StartNewGame();
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void SelectEndless()
    {
        if (LevelManager.Instance == null)
            return;

        LevelManager.Instance.IsEndlessMode = true;
        Time.timeScale = 1f;
        GameManager.Instance?.StartNewGame();
        SceneManager.LoadScene(gameplaySceneName);
    }
}