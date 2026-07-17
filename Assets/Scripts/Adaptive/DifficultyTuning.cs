using System;
using UnityEngine;

[Serializable]
public class DifficultyTuning
{
    [Range(0f, 1f)] public float difficulty = 0.5f;
    public float playerSpeedMultiplier = 1f;
    public float obstacleDamageMultiplier = 1f;
    public float obstacleSpawnMultiplier = 1f;
    public float swarmCohesionMultiplier = 1f;
    public float coinObstacleClearance = 3f;

    public void DeriveFromDifficulty(float value)
    {
        difficulty = Mathf.Clamp01(value);

        playerSpeedMultiplier = Mathf.Lerp(0.85f, 1.35f, difficulty);
        obstacleDamageMultiplier = Mathf.Lerp(0.6f, 1.5f, difficulty);
        obstacleSpawnMultiplier = Mathf.Lerp(0.75f, 1.35f, difficulty);
        swarmCohesionMultiplier = Mathf.Lerp(1.25f, 0.8f, difficulty);
        coinObstacleClearance = Mathf.Lerp(6f, 1f, difficulty);

        playerSpeedMultiplier = Mathf.Clamp(playerSpeedMultiplier, 0.75f, 1.5f);
        obstacleDamageMultiplier = Mathf.Clamp(obstacleDamageMultiplier, 0.5f, 1.75f);
        obstacleSpawnMultiplier = Mathf.Clamp(obstacleSpawnMultiplier, 0.5f, 1.5f);
        swarmCohesionMultiplier = Mathf.Clamp(swarmCohesionMultiplier, 0.75f, 1.4f);
        coinObstacleClearance = Mathf.Clamp(coinObstacleClearance, 0.75f, 8f);
    }
}
