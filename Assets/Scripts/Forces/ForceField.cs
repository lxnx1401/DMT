using UnityEngine;

public class ForceField : MonoBehaviour
{
    [SerializeField] private ForceFieldType fieldType = ForceFieldType.Attraction;
    [SerializeField] private float baseStrength = 5f;
    [SerializeField] private float currentStrength = 5f;
    [SerializeField, Min(0.01f)] private float radius = 5f;
    [SerializeField] private Vector2 direction = Vector2.right;
    [SerializeField] private bool affectedByDifficulty = true;
    [SerializeField] private DifficultyManager difficultyManager;

    private void OnEnable()
    {
        FindAndSubscribeToDifficultyManager();
    }

    private void Start()
    {
        FindAndSubscribeToDifficultyManager();
        if (affectedByDifficulty && difficultyManager != null)
            ApplyDifficulty(difficultyManager.CurrentTuning);
        else
            currentStrength = baseStrength;
    }

    private void OnDisable()
    {
        if (difficultyManager != null)
            difficultyManager.OnDifficultyChanged -= ApplyDifficulty;
    }

    public Vector2 GetForceAtPosition(Vector2 particlePosition)
    {
        Vector2 center = transform.position;
        Vector2 toCenter = center - particlePosition;
        float distance = toCenter.magnitude;

        if (distance > radius || radius <= 0f)
            return Vector2.zero;

        float falloff = 1f - Mathf.Clamp01(distance / radius);
        Vector2 inward = distance > 0.0001f ? toCenter / distance : Vector2.zero;

        switch (fieldType)
        {
            case ForceFieldType.Attraction:
                return inward * currentStrength * falloff;
            case ForceFieldType.Repulsion:
                return -inward * currentStrength * falloff;
            case ForceFieldType.Vortex:
                return new Vector2(-inward.y, inward.x) * currentStrength * falloff;
            case ForceFieldType.Wind:
                return direction.sqrMagnitude > 0f
                    ? direction.normalized * currentStrength * falloff
                    : Vector2.zero;
            case ForceFieldType.Slow:
                // Velocity is required for real drag. Use GetSlowForce when available.
                return Vector2.zero;
            case ForceFieldType.BlackHole:
                return inward * currentStrength * falloff * falloff * 2f;
            default:
                return Vector2.zero;
        }
    }

    public Vector2 GetSlowForce(Vector2 particlePosition, Vector2 velocity)
    {
        float distance = Vector2.Distance(transform.position, particlePosition);
        if (fieldType != ForceFieldType.Slow || distance > radius || radius <= 0f)
            return Vector2.zero;

        float falloff = 1f - Mathf.Clamp01(distance / radius);
        return -velocity * currentStrength * falloff;
    }

    public void ApplyDifficulty(DifficultyTuning tuning)
    {
        currentStrength = affectedByDifficulty && tuning != null
            ? baseStrength * tuning.forceStrengthMultiplier
            : baseStrength;
    }

    private void FindAndSubscribeToDifficultyManager()
    {
        if (!affectedByDifficulty)
            return;

        if (difficultyManager == null)
            difficultyManager = FindFirstObjectByType<DifficultyManager>();

        if (difficultyManager != null)
        {
            difficultyManager.OnDifficultyChanged -= ApplyDifficulty;
            difficultyManager.OnDifficultyChanged += ApplyDifficulty;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = fieldType == ForceFieldType.Repulsion ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, radius));
    }
}
