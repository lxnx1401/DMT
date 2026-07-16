using UnityEngine;

public class MovingObstacle : Obstacle
{
    [SerializeField] private Vector2 localStartPoint;
    [SerializeField] private Vector2 localEndPoint = Vector2.right * 5f;
    [SerializeField] private bool pingPong = true;

    private Vector2 origin;
    private float progress;
    private int movementDirection = 1;

    protected override void Start()
    {
        base.Start();
        origin = transform.position;
        transform.position = origin + localStartPoint;
    }

    private void Update()
    {
        Vector2 start = origin + localStartPoint;
        Vector2 end = origin + localEndPoint;
        float distance = Vector2.Distance(start, end);

        if (distance <= 0.0001f || currentSpeed <= 0f)
            return;

        progress += movementDirection * currentSpeed * Time.deltaTime / distance;

        if (pingPong)
        {
            if (progress >= 1f || progress <= 0f)
            {
                progress = Mathf.Clamp01(progress);
                movementDirection *= -1;
            }
        }
        else
        {
            progress = Mathf.Repeat(progress, 1f);
        }

        transform.position = Vector2.Lerp(start, end, progress);
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 drawOrigin = Application.isPlaying ? origin : (Vector2)transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(drawOrigin + localStartPoint, drawOrigin + localEndPoint);
        Gizmos.DrawWireSphere(drawOrigin + localStartPoint, 0.15f);
        Gizmos.DrawWireSphere(drawOrigin + localEndPoint, 0.15f);
    }
}
