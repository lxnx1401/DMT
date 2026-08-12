using System;
using UnityEngine;

[Serializable]
public class DifficultyTuning
{
    [Header("Global")]
    [Range(0f, 1f)] public float difficulty = 0.5f;
    public float playerSpeedMultiplier = 1f;
    public float swarmCohesionMultiplier = 1f;
    public float coinObstacleClearance = 3f;

    [Header("Obstacle")]
    [Range(0f, 1f)] public float obstacleDifficulty = 0.5f;
    public float obstacleDamageMultiplier = 1f;
    public float obstacleSpawnMultiplier = 1f;

    [Header("Laser")]
    [Range(0f, 1f)] public float laserDifficulty = 0.5f;
    public float laserDamageMultiplier = 1f;

    [Header("Black Hole")]
    [Range(0f, 1f)] public float blackHoleDifficulty = 0.5f;
    public float blackHoleDamageMultiplier = 1f;

    private const int IntensityStartLevelIndex = 8; // Level 9 (0-indexed) is the last unscaled level
    private const float IntensityGrowthPerLevel = 3f;
    private const float SafeClearance = 6f;

    public static float ComputeIntensity(int levelIndex) =>
        1f + Mathf.Max(0, levelIndex - IntensityStartLevelIndex) * IntensityGrowthPerLevel;

    public void DeriveGlobal(float value, int levelIndex = 0)
    {
        difficulty = Mathf.Clamp01(value);
        float intensity = ComputeIntensity(levelIndex);

        playerSpeedMultiplier = ScaleMultiplier(Mathf.Lerp(0.85f, 1.35f, difficulty), intensity, 0.75f, 1.5f);
        swarmCohesionMultiplier = ScaleMultiplier(Mathf.Lerp(1.25f, 0.8f, difficulty), intensity, 0.75f, 1.4f);
        coinObstacleClearance = ScaleClearance(Mathf.Lerp(SafeClearance, 1f, difficulty), intensity);
    }

    public float GetHazardDifficulty(HazardType type)
    {
        switch (type)
        {
            case HazardType.Laser: return laserDifficulty;
            case HazardType.BlackHole: return blackHoleDifficulty;
            default: return obstacleDifficulty;
        }
    }

    // Jeder Gefahrentyp bekommt seine eigene Difficulty, die unabhängig von den anderen wandert -
    // damit "viele Laser-Treffer, kaum Black-Hole-Treffer" auch wirklich nur den Laser verschärft.
    public void DeriveHazard(HazardType type, float value, int levelIndex = 0)
    {
        float clamped = Mathf.Clamp01(value);
        float intensity = ComputeIntensity(levelIndex);

        switch (type)
        {
            case HazardType.Laser:
                laserDifficulty = clamped;
                laserDamageMultiplier = ScaleMultiplier(Mathf.Lerp(0.6f, 1.5f, clamped), intensity, 0.5f, 1.75f);
                break;

            case HazardType.BlackHole:
                blackHoleDifficulty = clamped;
                blackHoleDamageMultiplier = ScaleMultiplier(Mathf.Lerp(0.6f, 1.5f, clamped), intensity, 0.5f, 1.75f);
                break;

            default:
                obstacleDifficulty = clamped;
                obstacleDamageMultiplier = ScaleMultiplier(Mathf.Lerp(0.6f, 1.5f, clamped), intensity, 0.5f, 1.75f);
                obstacleSpawnMultiplier = ScaleMultiplier(Mathf.Lerp(0.75f, 1.35f, clamped), intensity, 0.5f, 1.5f);
                break;
        }
    }

    // Verstärkt die Abweichung eines Multiplikators von seinem neutralen Wert (1.0) mit der Intensität,
    // und weitet dabei auch die Clamp-Grenzen auf - sonst würde die alte Obergrenze die Wirkung weiter kappen.
    private static float ScaleMultiplier(float baseValue, float intensity, float baseClampLow, float baseClampHigh)
    {
        float deviation = (baseValue - 1f) * intensity;
        float expandedLow = 1f - (1f - baseClampLow) * intensity;
        float expandedHigh = 1f + (baseClampHigh - 1f) * intensity;
        return Mathf.Clamp(1f + deviation, expandedLow, expandedHigh);
    }

    // Bei difficulty=0 bleibt der Abstand immer bei SafeClearance (sicher, unabhängig vom Level).
    // Erst die Abweichung durch steigende difficulty wird mit der Intensität verstärkt, und die
    // Untergrenze schrumpft mit dem Level - "Abstände reagieren extrem auf die Difficulty".
    private static float ScaleClearance(float baseValue, float intensity)
    {
        float deviation = (baseValue - SafeClearance) * intensity;
        float floor = Mathf.Max(0.15f, 0.75f / intensity);
        return Mathf.Clamp(SafeClearance + deviation, floor, 8f);
    }
}
