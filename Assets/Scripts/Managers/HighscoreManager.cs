using UnityEngine;

public static class HighscoreManager
{
    private const string KeyPrefix = "Highscore_Level_";
    private const string EndlessKey = "Highscore_Endless";

    public static int GetHighscore(int levelIndex)
    {
        return PlayerPrefs.GetInt(KeyPrefix + levelIndex, 0);
    }

    public static bool TrySubmitScore(int levelIndex, int score)
    {
        if (score <= GetHighscore(levelIndex))
            return false;

        PlayerPrefs.SetInt(KeyPrefix + levelIndex, score);
        PlayerPrefs.Save();
        return true;
    }

    public static int GetEndlessHighscore()
    {
        return PlayerPrefs.GetInt(EndlessKey, 0);
    }

    public static bool TrySubmitEndlessScore(int score)
    {
        if (score <= GetEndlessHighscore())
            return false;

        PlayerPrefs.SetInt(EndlessKey, score);
        PlayerPrefs.Save();
        return true;
    }
}
