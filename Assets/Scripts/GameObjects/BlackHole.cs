using UnityEngine;

public class BlackHole : MonoBehaviour
{
    [Header("Attraction")]
    [SerializeField, Min(0.1f)] private float radius = 1f;
    [SerializeField, Min(0.1f)] private float pullRadius = 6f;
    [SerializeField, Min(0f)] private float strength = 40f;

    [Header("Visual")]
    [SerializeField] private float rotationSpeed = 60f;

    [Header("Damage")]
    [SerializeField, Min(0)] private int baseParticleDamage = 15;
    [SerializeField, Min(0.05f)] private float damageTickInterval = 0.2f;

    private float tickTimer;

    public float Radius => radius;
    public float PullRadius => pullRadius;
    public float Strength => strength;

    private void Update()
    {
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        tickTimer += Time.deltaTime;
        if (tickTimer < damageTickInterval)
            return;

        tickTimer = 0f;

        float multiplier = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.CurrentTuning.blackHoleDamageMultiplier
            : 1f;

        int damage = Mathf.Max(0, Mathf.RoundToInt(baseParticleDamage * multiplier));
        PerformanceAnalyzer.Instance?.RegisterObstacleHit(HazardType.BlackHole);
        ParticleSimulation.Instance?.TakeDamage(damage);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            tickTimer = 0f;
    }
}
