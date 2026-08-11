using UnityEngine;

public static class LevelGenerator
{
    public static LevelData Generate(int levelIndex)
    {
        int index = Mathf.Max(0, levelIndex);
        System.Random random = new System.Random(index * 104729 + 31);

        LevelData level = ScriptableObject.CreateInstance<LevelData>();
        level.crownAmount = 3 + index + random.Next(0, 2);
        level.obstacleAmount = 4 + index * 2 + random.Next(0, 3);
        level.laserAmount = index / 3;
        level.blackHoleAmount = Mathf.Max(0, (index - 4) / 3);
        level.worldSize = 32 + Mathf.RoundToInt(12f * Mathf.Sqrt(index));
        return level;
    }
}
