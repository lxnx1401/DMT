using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float radius = 3f;
    [SerializeField, Min(0)] private int baseParticleDamage = 100;

    public float Radius => radius;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        float multiplier = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.CurrentTuning.obstacleDamageMultiplier
            : 1f;

        int damage = Mathf.Max(0, Mathf.RoundToInt(baseParticleDamage * multiplier));
        PerformanceAnalyzer.Instance?.RegisterObstacleHit();
        ParticleSimulation.Instance?.TakeDamage(damage);
    }
}
