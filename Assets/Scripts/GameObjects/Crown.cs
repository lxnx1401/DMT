using UnityEngine;

public class Crown : MonoBehaviour
{
    private bool collected;
    [SerializeField] private AudioClip crownCollectSound;
    
    private AudioSource audioSource;

    private void Start()
    {
        GameObject manager = GameObject.FindWithTag("SoundManager");
        
        if (manager != null)
        {
            audioSource = manager.GetComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        if (other.CompareTag("Player"))
        {
            collected = true;

            foreach (Collider2D crownCollider in GetComponentsInChildren<Collider2D>(true))
                crownCollider.enabled = false;
            foreach (Renderer crownRenderer in GetComponentsInChildren<Renderer>(true))
                crownRenderer.enabled = false;

            if (crownCollectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(crownCollectSound);
            }

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