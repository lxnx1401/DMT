using System.Runtime.InteropServices;
using UnityEngine;


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
