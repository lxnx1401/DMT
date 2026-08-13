using UnityEngine;

public class CameraController : MonoBehaviour
{
    public ParticleSimulation player;

    public float followSpeed = 30f;

    [Header("Offset")]
    public Vector2 offset = Vector2.zero;

    [Header("Sichtweite")]
    [SerializeField] private Camera targetCamera;
    [SerializeField, Min(1f)] private float minViewSize = 9f;
    [SerializeField, Min(1f)] private float maxViewSize = 17f;
    [SerializeField, Min(0.1f)] private float viewSizeLerpSpeed = 2f;
    [SerializeField, Range(0.1f, 1f)] private float maxLagFraction = 0.6f; // Anteil der halben Sichthoehe, den die Kamera maximal zurueckbleiben darf

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (player == null)
            return;

        Vector3 target = new Vector3(
            player.HeadPosition.x + offset.x,
            player.HeadPosition.y + offset.y,
            transform.position.z
        );

        float followFactor = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        Vector3 smoothed = Vector3.Lerp(transform.position, target, followFactor);

        // Hartes Nachzieh-Limit zusaetzlich zum Lerp: bei sehr hoher adaptiver Geschwindigkeit
        // (playerSpeedMultiplier kann bei hoher Intensitaet weit uebet 1 liegen) wuerde reines
        // Lerp irgendwann hinterherhinken und der Spieler koennte aus dem Bild laufen. Der Cap
        // richtet sich nach der AKTUELLEN Sichtgroesse (nicht der maximalen), damit die Grenze
        // auch waehrend des Zoom-Lerps von UpdateViewDistance() konsistent bleibt.
        float currentHalfView = targetCamera != null && targetCamera.orthographic
            ? targetCamera.orthographicSize
            : maxViewSize;
        float maxLag = currentHalfView * maxLagFraction;

        Vector3 lagVector = smoothed - target;
        if (lagVector.sqrMagnitude > maxLag * maxLag)
            smoothed = target + lagVector.normalized * maxLag;

        transform.position = smoothed;

        UpdateViewDistance();
    }

    private void UpdateViewDistance()
    {
        if (targetCamera == null || !targetCamera.orthographic)
            return;

        float difficulty = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.CurrentTuning.difficulty
            : 0.5f;

        float targetSize = Mathf.Lerp(minViewSize, maxViewSize, difficulty);
        targetCamera.orthographicSize = Mathf.Lerp(
            targetCamera.orthographicSize,
            targetSize,
            viewSizeLerpSpeed * Time.deltaTime);
    }
}