using UnityEngine;

public class LaserEnemy : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 90f;

    [Header("Pulse (Länge)")]
    [SerializeField] private float minLengthMultiplier = 0.7f;
    [SerializeField] private float maxLengthMultiplier = 1.3f;
    [SerializeField, Min(0.01f)] private float pulseSpeed = 1f;

    [Header("Damage")]
    [SerializeField, Min(0)] private int baseParticleDamage = 20;
    [SerializeField, Min(0.05f)] private float damageTickInterval = 0.2f;

    private Vector3 baseScale;
    private float tickTimer;

    private void Awake()
    {
        baseScale = transform.localScale;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;
    }

    private void Update()
    {
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);

        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float lengthMultiplier = Mathf.Lerp(minLengthMultiplier, maxLengthMultiplier, t);
        transform.localScale = new Vector3(baseScale.x * lengthMultiplier, baseScale.y, baseScale.z);
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
            ? DifficultyManager.Instance.CurrentTuning.laserDamageMultiplier
            : 1f;

        int damage = Mathf.Max(0, Mathf.RoundToInt(baseParticleDamage * multiplier));
        PerformanceAnalyzer.Instance?.RegisterObstacleHit();
        ParticleSimulation.Instance?.TakeDamage(damage);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            tickTimer = 0f;
    }
}
