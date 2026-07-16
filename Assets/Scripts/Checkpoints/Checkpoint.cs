using UnityEngine;

public interface IParticleCountProvider
{
    int CurrentParticleCount { get; }
}

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private int checkpointIndex;
    [SerializeField] private bool isGoal;
    [SerializeField] private string requiredTag = "Player";
    [SerializeField] private PerformanceAnalyzer performanceAnalyzer;

    [Tooltip("Optional component implementing IParticleCountProvider.")]
    [SerializeField] private MonoBehaviour particleCountProvider;
    [SerializeField, Min(0)] private int fallbackParticleCount = 1000;

    private bool hasTriggered;
    private IParticleCountProvider countProvider;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        fallbackParticleCount = Mathf.Max(0, fallbackParticleCount);
        if (particleCountProvider != null && !(particleCountProvider is IParticleCountProvider))
            particleCountProvider = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered || other == null || !other.CompareTag(requiredTag))
            return;

        if (performanceAnalyzer == null)
            performanceAnalyzer = FindFirstObjectByType<PerformanceAnalyzer>();

        if (performanceAnalyzer == null)
        {
            Debug.LogWarning("Checkpoint could not find a PerformanceAnalyzer.", this);
            return;
        }

        hasTriggered = true;
        int particleCount = GetCurrentParticleCount();

        // The first checkpoint starts measurement. Every later checkpoint completes
        // the previous section and, unless it is the goal, starts the next section.
        if (!performanceAnalyzer.IsSectionActive)
        {
            performanceAnalyzer.BeginSection(checkpointIndex, particleCount);
            return;
        }

        performanceAnalyzer.CompleteSection(checkpointIndex, particleCount);
        if (!isGoal)
            performanceAnalyzer.BeginSection(checkpointIndex, particleCount);
    }

    public int GetCurrentParticleCount()
    {
        if (countProvider == null && particleCountProvider != null)
            countProvider = particleCountProvider as IParticleCountProvider;

        return countProvider != null
            ? Mathf.Max(0, countProvider.CurrentParticleCount)
            : fallbackParticleCount;
    }

    private void ResolveReferences()
    {
        if (performanceAnalyzer == null)
            performanceAnalyzer = FindFirstObjectByType<PerformanceAnalyzer>();

        countProvider = particleCountProvider as IParticleCountProvider;
    }
}
