using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Liest den "aliveCounter"-Buffer asynchron vom GPU zurück und meldet Partikelverluste
/// per Event. Kapselt den Schutz vor überlappenden Requests, damit das Hauptskript sich
/// nicht mehr um readbackInFlight kümmern muss.
/// </summary>
public class AliveParticleTracker
{
    private readonly ComputeBuffer aliveCounterBuffer;
    private readonly int particleCapacity;
    private readonly int[] zeroReset = { 0 };

    private bool readbackInFlight;

    public int ActiveParticles { get; private set; }

    /// <summary>Feuert, wenn seit dem letzten Readback Partikel verloren gingen (Anzahl verlorener Partikel).</summary>
    public event System.Action<int> ParticlesLost;

    /// <summary>Feuert einmalig, sobald ActiveParticles von &gt;0 auf 0 fällt.</summary>
    public event System.Action AllParticlesLost;

    public AliveParticleTracker(ComputeBuffer aliveCounterBuffer, int particleCapacity)
    {
        this.aliveCounterBuffer = aliveCounterBuffer;
        this.particleCapacity = particleCapacity;
        ActiveParticles = particleCapacity;
    }

    /// <summary>Vor dem Dispatch pro Frame aufrufen, damit der GPU-Zähler wieder bei 0 startet.</summary>
    public void ResetForFrame()
    {
        aliveCounterBuffer.SetData(zeroReset);
    }

    /// <summary>Nach dem Dispatch aufrufen. Überspringt den Request, falls einer noch läuft.</summary>
    public void RequestReadback()
    {
        if (readbackInFlight)
            return;

        readbackInFlight = true;
        AsyncGPUReadback.Request(aliveCounterBuffer, OnReadback);
    }

    private void OnReadback(AsyncGPUReadbackRequest request)
    {
        readbackInFlight = false;

        if (request.hasError)
        {
            Debug.LogWarning("[AliveParticleTracker] Readback-Fehler!");
            return;
        }

        int aliveNow = Mathf.Clamp(request.GetData<int>()[0], 0, particleCapacity);
        Apply(aliveNow);
    }

    // Uebernimmt die vom GPU gemeldete, absolute Zahl noch lebender Partikel -- bewusst absolut
    // statt als aufsummiertes Delta, damit ein wegen readbackInFlight uebersprungenes Readback
    // sich beim naechsten erfolgreichen Readback von selbst korrigiert, statt Tode dauerhaft zu
    // verlieren (das fuehrte vorher dazu, dass der Schwarm optisch laengst tot war, ActiveParticles
    // aber nie 0 erreichte).
    private void Apply(int aliveNow)
    {
        int before = ActiveParticles;

        if (aliveNow >= before)
        {
            ActiveParticles = aliveNow;
            return;
        }

        int lost = before - aliveNow;
        ActiveParticles = aliveNow;

        ParticlesLost?.Invoke(lost);

        if (before > 0 && ActiveParticles == 0)
            AllParticlesLost?.Invoke();
    }
}
