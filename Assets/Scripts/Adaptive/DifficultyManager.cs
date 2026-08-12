using System;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class DifficultyManager : MonoBehaviour
{
    private static readonly HazardType[] HazardTypes = (HazardType[])Enum.GetValues(typeof(HazardType));

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
        RederiveAll();
    }

    public void EvaluateSection(SectionPerformanceData data)
    {
        if (data == null)
            return;

        float overallStruggle = data.OverallStruggleScore;
        float previousGlobal = currentTuning.difficulty;
        float nextGlobal = StepDifficulty(previousGlobal, overallStruggle);
        currentTuning.DeriveGlobal(nextGlobal, CurrentLevelIndex);

        foreach (HazardType type in HazardTypes)
        {
            float hazardStruggle = data.GetHazardStruggle(type);
            float previousHazard = currentTuning.GetHazardDifficulty(type);
            float nextHazard = StepDifficulty(previousHazard, hazardStruggle);
            currentTuning.DeriveHazard(type, nextHazard, CurrentLevelIndex);
        }

        Debug.Log(
            $"Adaptive section {data.sectionIndex}: struggle={overallStruggle:0.00}, " +
            $"difficulty={previousGlobal:0.00}->{nextGlobal:0.00}, " +
            $"obstacle={currentTuning.obstacleDifficulty:0.00}, laser={currentTuning.laserDifficulty:0.00}, " +
            $"blackHole={currentTuning.blackHoleDifficulty:0.00}, time={data.sectionTime:0.0}s, " +
            $"lost={data.particlesLost}, hits={data.TotalHits}",
            this);

        OnDifficultyChanged?.Invoke(currentTuning);
    }

    private float StepDifficulty(float previous, float struggle)
    {
        float direction = 0f;

        if (struggle < targetStruggleMin)
            direction = 1f;
        else if (struggle > targetStruggleMax)
            direction = -1f;

        return Mathf.Clamp(previous + direction * difficultyStep, minDifficulty, maxDifficulty);
    }

    private void OnValidate()
    {
        ValidateSettings();
        RederiveAll();
    }

    private void RederiveAll()
    {
        if (currentTuning == null)
            return;

        currentTuning.DeriveGlobal(
            Mathf.Clamp(currentTuning.difficulty, minDifficulty, maxDifficulty),
            CurrentLevelIndex);

        foreach (HazardType type in HazardTypes)
        {
            currentTuning.DeriveHazard(
                type,
                Mathf.Clamp(currentTuning.GetHazardDifficulty(type), minDifficulty, maxDifficulty),
                CurrentLevelIndex);
        }
    }

    private static int CurrentLevelIndex =>
        LevelManager.Instance != null ? LevelManager.Instance.currentLevel : 0;

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
