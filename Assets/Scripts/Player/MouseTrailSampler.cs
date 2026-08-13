using UnityEngine;

/// <summary>
/// Hält die Bewegungs-Historie des Mauszeigers (Position + Geschwindigkeit) für die
/// Trail-Darstellung im Shader. Reine Datenklasse ohne MonoBehaviour-Abhängigkeit,
/// damit sie unabhängig von Update()/Start() testbar bleibt.
/// </summary>
public class MouseTrailSampler
{
    private readonly Vector2[] positions;
    private readonly float[] speeds;
    private readonly float sampleInterval;
    private float sampleTimer;

    public Vector2[] Positions => positions;
    public float[] Speeds => speeds;
    public int Length => positions.Length;

    public MouseTrailSampler(int historyLength, float trailDuration, Vector2 initialPosition)
    {
        positions = new Vector2[historyLength];
        speeds = new float[historyLength];
        sampleInterval = trailDuration / historyLength;

        for (int i = 0; i < historyLength; i++)
        {
            positions[i] = initialPosition;
            speeds[i] = 0f;
        }
    }

    /// <summary>
    /// Schiebt bei Bedarf (ggf. mehrfach, je nach vergangener Zeit) einen neuen Sample-Punkt
    /// vorne in die Historie. Gibt zurück, ob mindestens ein Sample genommen wurde, damit der
    /// Aufrufer die GPU-Buffer nur bei tatsächlicher Änderung neu befüllt.
    /// </summary>
    public bool Sample(float deltaTime, Vector2 currentPosition, float currentSpeed)
    {
        sampleTimer += deltaTime;
        bool sampled = false;

        while (sampleTimer >= sampleInterval)
        {
            for (int i = positions.Length - 1; i > 0; i--)
            {
                positions[i] = positions[i - 1];
                speeds[i] = speeds[i - 1];
            }

            positions[0] = currentPosition;
            speeds[0] = currentSpeed;

            sampleTimer -= sampleInterval;
            sampled = true;
        }

        return sampled;
    }
}
