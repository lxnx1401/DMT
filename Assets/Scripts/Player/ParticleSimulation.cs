using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Rendering;

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
        public float alive;
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

    [Header("Weltgrenze")]
    [SerializeField, Min(0f)] private float boundaryMargin = 3f; // Sicherheitsabstand, damit der ausgefranste Schwarmrand nicht optisch über die Linie geht

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

    [Header("Stabilität")]
    [SerializeField] private int substeps = 4;
    [SerializeField] private float maxSpeed = 60f;
    [SerializeField] private float mouseVelSmoothing = 0.15f; // niedriger = träger/ruhiger
    [SerializeField] private float mousePosSmoothing = 25f;   // höher = reaktionsschneller, niedriger = glatter

    private ComputeBuffer particleBuffer;
    private ComputeBuffer mouseHistoryBuffer;
    private ComputeBuffer mouseSpeedHistoryBuffer;
    private ComputeBuffer obstacleBuffer;
    private ComputeBuffer blackHoleBuffer;
    private ComputeBuffer aliveCounterBuffer;
    private ComputeBuffer damageCounterBuffer;

    private ObstacleData[] obstacles;
    private BlackHoleData[] blackHoles;
    private Particle[] particles;
    private Vector2[] mouseHistory;
    private float[] mouseSpeedHistory;
    private readonly int[] zeroReset = { 0 };

    private float sampleTimer;
    private int kernelIndex;
    private Material particleMaterial;

    private Vector2 lastMouseWorld;
    private Vector2 lastMouseScreen;
    private Vector2 smoothedMouseWorld;
    private Vector2 smoothedMouseVelocity;
    private bool hasLastMouse;

    private float baseTangentialStiffness;
    private float baseLateralStiffness;

    private bool readbackInFlight;   // verhindert überlappende Requests
    private int pendingDamage;       // aus TakeDamage, wird 1x pro Frame konsumiert

    public Vector2 HeadPosition { get; private set; }
    public Vector2 PlayerPosition { get; private set; }
    private Rigidbody2D rb;

    private float currentHue;

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
        rb.bodyType = RigidbodyType2D.Kinematic;
        ResetMouse();
        PlayerPosition = GetMouseWorld();

        // --- Partikel-Buffer ---
        particleBuffer = new ComputeBuffer(particleCapacity, Marshal.SizeOf<Particle>());
        particles = new Particle[particleCapacity];

        for (int i = 0; i < particleCapacity; i++)
        {
            particles[i].position = Random.insideUnitCircle * 5f;
            particles[i].velocity = Vector2.zero;
            particles[i].offset = Random.insideUnitCircle;
            particles[i].damping = Random.Range(0.86f, 0.94f);
            particles[i].forceScale = Random.Range(0.75f, 1.25f);
            particles[i].alive = 1f;
        }
        particleBuffer.SetData(particles);

        // --- Trail-History-Buffer ---
        mouseHistory = new Vector2[trailHistoryLength];
        mouseSpeedHistory = new float[trailHistoryLength];
        Vector2 initialMouse = GetMouseWorld();
        for (int i = 0; i < trailHistoryLength; i++)
        {
            mouseHistory[i] = initialMouse;
            mouseSpeedHistory[i] = 0f;
        }

        mouseHistoryBuffer = new ComputeBuffer(trailHistoryLength, sizeof(float) * 2);
        mouseHistoryBuffer.SetData(mouseHistory);

        mouseSpeedHistoryBuffer = new ComputeBuffer(trailHistoryLength, sizeof(float));
        mouseSpeedHistoryBuffer.SetData(mouseSpeedHistory);

        // --- Alive-/Damage-Counter (je nur EINMAL angelegt) ---
        aliveCounterBuffer = new ComputeBuffer(1, sizeof(int));
        aliveCounterBuffer.SetData(zeroReset);

        damageCounterBuffer = new ComputeBuffer(1, sizeof(int));
        damageCounterBuffer.SetData(zeroReset);

        // --- Kernel + alle Buffer-Bindings ---
        kernelIndex = simulationShader.FindKernel("CSMain");
        UpdateObstacleBuffer();
        UpdateBlackHoleBuffer();

        simulationShader.SetBuffer(kernelIndex, "particles", particleBuffer);
        simulationShader.SetBuffer(kernelIndex, "mouseHistory", mouseHistoryBuffer);
        simulationShader.SetBuffer(kernelIndex, "mouseSpeedHistory", mouseSpeedHistoryBuffer);
        simulationShader.SetBuffer(kernelIndex, "aliveCounter", aliveCounterBuffer);
        simulationShader.SetBuffer(kernelIndex, "damageCounter", damageCounterBuffer);

        simulationShader.SetInt("particleCapacity", particleCapacity);
        simulationShader.SetInt("historyCount", trailHistoryLength);
        simulationShader.SetInt("substeps", substeps);
        simulationShader.SetFloat("minSpeed", minSpeed);
        simulationShader.SetFloat("maxSpeed2", maxSpeed2);

        simulationShader.SetFloat("neighborRadius", 1.5f);
        simulationShader.SetFloat("preferredDistance", 0.5f);
        simulationShader.SetFloat("cohesionStrength", 20f);
        simulationShader.SetFloat("separationStrength", 20f);

        particleMaterial = new Material(Shader.Find("Custom/ParticleShader"));
        particleMaterial.SetFloat("_ParticleRadius", 0.1f);
        particleMaterial.SetColor("_TintColor", Color.white);

        PerformanceAnalyzer.Instance?.ResetRun(ActiveParticles);
    }

    Vector2 GetMouseWorld()
    {
        if (Mouse.current == null)
            return Vector2.zero;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        return Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
    }

    private Vector2 ClampToPlayArea(Vector2 point)
    {
        // Endlos-Modus: kein Rand mehr, echtes unendliches Durchwandern über Kacheln.
        if (LevelManager.Instance != null && LevelManager.Instance.IsEndlessMode)
            return point;

        LevelData level = LevelManager.Instance?.CurrentLevel;
        if (level == null)
            return point;

        Bounds bounds = level.PlayAreaBounds;
        float marginX = Mathf.Min(boundaryMargin, bounds.size.x * 0.5f);
        float marginY = Mathf.Min(boundaryMargin, bounds.size.y * 0.5f);
        point.x = Mathf.Clamp(point.x, bounds.min.x + marginX, bounds.max.x - marginX);
        point.y = Mathf.Clamp(point.y, bounds.min.y + marginY, bounds.max.y - marginY);
        return point;
    }

    void Update()
    {
        if (Mouse.current == null)
            return;

        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
        mouseWorld = ClampToPlayArea(mouseWorld);

        float posSmoothFactor = 1f - Mathf.Exp(-mousePosSmoothing * dt);
        smoothedMouseWorld = Vector2.Lerp(smoothedMouseWorld, mouseWorld, posSmoothFactor);

        DifficultyTuning tuning = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.CurrentTuning
            : null;

        float blackHoleMultiplier = tuning != null ? tuning.blackHoleDamageMultiplier : 1f;
        simulationShader.SetFloat("blackHoleStrengthMultiplier", blackHoleMultiplier);

        float speedMultiplier = tuning != null ? tuning.playerSpeedMultiplier : 1f;
        Vector2 previousPlayerPosition = PlayerPosition;
        PlayerPosition = Vector2.MoveTowards(
            PlayerPosition,
            smoothedMouseWorld,
            basePlayerSpeed * speedMultiplier * dt);
        PlayerPosition = ClampToPlayArea(PlayerPosition);
        HeadPosition = PlayerPosition;

        float hueShiftSpeed = 0.015f;

        float playerSpeed = 0f;
        if (dt > 0.0001f)
        {
            playerSpeed = Vector2.Distance(previousPlayerPosition, PlayerPosition) / dt;
        }
        currentHue = Mathf.Repeat(
            currentHue + playerSpeed * hueShiftSpeed * dt,
            1f
        );

        particleMaterial.SetFloat("_StartHue", currentHue);

        if (rb != null)
            rb.position = PlayerPosition;
        else
            transform.position = PlayerPosition;

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
                mouseSpeedHistory[i] = mouseSpeedHistory[i - 1];
            }
            mouseHistory[0] = mouseWorld;
            mouseSpeedHistory[0] = smoothedMouseVelocity.magnitude;
            sampleTimer -= sampleInterval;
        }
        mouseHistoryBuffer.SetData(mouseHistory);
        mouseSpeedHistoryBuffer.SetData(mouseSpeedHistory);

        simulationShader.SetFloat("time", Time.time);
        simulationShader.SetFloat("deltaTime", dt);

        float cohesionMultiplier = tuning != null ? tuning.swarmCohesionMultiplier : 1f;
        simulationShader.SetFloat("tangentialStiffness", baseTangentialStiffness * cohesionMultiplier);
        simulationShader.SetFloat("lateralStiffness", baseLateralStiffness * cohesionMultiplier);
        simulationShader.SetFloat("trailWidth", trailWidth);
        simulationShader.SetFloat("idleWidth", idleWidth);
        simulationShader.SetFloat("tailTaper", tailTaper);
        simulationShader.SetFloat("wanderStrength", wanderStrength);
        simulationShader.SetFloat("stretchResponse", stretchResponse);
        simulationShader.SetFloat("maxSpeed", maxSpeed);

        // Schaden für diesen Frame reinreichen, Hilfszähler zurücksetzen
        simulationShader.SetInt("damageRequest", pendingDamage);
        damageCounterBuffer.SetData(zeroReset);
        pendingDamage = 0;

        aliveCounterBuffer.SetData(zeroReset);

        // WICHTIG: Dispatch + Readback + Draw laufen jeweils nur EINMAL pro Frame.
        simulationShader.Dispatch(kernelIndex, Mathf.CeilToInt(particleCapacity / 256f), 1, 1);


        if (!readbackInFlight)
        {
            readbackInFlight = true;
            AsyncGPUReadback.Request(aliveCounterBuffer, OnAliveCounterReadback);
        }

        particleMaterial.SetBuffer("particles", particleBuffer);
        Graphics.DrawProcedural(
            particleMaterial,
            new Bounds(new Vector3(PlayerPosition.x, PlayerPosition.y, 0f), Vector3.one * 1000),
            MeshTopology.Triangles,
            particleCapacity * 6 // IMMER volle Kapazität -- alive-Flag entscheidet Sichtbarkeit, nicht der Index
        );
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || ActiveParticles <= 0)
            return;

        StartCoroutine(DamageFlash());
        pendingDamage += amount; // GPU killt per Ticket-System tatsächlich sichtbare, lebende Partikel
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

        obstacleBuffer = new ComputeBuffer(
            Mathf.Max(1, obstacles.Length),
            Marshal.SizeOf<ObstacleData>()
        );

        if (obstacles.Length > 0)
            obstacleBuffer.SetData(obstacles);

        simulationShader.SetBuffer(kernelIndex, "obstacles", obstacleBuffer);
        simulationShader.SetInt("obstacleCount", obstacles.Length);
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

        simulationShader.SetBuffer(kernelIndex, "blackHoles", blackHoleBuffer);
        simulationShader.SetInt("blackHoleCount", blackHoles.Length);
    }

    // Wird von EndlessWorldController nach jedem Kachel-Refresh aufgerufen, damit neu gespawnte
    // Hindernisse/Black Holes auch in der GPU-Kollision/-Anziehung berücksichtigt werden - im
    // Level-Modus reicht der einmalige Aufruf in Start(), weil dort schon alles vorab spawnt.
    public void RefreshHazardBuffers()
    {
        UpdateObstacleBuffer();
        UpdateBlackHoleBuffer();
    }

    private void OnAliveCounterReadback(AsyncGPUReadbackRequest request)
    {
        readbackInFlight = false;
        if (request.hasError)
        {
            Debug.LogWarning("[ParticleSimulation] Readback-Fehler!");
            return;
        }

        int aliveNow = Mathf.Clamp(request.GetData<int>()[0], 0, particleCapacity);
        ApplyAliveCount(aliveNow);
    }

    // Uebernimmt die vom GPU gemeldete, absolute Zahl noch lebender Partikel -- bewusst absolut statt
    // als aufsummiertes Delta, damit ein wegen readbackInFlight uebersprungenes Readback sich beim
    // naechsten erfolgreichen Readback von selbst korrigiert, statt Tode dauerhaft zu verlieren
    // (das fuehrte vorher dazu, dass der Schwarm optisch laengst tot war, ActiveParticles aber nie 0 erreichte).
    private void ApplyAliveCount(int aliveNow)
    {
        int particlesBeforeDamage = ActiveParticles;
        if (aliveNow >= particlesBeforeDamage)
        {
            ActiveParticles = aliveNow;
            return;
        }

        int lost = particlesBeforeDamage - aliveNow;
        ActiveParticles = aliveNow;

        // ActiveParticles ist reine Buchhaltung fürs UI/Score -- beeinflusst weder
        // Dispatch noch Draw (siehe Update(): particleCapacity wird immer voll genutzt).
        PerformanceAnalyzer.Instance?.RegisterParticleLoss(lost);

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
        aliveCounterBuffer?.Release();
        damageCounterBuffer?.Release();
    }
}