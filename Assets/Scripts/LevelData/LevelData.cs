using UnityEngine;

[CreateAssetMenu(fileName = "New Level Data", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Content")]
    public int crownAmount;
    public int obstacleAmount;
    public int laserAmount;
    public int blackHoleAmount;
    public int rangedEnemyAmount;

    [Header("Play Area")]
    [Min(5)]
    public int worldSize;

    public Bounds PlayAreaBounds
    {
        get
        {
            float halfExtent = Mathf.Max(5, worldSize);
            return new Bounds(
                Vector3.zero,
                new Vector3(halfExtent * 2f, halfExtent * 2f, 0f));
        }
    }

    private void OnValidate()
    {
        crownAmount = Mathf.Max(0, crownAmount);
        obstacleAmount = Mathf.Max(0, obstacleAmount);
        laserAmount = Mathf.Max(0, laserAmount);
        blackHoleAmount = Mathf.Max(0, blackHoleAmount);
        rangedEnemyAmount = Mathf.Max(0, rangedEnemyAmount);
        worldSize = Mathf.Max(5, worldSize);
    }
}
