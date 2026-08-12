using System.Collections.Generic;
using UnityEngine;

// Endloses Durchwandern: Kacheln um den Spieler herum werden erzeugt, sobald sie in Reichweite
// kommen, und wieder abgebaut, sobald sie zu weit hinter dem Spieler liegen. Die Schwierigkeit
// pro Kachel richtet sich nach ihrer Distanz zur Start-Kachel (0,0) - je weiter draußen, desto mehr.
[DefaultExecutionOrder(50)]
public class EndlessWorldController : MonoBehaviour
{
    private class WorldTile
    {
        public Vector2Int Coord;
        public Bounds Bounds;
        public int DistanceIndex;
        public Transform Container;
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private GameObject crownPrefab;
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private GameObject blackHolePrefab;
    [SerializeField] private GameObject rangedEnemyPrefab;

    [Header("Kacheln")]
    [SerializeField, Min(10f)] private float tileSize = 40f;
    [SerializeField, Range(1, 4)] private int activeRadius = 1;
    [SerializeField, Range(1, 5)] private int unloadRadius = 2;

    [Header("Dichte pro Kachel (bei Distanz 0)")]
    [SerializeField, Min(0)] private int baseCrownsPerTile = 2;
    [SerializeField, Min(0)] private int baseObstaclesPerTile = 1;

    public static EndlessWorldController Instance { get; private set; }

    // Maximal je erreichte Distanz - fällt nicht zurück, wenn der Spieler wieder Richtung Start läuft.
    public int MaxDistanceIndex { get; private set; }

    private readonly Dictionary<Vector2Int, WorldTile> activeTiles = new Dictionary<Vector2Int, WorldTile>();
    private Vector2Int lastPlayerTile;
    private bool hasLastPlayerTile;

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
        if (LevelManager.Instance == null || !LevelManager.Instance.IsEndlessMode)
            return;

        HideWorldBoundary();
        RefreshTiles(Vector2Int.zero);
    }

    private void Update()
    {
        if (LevelManager.Instance == null || !LevelManager.Instance.IsEndlessMode)
            return;

        Vector2 playerPosition = ParticleSimulation.Instance != null
            ? ParticleSimulation.Instance.PlayerPosition
            : Vector2.zero;
        Vector2Int playerTile = WorldToTile(playerPosition);

        if (hasLastPlayerTile && playerTile == lastPlayerTile)
            return;

        hasLastPlayerTile = true;
        lastPlayerTile = playerTile;
        RefreshTiles(playerTile);
    }

    private Vector2Int WorldToTile(Vector2 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / tileSize),
            Mathf.FloorToInt(worldPosition.y / tileSize));
    }

    private void RefreshTiles(Vector2Int centerTile)
    {
        for (int dx = -activeRadius; dx <= activeRadius; dx++)
        {
            for (int dy = -activeRadius; dy <= activeRadius; dy++)
            {
                Vector2Int coord = new Vector2Int(centerTile.x + dx, centerTile.y + dy);
                if (!activeTiles.ContainsKey(coord))
                    SpawnTile(coord);
            }
        }

        List<Vector2Int> toRemove = null;
        foreach (KeyValuePair<Vector2Int, WorldTile> entry in activeTiles)
        {
            int distance = Mathf.Max(
                Mathf.Abs(entry.Key.x - centerTile.x),
                Mathf.Abs(entry.Key.y - centerTile.y));

            if (distance <= unloadRadius)
                continue;

            toRemove ??= new List<Vector2Int>();
            toRemove.Add(entry.Key);
        }

        if (toRemove == null)
            return;

        foreach (Vector2Int coord in toRemove)
        {
            Destroy(activeTiles[coord].Container.gameObject);
            activeTiles.Remove(coord);
        }
    }

    private void SpawnTile(Vector2Int coord)
    {
        int distanceIndex = Mathf.Max(Mathf.Abs(coord.x), Mathf.Abs(coord.y));
        MaxDistanceIndex = Mathf.Max(MaxDistanceIndex, distanceIndex);

        GameObject container = new GameObject($"Tile_{coord.x}_{coord.y}");
        container.transform.SetParent(transform);

        Vector2 center = new Vector2(
            (coord.x + 0.5f) * tileSize,
            (coord.y + 0.5f) * tileSize);
        Bounds bounds = new Bounds(center, new Vector3(tileSize, tileSize, 0f));

        WorldTile tile = new WorldTile
        {
            Coord = coord,
            Bounds = bounds,
            DistanceIndex = distanceIndex,
            Container = container.transform
        };
        activeTiles[coord] = tile;

        PopulateTile(tile);
    }

    private void PopulateTile(WorldTile tile)
    {
        // Startkachel bleibt sicher - keine Hindernisse direkt am Spawnpunkt.
        if (tile.DistanceIndex == 0)
        {
            SpawnInTile(crownPrefab, baseCrownsPerTile, tile, new System.Random(SeedFor(tile.Coord)));
            return;
        }

        System.Random random = new System.Random(SeedFor(tile.Coord));

        int obstacleCount = baseObstaclesPerTile + random.Next(0, 2 + tile.DistanceIndex / 2);
        int crownCount = baseCrownsPerTile + random.Next(0, 3);
        int laserCount = tile.DistanceIndex >= 4 && random.NextDouble() < 0.4 ? 1 : 0;
        int rangedEnemyCount = tile.DistanceIndex >= 5 && random.NextDouble() < 0.35 ? 1 : 0;
        int blackHoleCount = tile.DistanceIndex >= 8 && random.NextDouble() < 0.3 ? 1 : 0;

        SpawnInTile(obstaclePrefab, obstacleCount, tile, random);
        SpawnInTile(laserPrefab, laserCount, tile, random);
        SpawnInTile(rangedEnemyPrefab, rangedEnemyCount, tile, random);
        SpawnInTile(blackHolePrefab, blackHoleCount, tile, random);
        SpawnInTile(crownPrefab, crownCount, tile, random);
    }

    private void SpawnInTile(GameObject prefab, int count, WorldTile tile, System.Random random)
    {
        if (prefab == null)
            return;

        for (int i = 0; i < count; i++)
        {
            Vector2 position = new Vector2(
                Mathf.Lerp(tile.Bounds.min.x, tile.Bounds.max.x, (float)random.NextDouble()),
                Mathf.Lerp(tile.Bounds.min.y, tile.Bounds.max.y, (float)random.NextDouble()));

            Instantiate(prefab, position, Quaternion.identity, tile.Container);
        }
    }

    private static int SeedFor(Vector2Int coord)
    {
        return coord.x * 92821 + coord.y * 68917 + 17;
    }

    private void HideWorldBoundary()
    {
        GameObject boundaryObject = GameObject.Find("WorldBoundary");
        LineRenderer boundary = boundaryObject != null ? boundaryObject.GetComponent<LineRenderer>() : null;
        if (boundary != null)
            boundary.enabled = false;
    }
}
