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

        int unlockedCount = LevelManager.Instance.UnlockedLevelCount;
        for (int levelIndex = 0; levelIndex < unlockedCount; levelIndex++)
        {
            Button button = Instantiate(levelButtonPrefab, buttonContainer);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = (levelIndex + 1).ToString();

            int capturedIndex = levelIndex;
            button.onClick.AddListener(() => SelectLevel(capturedIndex));
        }
    }

    private void SelectLevel(int levelIndex)
    {
        if (LevelManager.Instance == null || !LevelManager.Instance.TrySelectLevel(levelIndex))
            return;

        Time.timeScale = 1f;
        GameManager.Instance?.StartNewGame();
        SceneManager.LoadScene(gameplaySceneName);
    }
}
