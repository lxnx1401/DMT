using UnityEngine;

public class CameraController : MonoBehaviour
{
    public ParticleSimulation player;

    public float followSpeed = 5f;

    [Header("Offset")]
    public Vector2 offset = Vector2.zero;


    void LateUpdate()
    {
        if (player == null)
            return;


        Vector3 target = new Vector3(
            player.HeadPosition.x + offset.x,
            player.HeadPosition.y + offset.y,
            transform.position.z
        );


        transform.position = Vector3.Lerp(
            transform.position,
            target,
            followSpeed * Time.deltaTime
        );
    }
}