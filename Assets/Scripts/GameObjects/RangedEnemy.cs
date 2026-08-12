using UnityEngine;

// Stationärer Gegner: dreht sich zum Spieler und feuert in regelmäßigen Abständen ein
// Projektil in dessen aktuelle Richtung - Feuerrate und Schaden skalieren mit der Difficulty.
public class RangedEnemy : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Min(0f)] private float projectileSpawnOffset = 0.6f;

    [Header("Firing")]
    [SerializeField, Min(0.1f)] private float baseFireInterval = 2.5f;

    private float fireTimer;

    private void Update()
    {
        AimAtPlayer();

        fireTimer += Time.deltaTime;

        float fireRateMultiplier = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.CurrentTuning.rangedFireRateMultiplier
            : 1f;
        float effectiveInterval = baseFireInterval / Mathf.Max(0.1f, fireRateMultiplier);

        if (fireTimer < effectiveInterval)
            return;

        fireTimer = 0f;
        Fire();
    }

    private void AimAtPlayer()
    {
        if (ParticleSimulation.Instance == null)
            return;

        Vector2 toPlayer = ParticleSimulation.Instance.PlayerPosition - (Vector2)transform.position;
        if (toPlayer.sqrMagnitude > 0.0001f)
            transform.right = toPlayer;
    }

    private void Fire()
    {
        if (projectilePrefab == null || ParticleSimulation.Instance == null)
            return;

        Vector2 direction = ParticleSimulation.Instance.PlayerPosition - (Vector2)transform.position;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Vector2 spawnPosition = (Vector2)transform.position + direction * projectileSpawnOffset;

        GameObject projectileObject = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        Projectile projectile = projectileObject.GetComponent<Projectile>();
        if (projectile != null)
            projectile.Launch(direction);
    }
}
