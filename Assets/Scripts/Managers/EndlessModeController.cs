using UnityEngine;

[DefaultExecutionOrder(50)]
public class EndlessModeController : MonoBehaviour
{
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    [SerializeField] private CrownSpawner crownSpawner;
    [SerializeField] private LaserEnemySpawner laserSpawner;
    [SerializeField] private BlackHoleSpawner blackHoleSpawner;

    public static EndlessModeController Instance { get; private set; }

    public int WaveIndex { get; private set; }
    public LevelData CurrentWave { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.IsEndlessMode)
            BeginRun();
    }

    public void BeginRun()
    {
        WaveIndex = 0;
        SpawnWave();
    }

    public void AdvanceWave()
    {
        WaveIndex++;
        SpawnWave();
    }

    private void SpawnWave()
    {
        CurrentWave = LevelGenerator.Generate(WaveIndex);

        obstacleSpawner?.ClearSpawned();
        crownSpawner?.ClearSpawned();
        laserSpawner?.ClearSpawned();
        blackHoleSpawner?.ClearSpawned();

        // Obstacles/Laser/BlackHole zuerst, damit CrownSpawner ihre Positionen für die
        // Abstandsprüfung schon vorfindet (kein Frame-Delay nötig, da synchron aufgerufen).
        obstacleSpawner?.SpawnFor(CurrentWave, WaveIndex);
        laserSpawner?.SpawnFor(CurrentWave, WaveIndex);
        blackHoleSpawner?.SpawnFor(CurrentWave, WaveIndex);
        crownSpawner?.SpawnFor(CurrentWave, WaveIndex);
    }
}
