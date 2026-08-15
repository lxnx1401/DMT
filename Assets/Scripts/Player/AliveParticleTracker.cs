using UnityEngine;
using UnityEngine.Rendering;


public class AliveParticleTracker
{
    private readonly ComputeBuffer aliveCounterBuffer;
    private readonly int particleCapacity;
    private readonly int[] zeroReset = { 0 };

    private bool readbackInFlight;

    public int ActiveParticles { get; private set; }


    public event System.Action<int> ParticlesLost;

    public event System.Action AllParticlesLost;

    public AliveParticleTracker(ComputeBuffer aliveCounterBuffer, int particleCapacity)
    {
        this.aliveCounterBuffer = aliveCounterBuffer;
        this.particleCapacity = particleCapacity;
        ActiveParticles = particleCapacity;
    }

    public void ResetForFrame()
    {
        aliveCounterBuffer.SetData(zeroReset);
    }

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
