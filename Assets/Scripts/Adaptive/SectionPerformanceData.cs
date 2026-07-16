using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SectionPerformanceData
{
    public int checkpointIndex;
    public float sectionTime;
    public float targetTime = 20f;
    public int particlesAtStart;
    public int particlesAtEnd;
    public int particlesLost;
    public float particleLossRate;
    public int totalObstacleHits;
    public float averageSwarmSpread;
    public float recoveryTime;
    public List<string> obstacleTypes = new List<string>();
    public List<int> obstacleHitCounts = new List<int>();

    public float TimeScore
    {
        get
        {
            if (targetTime <= 0f)
                return 0f;

            return Mathf.Clamp01((sectionTime - targetTime) / targetTime);
        }
    }

    public float ParticleLossScore => Mathf.Clamp01(particleLossRate);

    public float CollisionScore => Mathf.Clamp01(totalObstacleHits / 5f);

    public float StabilityScore
    {
        get
        {
            const float comfortableSpread = 1f;
            const float difficultSpread = 5f;
            float spreadScore = Mathf.InverseLerp(comfortableSpread, difficultSpread, averageSwarmSpread);
            float recoveryScore = Mathf.Clamp01(recoveryTime / 5f);
            return Mathf.Clamp01((spreadScore + recoveryScore) * 0.5f);
        }
    }

    public float OverallStruggleScore
    {
        get
        {
            return Mathf.Clamp01(
                TimeScore * 0.25f +
                ParticleLossScore * 0.35f +
                CollisionScore * 0.25f +
                StabilityScore * 0.15f);
        }
    }

    public void SetObstacleHits(IReadOnlyDictionary<string, int> hitsByType)
    {
        obstacleTypes.Clear();
        obstacleHitCounts.Clear();

        if (hitsByType == null)
            return;

        foreach (KeyValuePair<string, int> entry in hitsByType)
        {
            obstacleTypes.Add(entry.Key);
            obstacleHitCounts.Add(entry.Value);
        }
    }
}
