using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem;

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

    [SerializeField] private ComputeShader simulationShader;
    [SerializeField] private int particleCount = 1000;

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
    private ComputeBuffer particleBuffer;
    private ComputeBuffer mouseHistoryBuffer;
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

    private float[] mouseSpeedHistory;
    private ComputeBuffer mouseSpeedHistoryBuffer;

    void Start()
    {
        particleBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf<Particle>());
        particles = new Particle[particleCount];

        for (int i = 0; i < particleCount; i++)
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
        simulationShader.SetBuffer(kernelIndex, "particles", particleBuffer);
        simulationShader.SetBuffer(kernelIndex, "mouseHistory", mouseHistoryBuffer);
        simulationShader.SetBuffer(kernelIndex, "mouseSpeedHistory", mouseSpeedHistoryBuffer); // NEU
        simulationShader.SetInt("particleCount", particleCount);
        simulationShader.SetInt("historyCount", trailHistoryLength);
        simulationShader.SetInt("substeps", substeps);

        // NEU: fehlte komplett
        particleMaterial = new Material(Shader.Find("Custom/ParticleShader"));
        particleMaterial.SetFloat("_ParticleRadius", 8.0f);
    }

    Vector2 GetMouseWorld()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        return Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
    }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);

        Vector2 mouseWorld = GetMouseWorld();

        Vector2 rawMouseVelocity = Vector2.zero;
        if (hasLastMouse && dt > 0.0001f)
            rawMouseVelocity = (mouseWorld - lastMouseWorld) / dt;
        lastMouseWorld = mouseWorld;
        hasLastMouse = true;

        smoothedMouseVelocity = Vector2.Lerp(smoothedMouseVelocity, rawMouseVelocity, mouseVelSmoothing);

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

        simulationShader.SetFloat("time", Time.time);
        simulationShader.SetFloat("deltaTime", dt);
        // simulationShader.SetFloat("mouseSpeed", ...) <- Zeile entfernen, nicht mehr gebraucht
        simulationShader.SetFloat("tangentialStiffness", tangentialStiffness);
        simulationShader.SetFloat("lateralStiffness", lateralStiffness);
        simulationShader.SetFloat("trailWidth", trailWidth);
        simulationShader.SetFloat("idleWidth", idleWidth);
        simulationShader.SetFloat("tailTaper", tailTaper);
        simulationShader.SetFloat("wanderStrength", wanderStrength);
        simulationShader.SetFloat("stretchResponse", stretchResponse);
        simulationShader.SetFloat("maxSpeed", maxSpeed);

        simulationShader.Dispatch(kernelIndex, Mathf.CeilToInt(particleCount / 256f), 1, 1);

        particleMaterial.SetBuffer("particles", particleBuffer);
        Graphics.DrawProcedural(
            particleMaterial,
            new Bounds(Vector3.zero, Vector3.one * 1000),
            MeshTopology.Triangles,
            particleCount * 6
        );
    }

    private void OnDestroy()
    {
        particleBuffer?.Release();
        mouseHistoryBuffer?.Release();
        mouseSpeedHistoryBuffer?.Release();
    }
}