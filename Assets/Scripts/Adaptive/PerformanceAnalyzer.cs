using System.Collections.Generic;
using UnityEngine;

public class PerformanceAnalyzer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DifficultyManager difficultyManager;

    [Header("Defaults")]
    [SerializeField, Min(0.1f)] private float defaultTargetTime = 20f;
    [SerializeField, Min(0)] private int defaultParticleCount = 1000;

    private readonly Dictionary<string, int> obstacleHits = new Dictionary<string, int>();
    private int activeCheckpointIndex;
    private int particlesAtStart;
    private int reportedParticleLoss;
    private float sectionStartTime;
    private float spreadTotal;
    private int spreadSampleCount;
    private float recoveryTime;

    public bool IsSectionActive { get; private set; }
    public SectionPerformanceData LastCompletedSection { get; private set; }

    private void Awake()
    {
        if (difficultyManager == null)
            difficultyManager = FindFirstObjectByType<DifficultyManager>();
    }

    public void BeginSection(int checkpointIndex, int currentParticleCount)
    {
        activeCheckpointIndex = checkpointIndex;
        particlesAtStart = NormalizeParticleCount(currentParticleCount);
        reportedParticleLoss = 0;
        sectionStartTime = Time.time;
        spreadTotal = 0f;
        spreadSampleCount = 0;
        recoveryTime = 0f;
        obstacleHits.Clear();
        IsSectionActive = true;
    }

    public SectionPerformanceData CompleteSection(int checkpointIndex, int currentParticleCount)
    {
        if (!IsSectionActive)
        {
            BeginSection(checkpointIndex, currentParticleCount);
            return null;
        }

        int particlesAtEnd = NormalizeParticleCount(currentParticleCount);
        int measuredLoss = Mathf.Max(0, particlesAtStart - particlesAtEnd);
        int particlesLost = Mathf.Max(measuredLoss, reportedParticleLoss);
        int totalHits = 0;

        foreach (int hitCount in obstacleHits.Values)
            totalHits += hitCount;

        SectionPerformanceData data = new SectionPerformanceData
        {
            checkpointIndex = checkpointIndex,
            sectionTime = Mathf.Max(0f, Time.time - sectionStartTime),
            targetTime = defaultTargetTime,
            particlesAtStart = particlesAtStart,
            particlesAtEnd = particlesAtEnd,
            particlesLost = particlesLost,
            particleLossRate = particlesAtStart > 0
                ? Mathf.Clamp01((float)particlesLost / particlesAtStart)
                : 0f,
            totalObstacleHits = totalHits,
            averageSwarmSpread = spreadSampleCount > 0 ? spreadTotal / spreadSampleCount : 0f,
            recoveryTime = recoveryTime
        };

        data.SetObstacleHits(obstacleHits);
        LastCompletedSection = data;
        IsSectionActive = false;

        if (difficultyManager == null)
            difficultyManager = FindFirstObjectByType<DifficultyManager>();

        difficultyManager?.EvaluateSection(data);
        return data;
    }

    public void RegisterObstacleHit(string obstacleType)
    {
        if (!IsSectionActive)
            return;

        string typeName = string.IsNullOrWhiteSpace(obstacleType) ? "Unknown" : obstacleType;
        obstacleHits.TryGetValue(typeName, out int hitCount);
        obstacleHits[typeName] = hitCount + 1;
    }

    public void RegisterParticleLoss(int amount)
    {
        if (IsSectionActive)
            reportedParticleLoss += Mathf.Max(0, amount);
    }

    public void ReportSwarmSpread(float spread)
    {
        if (!IsSectionActive || spread < 0f)
            return;

        spreadTotal += spread;
        spreadSampleCount++;
    }

    public void ReportRecoveryTime(float reportedRecoveryTime)
    {
        if (IsSectionActive)
            recoveryTime = Mathf.Max(recoveryTime, Mathf.Max(0f, reportedRecoveryTime));
    }

    private int NormalizeParticleCount(int particleCount)
    {
        return particleCount >= 0 ? particleCount : defaultParticleCount;
    }
}
