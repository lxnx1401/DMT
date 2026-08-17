using UnityEngine;

public class UIPulse : MonoBehaviour
{
    [SerializeField] private float scaleAmount = 1.1f;
    [SerializeField] private float speed = 2f;

    private Vector3 originalScale;

    private void Start()
    {
        originalScale = transform.localScale;
    }

    private void Update()
    {
        float scale = 1f + (Mathf.Sin(Time.unscaledTime * speed) + 1f) / 2f * (scaleAmount - 1f);

        transform.localScale = originalScale * scale;
    }
}