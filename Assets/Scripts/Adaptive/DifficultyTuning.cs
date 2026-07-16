using System;
using UnityEngine;

[Serializable]
public class DifficultyTuning
{
    [Range(0f, 1f)] public float difficulty = 0.5f;
    public float forceStrengthMultiplier = 1f;
    public float obstacleSpeedMultiplier = 1f;
    public float obstacleDamageMultiplier = 1f;
    public float obstacleSpawnMultiplier = 1f;
    public float swarmCohesionAssist = 1f;
    public float checkpointGraceMultiplier = 1f;

    public void DeriveFromDifficulty(float difficultyValue)
    {
        difficulty = Mathf.Clamp01(difficultyValue);
        forceStrengthMultiplier = Mathf.Lerp(0.7f, 1.5f, difficulty);
        obstacleSpeedMultiplier = Mathf.Lerp(0.75f, 1.5f, difficulty);
        obstacleDamageMultiplier = Mathf.Lerp(0.6f, 1.5f, difficulty);
        obstacleSpawnMultiplier = Mathf.Lerp(0.7f, 1.4f, difficulty);
        swarmCohesionAssist = Mathf.Lerp(1.4f, 0.75f, difficulty);
        checkpointGraceMultiplier = Mathf.Lerp(1.4f, 0.75f, difficulty);

        forceStrengthMultiplier = Mathf.Clamp(forceStrengthMultiplier, 0.5f, 2f);
        obstacleSpeedMultiplier = Mathf.Clamp(obstacleSpeedMultiplier, 0.5f, 2f);
        obstacleDamageMultiplier = Mathf.Clamp(obstacleDamageMultiplier, 0.25f, 2f);
        obstacleSpawnMultiplier = Mathf.Clamp(obstacleSpawnMultiplier, 0.5f, 2f);
        swarmCohesionAssist = Mathf.Clamp(swarmCohesionAssist, 0.5f, 2f);
        checkpointGraceMultiplier = Mathf.Clamp(checkpointGraceMultiplier, 0.5f, 2f);
    }
}
