using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Rendering;

public class ParticleSimulation : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Particle
    {
        public Vector2 position;
        public Vector2 velocity;
        public Vector2 offset;
        public float damping;
        public float forceScale;
        public float alive;
    }

    [SerializeField] private ComputeShader simulationShader;
    [SerializeField] private int particleCapacity = 1000;

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

    public int ActiveParticles => aliveTracker.ActiveParticles;
    public int MaxParticles => particleCapacity;
    public Vector2 HeadPosition => PlayerPosition;
    // Hindernis-/Gegner-Spawner laufen absichtlich VOR ParticleSimulation.Start() (siehe deren
    // negative DefaultExecutionOrder) und fragen PlayerPosition schon vor dessen Initialisierung ab --
    // motion ist bis dahin noch null, daher hier defensiv statt eines NullReferenceException-Absturzes.
    public Vector2 PlayerPosition => motion != null ? motion.PlayerPosition : Vector2.zero;

    public static ParticleSimulation Instance;

    private ComputeBuffer particleBuffer;
    private ComputeBuffer mouseHistoryBuffer;
    private ComputeBuffer mouseSpeedHistoryBuffer;
    private ComputeBuffer aliveCounterBuffer;
    private ComputeBuffer damageCounterBuffer;

    private readonly int[] zeroReset = { 0 };

    private int kernelIndex;
    private Material particleMaterial;
    private Rigidbody2D rb;

    private float baseTangentialStiffness;
    private float baseLateralStiffness;
    private float currentHue;
    private int pendingDamage;

    private PlayerMotionController motion;
    private MouseTrailSampler trail;
    private HazardBufferManager hazards;
    private AliveParticleTracker aliveTracker;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        baseTangentialStiffness = tangentialStiffness;
        baseLateralStiffness = lateralStiffness;

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        Vector2 startPosition = GetMouseWorld();
        motion = new PlayerMotionController(mousePosSmoothing, mouseVelSmoothing, basePlayerSpeed, boundaryMargin, startPosition);

        SetupParticleBuffer();
        SetupTrailBuffers(startPosition);
        SetupCounterBuffers();

        kernelIndex = simulationShader.FindKernel("CSMain");

        hazards = new HazardBufferManager(simulationShader, kernelIndex);
        hazards.RefreshAll();

        aliveTracker = new AliveParticleTracker(aliveCounterBuffer, particleCapacity);
        aliveTracker.ParticlesLost += lost => PerformanceAnalyzer.Instance?.RegisterParticleLoss(lost);
        aliveTracker.AllParticlesLost += HandleAllParticlesLost;

        BindStaticShaderBuffers();
        BindStaticShaderParams();

        particleMaterial = CreateParticleMaterial();

        PerformanceAnalyzer.Instance?.ResetRun(ActiveParticles);
    }

    private void SetupParticleBuffer()
    {
        particleBuffer = new ComputeBuffer(particleCapacity, Marshal.SizeOf<Particle>());
        var particles = new Particle[particleCapacity];

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
    }

    private void SetupTrailBuffers(Vector2 startPosition)
    {
        trail = new MouseTrailSampler(trailHistoryLength, trailDuration, startPosition);

        mouseHistoryBuffer = new ComputeBuffer(trailHistoryLength, sizeof(float) * 2);
        mouseHistoryBuffer.SetData(trail.Positions);

        mouseSpeedHistoryBuffer = new ComputeBuffer(trailHistoryLength, sizeof(float));
        mouseSpeedHistoryBuffer.SetData(trail.Speeds);
    }

    private void SetupCounterBuffers()
    {
        aliveCounterBuffer = new ComputeBuffer(1, sizeof(int));
        aliveCounterBuffer.SetData(zeroReset);

        damageCounterBuffer = new ComputeBuffer(1, sizeof(int));
        damageCounterBuffer.SetData(zeroReset);
    }

    private void BindStaticShaderBuffers()
    {
        simulationShader.SetBuffer(kernelIndex, "particles", particleBuffer);
        simulationShader.SetBuffer(kernelIndex, "mouseHistory", mouseHistoryBuffer);
        simulationShader.SetBuffer(kernelIndex, "mouseSpeedHistory", mouseSpeedHistoryBuffer);
        simulationShader.SetBuffer(kernelIndex, "aliveCounter", aliveCounterBuffer);
        simulationShader.SetBuffer(kernelIndex, "damageCounter", damageCounterBuffer);
    }

    private void BindStaticShaderParams()
    {
        simulationShader.SetInt("particleCapacity", particleCapacity);
        simulationShader.SetInt("historyCount", trailHistoryLength);
        simulationShader.SetInt("substeps", substeps);
        simulationShader.SetFloat("minSpeed", minSpeed);
        simulationShader.SetFloat("maxSpeed2", maxSpeed2);

        simulationShader.SetFloat("neighborRadius", 1.5f);
        simulationShader.SetFloat("preferredDistance", 0.5f);
        simulationShader.SetFloat("cohesionStrength", 20f);
        simulationShader.SetFloat("separationStrength", 20f);
    }

    private Material CreateParticleMaterial()
    {
        var mat = new Material(Shader.Find("Custom/ParticleShader"));
        mat.SetFloat("_ParticleRadius", 0.1f);
        mat.SetColor("_TintColor", Color.white);
        mat.SetColor("_FlashColor", Color.red);
        mat.SetFloat("_FlashAmount", 0f);
        mat.SetFloat("_FlashBrightness", 2.5f);
        return mat;
    }

    Vector2 GetMouseWorld()
    {
        if (Mouse.current == null)
            return Vector2.zero;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        return Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
    }

    void Update()
    {
        if (Mouse.current == null)
            return;

        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);

        DifficultyTuning tuning = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.CurrentTuning
            : null;
        float blackHoleMultiplier = tuning != null ? tuning.blackHoleDamageMultiplier : 1f;
        float speedMultiplier = tuning != null ? tuning.playerSpeedMultiplier : 1f;
        float cohesionMultiplier = tuning != null ? tuning.swarmCohesionMultiplier : 1f;

        float playerSpeed = motion.Tick(dt, speedMultiplier);
        UpdateHue(playerSpeed, dt);

        if (rb != null)
            rb.position = PlayerPosition;
        else
            transform.position = PlayerPosition;

        if (trail.Sample(dt, motion.LastMouseWorld, motion.SmoothedMouseVelocity.magnitude))
        {
            mouseHistoryBuffer.SetData(trail.Positions);
            mouseSpeedHistoryBuffer.SetData(trail.Speeds);
        }

        PushPerFrameShaderParams(dt, blackHoleMultiplier, cohesionMultiplier);

        aliveTracker.ResetForFrame();

        // WICHTIG: Dispatch + Readback + Draw laufen jeweils nur EINMAL pro Frame.
        simulationShader.Dispatch(kernelIndex, Mathf.CeilToInt(particleCapacity / 256f), 1, 1);
        aliveTracker.RequestReadback();

        DrawParticles();
    }

    private void UpdateHue(float playerSpeed, float dt)
    {
        const float hueShiftSpeed = 0.015f;
        currentHue = Mathf.Repeat(currentHue + playerSpeed * hueShiftSpeed * dt, 1f);
        particleMaterial.SetFloat("_StartHue", currentHue);
    }

    private void PushPerFrameShaderParams(float dt, float blackHoleMultiplier, float cohesionMultiplier)
    {
        simulationShader.SetFloat("blackHoleStrengthMultiplier", blackHoleMultiplier);
        simulationShader.SetFloat("time", Time.time);
        simulationShader.SetFloat("deltaTime", dt);

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
    }

    private void DrawParticles()
    {
        Debug.DrawLine(
            PlayerPosition + Vector2.left * 1f,
            PlayerPosition + Vector2.right * 1f,
            Color.red
        );

        Debug.DrawLine(
            PlayerPosition + Vector2.down * 1f,
            PlayerPosition + Vector2.up * 1f,
            Color.red
        );
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
        Debug.Log($"TakeDamage Frame: {Time.frameCount}");
        if (amount <= 0 || ActiveParticles <= 0)
            return;

        StartCoroutine(DamageFlash());
        pendingDamage += amount; // GPU killt per Ticket-System tatsächlich sichtbare, lebende Partikel
    }

    IEnumerator DamageFlash()
    {
        particleMaterial.SetFloat("_FlashAmount", 1f);

        // Rot etwas länger halten
        yield return new WaitForSeconds(0.15f);

        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            particleMaterial.SetFloat("_FlashAmount", 1f - Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        particleMaterial.SetFloat("_FlashAmount", 0f);
    }

    private void HandleAllParticlesLost()
    {
        PerformanceAnalyzer.Instance?.CompleteSection(0);
        GameManager.Instance?.Lose();
        enabled = false;
    }

    // Wird von EndlessWorldController nach jedem Kachel-Refresh aufgerufen, damit neu gespawnte
    // Hindernisse/Black Holes auch in der GPU-Kollision/-Anziehung berücksichtigt werden - im
    // Level-Modus reicht der einmalige Aufruf in Start(), weil dort schon alles vorab spawnt.
    public void RefreshHazardBuffers()
    {
        hazards.RefreshAll();
    }

    public void ResetMouse()
    {
        motion?.ResetMouse();
    }

    private void OnDestroy()
    {
        particleBuffer?.Release();
        mouseHistoryBuffer?.Release();
        mouseSpeedHistoryBuffer?.Release();
        aliveCounterBuffer?.Release();
        damageCounterBuffer?.Release();
        hazards?.Release();
    }
}
