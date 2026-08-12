using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelSelectController : MonoBehaviour
{
    [SerializeField] private RectTransform buttonContainer;
    [SerializeField] private Button levelButtonPrefab;
    [SerializeField] private string gameplaySceneName = "Game";

    private void OnEnable()
    {
        Populate();
    }

    private void Populate()
    {
        if (buttonContainer == null || levelButtonPrefab == null || LevelManager.Instance == null)
            return;

        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        AddEndlessButton();

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

    // Eigene Zeile über den Leveln - Highscore hier ist die Anzahl gesammelter Kronen
    // über den ganzen Run, nicht die übrig gebliebenen Partikel wie bei den Leveln.
    private void AddEndlessButton()
    {
        Button button = Instantiate(levelButtonPrefab, buttonContainer);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            int highscore = HighscoreManager.GetEndlessHighscore();
            label.text = $"Endless   Best: {highscore:000}";
        }

        button.onClick.AddListener(SelectEndless);
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
