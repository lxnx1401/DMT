using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Verwaltet die ComputeBuffer für Hindernisse und Schwarze Löcher: Sammelt die
/// Szenen-Objekte ein, befüllt die Buffer neu und bindet sie an den Kernel.
/// </summary>
public class HazardBufferManager
{
    [StructLayout(LayoutKind.Sequential)]
    private struct ObstacleData
    {
        public Vector2 position;
        public float radius;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BlackHoleData
    {
        public Vector2 position;
        public float radius;
        public float pullRadius;
        public float strength;
    }

    private readonly ComputeShader shader;
    private readonly int kernelIndex;

    private ComputeBuffer obstacleBuffer;
    private ComputeBuffer blackHoleBuffer;

    public HazardBufferManager(ComputeShader shader, int kernelIndex)
    {
        this.shader = shader;
        this.kernelIndex = kernelIndex;
    }

    /// <summary>
    /// Liest alle Hindernisse und Black Holes aus der Szene neu ein und aktualisiert die
    /// GPU-Buffer. Wird einmalig in Start() sowie von EndlessWorldController nach jedem
    /// Kachel-Refresh aufgerufen, damit neu gespawnte Objekte in der GPU-Simulation ankommen.
    /// </summary>
    public void RefreshAll()
    {
        RefreshObstacles();
        RefreshBlackHoles();
    }

    private void RefreshObstacles()
    {
        Obstacle[] sceneObstacles = Object.FindObjectsByType<Obstacle>(FindObjectsSortMode.None);
        var data = new ObstacleData[sceneObstacles.Length];

        for (int i = 0; i < sceneObstacles.Length; i++)
        {
            data[i].position = sceneObstacles[i].transform.position;
            data[i].radius = sceneObstacles[i].Radius;
        }

        RecreateBuffer(ref obstacleBuffer, data, "obstacles", "obstacleCount");
    }

    private void RefreshBlackHoles()
    {
        BlackHole[] sceneBlackHoles = Object.FindObjectsByType<BlackHole>(FindObjectsSortMode.None);
        var data = new BlackHoleData[sceneBlackHoles.Length];

        for (int i = 0; i < sceneBlackHoles.Length; i++)
        {
            data[i].position = sceneBlackHoles[i].transform.position;
            data[i].radius = sceneBlackHoles[i].Radius;
            data[i].pullRadius = sceneBlackHoles[i].PullRadius;
            data[i].strength = sceneBlackHoles[i].Strength;
        }

        RecreateBuffer(ref blackHoleBuffer, data, "blackHoles", "blackHoleCount");
    }

    private void RecreateBuffer<T>(ref ComputeBuffer buffer, T[] data, string bufferName, string countName)
        where T : struct
    {
        buffer?.Release();
        buffer = new ComputeBuffer(Mathf.Max(1, data.Length), Marshal.SizeOf<T>());

        if (data.Length > 0)
            buffer.SetData(data);

        shader.SetBuffer(kernelIndex, bufferName, buffer);
        shader.SetInt(countName, data.Length);
    }

    public void Release()
    {
        obstacleBuffer?.Release();
        blackHoleBuffer?.Release();
        obstacleBuffer = null;
        blackHoleBuffer = null;
    }
}
