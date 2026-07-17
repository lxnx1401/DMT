using UnityEngine;

public class Crown : MonoBehaviour
{
    private bool collected;
    [SerializeField] private AudioClip crownCollectSound;



    private void OnTriggerEnter2D(Collider2D other)
    {

        if (collected)
            return;


        if (other.CompareTag("Player"))
        {

            collected = true;

            if (crownCollectSound != null)
                AudioSource.PlayClipAtPoint(crownCollectSound, transform.position);

            int particleCount = ParticleSimulation.Instance != null
                ? ParticleSimulation.Instance.ActiveParticles
                : 0;
            PerformanceAnalyzer.Instance?.CompleteSection(particleCount);

            GameManager.Instance.CollectCrown();

            PerformanceAnalyzer.Instance?.BeginSection(particleCount);

            Destroy(gameObject);
        }
    }
}
