using UnityEngine;

[DefaultExecutionOrder(-900)]
public class PerformanceAnalyzer : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float targetTimePerCoin = 8f;

    public static PerformanceAnalyzer Instance { get; private set; }
    public bool IsSectionActive { get; private set; }

    private int sectionIndex;
    private int particlesAtStart;
    private int reportedParticleLoss;
    private int obstacleHits;
    private float sectionStartTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void BeginSection(int currentParticleCount)
    {
        particlesAtStart = Mathf.Max(0, currentParticleCount);
        reportedParticleLoss = 0;
        obstacleHits = 0;
        sectionStartTime = Time.time;
        IsSectionActive = true;
    }

    public void CompleteSection(int currentParticleCount)
    {
        if (!IsSectionActive)
        {
            BeginSection(currentParticleCount);
            return;
        }

        int particlesAtEnd = Mathf.Max(0, currentParticleCount);
        int measuredLoss = Mathf.Max(0, particlesAtStart - particlesAtEnd);
        SectionPerformanceData data = new SectionPerformanceData
        {
            sectionIndex = sectionIndex++,
            sectionTime = Mathf.Max(0f, Time.time - sectionStartTime),
            targetTime = targetTimePerCoin,
            particlesAtStart = particlesAtStart,
            particlesAtEnd = particlesAtEnd,
            particlesLost = Mathf.Max(measuredLoss, reportedParticleLoss),
            obstacleHits = obstacleHits
        };

        IsSectionActive = false;
        DifficultyManager.Instance?.EvaluateSection(data);
    }

    public void RegisterObstacleHit()
    {
        if (IsSectionActive)
            obstacleHits++;
    }

    public void RegisterParticleLoss(int amount)
    {
        if (IsSectionActive)
            reportedParticleLoss += Mathf.Max(0, amount);
    }

    public void ResetRun(int currentParticleCount)
    {
        sectionIndex = 0;
        BeginSection(currentParticleCount);
    }
}
