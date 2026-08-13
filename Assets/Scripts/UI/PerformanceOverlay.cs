using UnityEngine;

public class PerformanceOverlay : MonoBehaviour
{
    [SerializeField] private ParticleSimulation particleSimulation;

    [Header("Anzeige")]
    [SerializeField] private float updateInterval = 0.25f;

    private float timer;
    private float fps;
    private float frameTime;

    void Update()
    {
        timer += Time.unscaledDeltaTime;

        if (timer >= updateInterval)
        {
            float deltaTime = Time.unscaledDeltaTime;

            fps = 1f / deltaTime;
            frameTime = deltaTime * 1000f;

            timer = 0f;
        }
    }

    void OnGUI()
    {
        if (particleSimulation == null)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 18;
        style.normal.textColor = Color.white;

        string text =
            $"FPS        {fps:F0}\n" +
            $"Frame Time {frameTime:F1} ms\n" +
            $"Particles  {particleSimulation.ActiveParticles} / {particleSimulation.MaxParticles}";

        GUI.Label(
            new Rect(20, 20, 300, 100),
            text,
            style
        );
    }
}