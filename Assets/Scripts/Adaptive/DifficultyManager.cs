using System;
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    [Header("Difficulty Range")]
    [SerializeField, Range(0f, 1f)] private float minDifficulty = 0.1f;
    [SerializeField, Range(0f, 1f)] private float maxDifficulty = 1f;
    [SerializeField, Min(0f)] private float difficultyChangeSpeed = 0.08f;

    [Header("Desired Struggle Range")]
    [SerializeField, Range(0f, 1f)] private float targetStruggleMin = 0.3f;
    [SerializeField, Range(0f, 1f)] private float targetStruggleMax = 0.6f;

    [Header("Runtime State")]
    [SerializeField] private DifficultyTuning currentTuning = new DifficultyTuning();

    public DifficultyTuning CurrentTuning => currentTuning;
    public event Action<DifficultyTuning> OnDifficultyChanged;

    private void Awake()
    {
        ValidateSettings();
        currentTuning.DeriveFromDifficulty(
            Mathf.Clamp(currentTuning.difficulty, minDifficulty, maxDifficulty));
    }

    private void OnValidate()
    {
        ValidateSettings();
        if (currentTuning != null)
            currentTuning.DeriveFromDifficulty(
                Mathf.Clamp(currentTuning.difficulty, minDifficulty, maxDifficulty));
    }

    public void EvaluateSection(SectionPerformanceData data)
    {
        if (data == null)
        {
            Debug.LogWarning("Difficulty evaluation skipped because section data was null.", this);
            return;
        }

        float struggle = data.OverallStruggleScore;
        float targetDifficulty = currentTuning.difficulty;

        if (struggle < targetStruggleMin)
            targetDifficulty = maxDifficulty;
        else if (struggle > targetStruggleMax)
            targetDifficulty = minDifficulty;

        float previousDifficulty = currentTuning.difficulty;
        float nextDifficulty = Mathf.MoveTowards(
            previousDifficulty,
            targetDifficulty,
            difficultyChangeSpeed);

        nextDifficulty = Mathf.Clamp(nextDifficulty, minDifficulty, maxDifficulty);
        currentTuning.DeriveFromDifficulty(nextDifficulty);

        Debug.Log(
            $"Adaptive difficulty: checkpoint {data.checkpointIndex}, struggle " +
            $"{struggle:0.00}, difficulty {previousDifficulty:0.00} -> {nextDifficulty:0.00}",
            this);

        OnDifficultyChanged?.Invoke(currentTuning);
    }

    private void ValidateSettings()
    {
        minDifficulty = Mathf.Clamp01(minDifficulty);
        maxDifficulty = Mathf.Clamp(maxDifficulty, minDifficulty, 1f);
        difficultyChangeSpeed = Mathf.Max(0f, difficultyChangeSpeed);
        targetStruggleMin = Mathf.Clamp01(targetStruggleMin);
        targetStruggleMax = Mathf.Clamp(targetStruggleMax, targetStruggleMin, 1f);

        if (currentTuning == null)
            currentTuning = new DifficultyTuning();
    }
}
