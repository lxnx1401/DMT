using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SectionPerformanceData
{
    public int sectionIndex;
    public float sectionTime;
    public float targetTime;
    public int particlesAtStart;
    public int particlesAtEnd;
    public int particlesLost;
    public Dictionary<HazardType, int> hitsByType = new Dictionary<HazardType, int>();

    public int TotalHits
    {
        get
        {
            int total = 0;
            foreach (int count in hitsByType.Values)
                total += count;
            return total;
        }
    }

    public float TimeStruggle
    {
        get
        {
            if (targetTime <= 0f)
                return 0f;

            return Mathf.Clamp01((sectionTime - targetTime) / targetTime);
        }
    }

    public float ParticleLossStruggle => particlesAtStart > 0
        ? Mathf.Clamp01((float)particlesLost / particlesAtStart * 4f)
        : 0f;

    public float CollisionStruggle => Mathf.Clamp01(TotalHits / 3f);

    public float OverallStruggleScore => Mathf.Clamp01(
        TimeStruggle * 0.25f +
        ParticleLossStruggle * 0.5f +
        CollisionStruggle * 0.25f);

    public float GetHazardStruggle(HazardType type)
    {
        hitsByType.TryGetValue(type, out int count);
        return Mathf.Clamp01(count / 3f);
    }
}
