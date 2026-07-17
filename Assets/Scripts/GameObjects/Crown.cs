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

            // Destroy is deferred until the end of the frame. Hide collision and
            // visuals immediately so the final crown cannot remain on the win screen.
            foreach (Collider2D crownCollider in GetComponentsInChildren<Collider2D>(true))
                crownCollider.enabled = false;
            foreach (Renderer crownRenderer in GetComponentsInChildren<Renderer>(true))
                crownRenderer.enabled = false;

            if (crownCollectSound != null)
                AudioSource.PlayClipAtPoint(crownCollectSound, transform.position);

            int particleCount = ParticleSimulation.Instance != null
                ? ParticleSimulation.Instance.ActiveParticles
                : 0;
            PerformanceAnalyzer.Instance?.CompleteSection(particleCount);

            GameManager.Instance?.CollectCrown();

            if (!(GameManager.Instance?.IsGameOver ?? false))
                PerformanceAnalyzer.Instance?.BeginSection(particleCount);

            Destroy(gameObject);
        }
    }
}
