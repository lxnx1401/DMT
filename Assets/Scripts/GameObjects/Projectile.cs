using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float speed = 10f;
    [SerializeField, Min(0)] private int baseParticleDamage = 12;
    [SerializeField, Min(0.1f)] private float maxLifetime = 6f;

    private Vector2 direction = Vector2.right;
    private float lifeTimer;

    public void Launch(Vector2 launchDirection)
    {
        direction = launchDirection.sqrMagnitude > 0.0001f ? launchDirection.normalized : Vector2.right;
        transform.right = direction;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= maxLifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        float multiplier = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.CurrentTuning.rangedDamageMultiplier
            : 1f;

        int damage = Mathf.Max(0, Mathf.RoundToInt(baseParticleDamage * multiplier));
        PerformanceAnalyzer.Instance?.RegisterObstacleHit(HazardType.Ranged);
        ParticleSimulation.Instance?.TakeDamage(damage);

        Destroy(gameObject);
    }
}
