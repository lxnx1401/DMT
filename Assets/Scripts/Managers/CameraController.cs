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

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        Debug.Log("CAMERA CONTROLLER RUNNING");
        if (player == null)
            return;


        Vector3 target = new Vector3(
            player.HeadPosition.x + offset.x,
            player.HeadPosition.y + offset.y,
            transform.position.z
        );

        float followFactor = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);

        transform.position = target;
        Debug.Log($"CAMERA TARGET {target} / ACTUAL {transform.position}");

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