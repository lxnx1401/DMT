using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerMotionController
{
    private readonly float posSmoothing;
    private readonly float velSmoothing;
    private readonly float basePlayerSpeed;
    private readonly float boundaryMargin;

    private Vector2 smoothedMouseWorld;
    private Vector2 lastMouseScreen;
    private bool hasLastMouse;

    public Vector2 PlayerPosition { get; private set; }
    public Vector2 SmoothedMouseVelocity { get; private set; }
    public Vector2 LastMouseWorld { get; private set; }

    public PlayerMotionController(float posSmoothing, float velSmoothing, float basePlayerSpeed,
        float boundaryMargin, Vector2 startPosition)
    {
        this.posSmoothing = posSmoothing;
        this.velSmoothing = velSmoothing;
        this.basePlayerSpeed = basePlayerSpeed;
        this.boundaryMargin = boundaryMargin;

        PlayerPosition = startPosition;
        smoothedMouseWorld = startPosition;
        LastMouseWorld = startPosition;
    }

    public void ResetMouse()
    {
        hasLastMouse = false;
        SmoothedMouseVelocity = Vector2.zero;
    }

   
    public float Tick(float dt, float speedMultiplier)
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
        mouseWorld = ClampToPlayArea(mouseWorld);
        LastMouseWorld = mouseWorld;

        float posSmoothFactor = 1f - Mathf.Exp(-posSmoothing * dt);
        smoothedMouseWorld = Vector2.Lerp(smoothedMouseWorld, mouseWorld, posSmoothFactor);

        Vector2 previousPosition = PlayerPosition;
        PlayerPosition = Vector2.MoveTowards(PlayerPosition, smoothedMouseWorld, basePlayerSpeed * speedMultiplier * dt);
        PlayerPosition = ClampToPlayArea(PlayerPosition);

        Vector2 rawScreenVelocity = Vector2.zero;
        if (hasLastMouse && dt > 0.0001f)
            rawScreenVelocity = (mouseScreen - lastMouseScreen) / dt;

        lastMouseScreen = mouseScreen;
        hasLastMouse = true;

        float smoothFactor = 1f - Mathf.Exp(-velSmoothing * dt * 60f);
        SmoothedMouseVelocity = Vector2.Lerp(SmoothedMouseVelocity, rawScreenVelocity, smoothFactor);

        return dt > 0.0001f ? Vector2.Distance(previousPosition, PlayerPosition) / dt : 0f;
    }

    private Vector2 ClampToPlayArea(Vector2 point)
    {

        if (LevelManager.Instance != null && LevelManager.Instance.IsEndlessMode)
            return point;

        LevelData level = LevelManager.Instance?.CurrentLevel;
        if (level == null)
            return point;

        Bounds bounds = level.PlayAreaBounds;
        float marginX = Mathf.Min(boundaryMargin, bounds.size.x * 0.5f);
        float marginY = Mathf.Min(boundaryMargin, bounds.size.y * 0.5f);
        point.x = Mathf.Clamp(point.x, bounds.min.x + marginX, bounds.max.x - marginX);
        point.y = Mathf.Clamp(point.y, bounds.min.y + marginY, bounds.max.y - marginY);
        return point;
    }
}
