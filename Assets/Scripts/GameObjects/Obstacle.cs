using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public float radius = 10f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ParticleSimulation.Instance.TakeDamage();
        }
    }
}