using System;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class DifficultyManager : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float minDifficulty = 0.1f;
    [SerializeField, Range(0f, 1f)] private float maxDifficulty = 1f;
    [SerializeField, Min(0f)] private float difficultyStep = 0.08f;
    [SerializeField, Range(0f, 1f)] private float targetStruggleMin = 0.25f;
    [SerializeField, Range(0f, 1f)] private float targetStruggleMax = 0.6f;
    [SerializeField] private DifficultyTuning currentTuning = new DifficultyTuning();

    public static DifficultyManager Instance { get; private set; }
    public DifficultyTuning CurrentTuning => currentTuning;
    public event Action<DifficultyTuning> OnDifficultyChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateRuntimeServices()
    {
        if (FindFirstObjectByType<DifficultyManager>() != null)
            return;

        GameObject services = new GameObject("Adaptive Runtime");
        services.AddComponent<DifficultyManager>();
        services.AddComponent<PerformanceAnalyzer>();
        DontDestroyOnLoad(services);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ValidateSettings();
        currentTuning.DeriveFromDifficulty(
            Mathf.Clamp(currentTuning.difficulty, minDifficulty, maxDifficulty));
    }

    public void EvaluateSection(SectionPerformanceData data)
    {
        if (data == null)
            return;

        float struggle = data.OverallStruggleScore;
        float direction = 0f;

        if (struggle < targetStruggleMin)
            direction = 1f;
        else if (struggle > targetStruggleMax)
            direction = -1f;

        float previous = currentTuning.difficulty;
        float next = Mathf.Clamp(previous + direction * difficultyStep, minDifficulty, maxDifficulty);
        currentTuning.DeriveFromDifficulty(next);

        Debug.Log(
            $"Adaptive section {data.sectionIndex}: struggle={struggle:0.00}, " +
            $"difficulty={previous:0.00}->{next:0.00}, time={data.sectionTime:0.0}s, " +
            $"lost={data.particlesLost}, hits={data.obstacleHits}",
            this);

        OnDifficultyChanged?.Invoke(currentTuning);
    }

    private void OnValidate()
    {
        ValidateSettings();
        currentTuning?.DeriveFromDifficulty(
            Mathf.Clamp(currentTuning.difficulty, minDifficulty, maxDifficulty));
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void ValidateSettings()
    {
        minDifficulty = Mathf.Clamp01(minDifficulty);
        maxDifficulty = Mathf.Clamp(maxDifficulty, minDifficulty, 1f);
        difficultyStep = Mathf.Clamp(difficultyStep, 0f, 0.25f);
        targetStruggleMin = Mathf.Clamp01(targetStruggleMin);
        targetStruggleMax = Mathf.Clamp(targetStruggleMax, targetStruggleMin, 1f);
        currentTuning ??= new DifficultyTuning();
    }
}
