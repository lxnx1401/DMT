using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] protected ObstacleType obstacleType = ObstacleType.Generic;
    [SerializeField, Min(0f)] protected float baseDamage = 1f;
    [SerializeField, Min(0f)] protected float currentDamage = 1f;
    [SerializeField, Min(0f)] protected float baseSpeed = 1f;
    [SerializeField, Min(0f)] protected float currentSpeed = 1f;
    [SerializeField] protected bool affectedByDifficulty = true;
    [SerializeField] private DifficultyManager difficultyManager;
    [SerializeField] private PerformanceAnalyzer performanceAnalyzer;
    [SerializeField] private string playerTag = "Player";

    public float CurrentDamage => currentDamage;
    public float CurrentSpeed => currentSpeed;

    protected virtual void OnEnable()
    {
        ResolveReferences();
        SubscribeToDifficulty();
    }

    protected virtual void Start()
    {
        ResolveReferences();
        SubscribeToDifficulty();
        ApplyDifficulty(difficultyManager != null ? difficultyManager.CurrentTuning : null);
    }

    protected virtual void OnDisable()
    {
        if (difficultyManager != null)
            difficultyManager.OnDifficultyChanged -= ApplyDifficulty;
    }

    public virtual void ApplyDifficulty(DifficultyTuning tuning)
    {
        if (affectedByDifficulty && tuning != null)
        {
            currentDamage = baseDamage * tuning.obstacleDamageMultiplier;
            currentSpeed = baseSpeed * tuning.obstacleSpeedMultiplier;
        }
        else
        {
            currentDamage = baseDamage;
            currentSpeed = baseSpeed;
        }
    }

    public virtual void RegisterHit()
    {
        if (performanceAnalyzer == null)
            performanceAnalyzer = FindFirstObjectByType<PerformanceAnalyzer>();

        performanceAnalyzer?.RegisterObstacleHit(obstacleType.ToString());
    }

    public virtual void RegisterHit(GameObject other)
    {
        if (other != null && other.CompareTag(playerTag))
            RegisterHit();
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null)
            RegisterHit(other.gameObject);
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null)
            RegisterHit(collision.gameObject);
    }

    private void ResolveReferences()
    {
        if (difficultyManager == null)
            difficultyManager = FindFirstObjectByType<DifficultyManager>();
        if (performanceAnalyzer == null)
            performanceAnalyzer = FindFirstObjectByType<PerformanceAnalyzer>();
    }

    private void SubscribeToDifficulty()
    {
        if (!affectedByDifficulty || difficultyManager == null)
            return;

        difficultyManager.OnDifficultyChanged -= ApplyDifficulty;
        difficultyManager.OnDifficultyChanged += ApplyDifficulty;
    }
}
