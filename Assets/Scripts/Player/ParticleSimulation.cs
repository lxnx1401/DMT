using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem;
using System.Collections;

public class ParticleSimulation : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    struct Particle
    {
        public Vector2 position;
        public Vector2 velocity;
        public Vector2 offset;
        public float damping;
        public float forceScale;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct ObstacleData
    {
        public Vector2 position;
        public float radius;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct BlackHoleData
    {
        public Vector2 position;
        public float radius;
        public float pullRadius;
        public float strength;
    }

    [SerializeField] private ComputeShader simulationShader;
    [SerializeField] private int particleCapacity = 1000;

    public int ActiveParticles { get; private set; }

    public int MaxParticles => particleCapacity;

    [Header("Trail / Path")]
    [SerializeField] private int trailHistoryLength = 48;
    [SerializeField] private float trailDuration = 0.35f; // wie lange die Spur "hält" (Sekunden)

    [Header("Tuning")]
    [SerializeField] private float tangentialStiffness = 140f;
    [SerializeField] private float lateralStiffness = 260f;    // > tangential = engere Formhaltung
    [SerializeField] private float trailWidth = 0.4f;
    [SerializeField] private float idleWidth = 2.5f;
    [SerializeField] private float tailTaper = 0.1f;           // 0 = spitz zulaufend, 1 = keine Verjüngung
    [SerializeField] private float wanderStrength = 0.12f;
    [SerializeField] private float stretchResponse = 0.08f;
    [SerializeField] private float minSpeed = 0.15f;
    [SerializeField] private float maxSpeed2 = 1.2f;
    [SerializeField, Min(0.1f)] private float basePlayerSpeed = 18f;
    private ComputeBuffer particleBuffer;
    private ComputeBuffer mouseHistoryBuffer;
    private ComputeBuffer obstacleBuffer;
    private ObstacleData[] obstacles;
    private ComputeBuffer blackHoleBuffer;
    private BlackHoleData[] blackHoles;
    private Particle[] particles;
    private Vector2[] mouseHistory;
    private float sampleTimer;

    private int kernelIndex;
    private Material particleMaterial;

    private Vector2 lastMouseWorld;
    private bool hasLastMouse;

    [Header("Stabilität")]
    [SerializeField] private int substeps = 4;
    [SerializeField] private float maxSpeed = 60f;
    [SerializeField] private float mouseVelSmoothing = 0.15f; // niedriger = träger/ruhiger


    private Vector2 smoothedMouseVelocity;
    private float baseTangentialStiffness;
    private float baseLateralStiffness;

    private float[] mouseSpeedHistory;
    private ComputeBuffer mouseSpeedHistoryBuffer;
    public Vector2 HeadPosition { get; private set; }
    public Vector2 PlayerPosition { get; private set; }
    private Rigidbody2D rb;

    public static ParticleSimulation Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        baseTangentialStiffness = tangentialStiffness;
        baseLateralStiffness = lateralStiffness;
        ActiveParticles = particleCapacity;
        rb = GetComponent<Rigidbody2D>();
        ResetMouse();
        PlayerPosition = GetMouseWorld();
        particleBuffer = new ComputeBuffer(particleCapacity, Marshal.SizeOf<Particle>());
        particles = new Particle[particleCapacity];

        for (int i = 0; i < particleCapacity; i++)
        {
            particles[i].position = Random.insideUnitCircle * 5f;
            particles[i].velocity = Vector2.zero;
            particles[i].offset = Random.insideUnitCircle;
            // engerer Bereich als vorher -> keine Resonanz-Kombinationen mehr
            particles[i].damping = Random.Range(0.86f, 0.94f);
            particles[i].forceScale = Random.Range(0.75f, 1.25f);
        }
        particleBuffer.SetData(particles);

        mouseHistory = new Vector2[trailHistoryLength];
        mouseSpeedHistory = new float[trailHistoryLength]; // NEU
        Vector2 initialMouse = GetMouseWorld();
        for (int i = 0; i < trailHistoryLength; i++)
        {
            mouseHistory[i] = initialMouse;
            mouseSpeedHistory[i] = 0f;
        }

        mouseHistoryBuffer = new ComputeBuffer(trailHistoryLength, sizeof(float) * 2);
        mouseHistoryBuffer.SetData(mouseHistory);

        mouseSpeedHistoryBuffer = new ComputeBuffer(trailHistoryLength, sizeof(float)); // NEU
        mouseSpeedHistoryBuffer.SetData(mouseSpeedHistory);

        kernelIndex = simulationShader.FindKernel("CSMain");
        UpdateObstacleBuffer();
        UpdateBlackHoleBuffer();
        simulationShader.SetBuffer(kernelIndex, "particles", particleBuffer);
        simulationShader.SetBuffer(kernelIndex, "mouseHistory", mouseHistoryBuffer);
        simulationShader.SetBuffer(kernelIndex, "mouseSpeedHistory", mouseSpeedHistoryBuffer); // NEU
        simulationShader.SetInt("particleCount", ActiveParticles);
        simulationShader.SetInt("historyCount", trailHistoryLength);
        simulationShader.SetInt("substeps", substeps);
        simulationShader.SetFloat("minSpeed", minSpeed);
        simulationShader.SetFloat("maxSpeed2", maxSpeed2);

        // NEU: fehlte komplett
        particleMaterial = new Material(Shader.Find("Custom/ParticleShader"));
        particleMaterial.SetFloat("_ParticleRadius", 0.1f);
        particleMaterial.SetColor("_TintColor", Color.white);

        PerformanceAnalyzer.Instance?.ResetRun(ActiveParticles);
    }

    Vector2 GetMouseWorld()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        return Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
    }

    private Vector2 ClampToPlayArea(Vector2 point)
    {
        LevelData level = LevelManager.Instance?.CurrentLevel;
        if (level == null)
            return point;

        Bounds bounds = level.PlayAreaBounds;
        point.x = Mathf.Clamp(point.x, bounds.min.x, bounds.max.x);
        point.y = Mathf.Clamp(point.y, bounds.min.y, bounds.max.y);
        return point;
    }
    private Vector2 lastMouseScreen;

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
        mouseWorld = ClampToPlayArea(mouseWorld);

        // NEU: PlayerPosition wieder aktualisieren
        DifficultyTuning tuning = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.CurrentTuning
            : null;
        float speedMultiplier = tuning != null ? tuning.playerSpeedMultiplier : 1f;
        PlayerPosition = Vector2.MoveTowards(
            PlayerPosition,
            mouseWorld,
            basePlayerSpeed * speedMultiplier * Time.deltaTime);
        PlayerPosition = ClampToPlayArea(PlayerPosition);
        HeadPosition = PlayerPosition;

        Vector2 rawScreenVelocity = Vector2.zero;
        if (hasLastMouse && dt > 0.0001f)
            rawScreenVelocity = (mouseScreen - lastMouseScreen) / dt;
        lastMouseScreen = mouseScreen;
        lastMouseWorld = mouseWorld;
        hasLastMouse = true;

        float smoothFactor = 1f - Mathf.Exp(-mouseVelSmoothing * dt * 60f);
        smoothedMouseVelocity = Vector2.Lerp(smoothedMouseVelocity, rawScreenVelocity, smoothFactor);

        float sampleInterval = trailDuration / trailHistoryLength;
        sampleTimer += dt;
        while (sampleTimer >= sampleInterval)
        {
            for (int i = trailHistoryLength - 1; i > 0; i--)
            {
                mouseHistory[i] = mouseHistory[i - 1];
                mouseSpeedHistory[i] = mouseSpeedHistory[i - 1]; // NEU
            }
            mouseHistory[0] = mouseWorld;
            mouseSpeedHistory[0] = smoothedMouseVelocity.magnitude; // NEU
            sampleTimer -= sampleInterval;
        }
        mouseHistoryBuffer.SetData(mouseHistory);
        mouseSpeedHistoryBuffer.SetData(mouseSpeedHistory); // NEU

        if (rb != null)
            rb.MovePosition(PlayerPosition);
        else
            transform.position = PlayerPosition;

        simulationShader.SetFloat("time", Time.time);
        simulationShader.SetFloat("deltaTime", dt);
        // simulationShader.SetFloat("mouseSpeed", ...) <- Zeile entfernen, nicht mehr gebraucht
        float cohesionMultiplier = tuning != null ? tuning.swarmCohesionMultiplier : 1f;
        simulationShader.SetFloat(
            "tangentialStiffness", baseTangentialStiffness * cohesionMultiplier);
        simulationShader.SetFloat(
            "lateralStiffness", baseLateralStiffness * cohesionMultiplier);
        simulationShader.SetFloat("trailWidth", trailWidth);
        simulationShader.SetFloat("idleWidth", idleWidth);
        simulationShader.SetFloat("tailTaper", tailTaper);
        simulationShader.SetFloat("wanderStrength", wanderStrength);
        simulationShader.SetFloat("stretchResponse", stretchResponse);
        simulationShader.SetFloat("maxSpeed", maxSpeed);

        simulationShader.Dispatch(kernelIndex, Mathf.CeilToInt(particleCapacity / 256f), 1, 1);

        particleMaterial.SetBuffer("particles", particleBuffer);
        Graphics.DrawProcedural(
            particleMaterial,
            new Bounds(Vector3.zero, Vector3.one * 1000),
            MeshTopology.Triangles,
            ActiveParticles * 6
        );
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || ActiveParticles <= 0)
            return;

        StartCoroutine(DamageFlash());
        RemoveParticles(amount);
    }

    IEnumerator DamageFlash()
    {
        particleMaterial.SetColor("_TintColor", new Color(1, 0.4f, 0.4f, 0.75f));

        yield return new WaitForSeconds(0.08f);

        particleMaterial.SetColor("_TintColor", Color.white);
    }

    void UpdateObstacleBuffer()
    {
        Obstacle[] sceneObstacles = FindObjectsByType<Obstacle>(FindObjectsSortMode.None);

        obstacles = new ObstacleData[sceneObstacles.Length];

        for (int i = 0; i < sceneObstacles.Length; i++)
        {
            obstacles[i].position = sceneObstacles[i].transform.position;
            obstacles[i].radius = sceneObstacles[i].Radius;
        }


        if (obstacleBuffer != null)
            obstacleBuffer.Release();


        if (obstacles.Length > 0)
        {
            obstacleBuffer = new ComputeBuffer(
                obstacles.Length,
                Marshal.SizeOf<ObstacleData>()
            );

            obstacleBuffer.SetData(obstacles);

            simulationShader.SetBuffer(
                kernelIndex,
                "obstacles",
                obstacleBuffer
            );

            simulationShader.SetInt(
                "obstacleCount",
                obstacles.Length
            );
        }
        else
        {
            simulationShader.SetInt(
                "obstacleCount",
                0
            );
        }
    }

    void UpdateBlackHoleBuffer()
    {
        BlackHole[] sceneBlackHoles = FindObjectsByType<BlackHole>(FindObjectsSortMode.None);

        blackHoles = new BlackHoleData[sceneBlackHoles.Length];

        for (int i = 0; i < sceneBlackHoles.Length; i++)
        {
            blackHoles[i].position = sceneBlackHoles[i].transform.position;
            blackHoles[i].radius = sceneBlackHoles[i].Radius;
            blackHoles[i].pullRadius = sceneBlackHoles[i].PullRadius;
            blackHoles[i].strength = sceneBlackHoles[i].Strength;
        }

        if (blackHoleBuffer != null)
            blackHoleBuffer.Release();

        blackHoleBuffer = new ComputeBuffer(
            Mathf.Max(1, blackHoles.Length),
            Marshal.SizeOf<BlackHoleData>()
        );

        if (blackHoles.Length > 0)
            blackHoleBuffer.SetData(blackHoles);

        simulationShader.SetBuffer(
            kernelIndex,
            "blackHoles",
            blackHoleBuffer
        );

        simulationShader.SetInt(
            "blackHoleCount",
            blackHoles.Length
        );
    }

    public void RemoveParticles(int amount)
    {
        int particlesBeforeDamage = ActiveParticles;
        int actualLoss = Mathf.Min(ActiveParticles, Mathf.Max(0, amount));
        ActiveParticles -= actualLoss;

        simulationShader.SetInt("particleCount", ActiveParticles);
        PerformanceAnalyzer.Instance?.RegisterParticleLoss(actualLoss);

        if (particlesBeforeDamage > 0 && ActiveParticles == 0)
        {
            PerformanceAnalyzer.Instance?.CompleteSection(0);
            GameManager.Instance?.Lose();
            enabled = false;
        }
    }

    public void ResetMouse()
    {
        hasLastMouse = false;
        smoothedMouseVelocity = Vector2.zero;
    }

    private void OnDestroy()
    {
        particleBuffer?.Release();
        mouseHistoryBuffer?.Release();
        mouseSpeedHistoryBuffer?.Release();
        obstacleBuffer?.Release();
        blackHoleBuffer?.Release();
    }
}
