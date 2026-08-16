using UnityEngine;

public class Crown : MonoBehaviour
{
    [System.Serializable]
    public struct CrownVariation
    {
        public Sprite sprite;
    }

    private bool collected;
    [SerializeField] private AudioClip crownCollectSound;

    [Header("Sprite Variations")]
    [SerializeField] private CrownVariation[] crownVariations;

    [Header("Color Effects")]
    [SerializeField] private float hueShiftSpeed = 0.5f;   
    [Range(0f, 1f)]
    [SerializeField] private float saturation = 1f;

    [Header("Scale Pulsing")]
    [SerializeField] private float scaleSpeed = 6f;          
    [SerializeField] private float minScaleMultiplier = 0.8f; 
    [SerializeField] private float maxScaleMultiplier = 1.2f; 

    private AudioSource audioSource;
    private SpriteRenderer[] spriteRenderers;

    private float hueOffset;
    private float pulseOffset;
    private Vector3 baseScale;

    private void Start()
    {
        GameObject manager = GameObject.FindWithTag("SoundManager");
        
        if (manager != null)
        {
            audioSource = manager.GetComponent<AudioSource>();
        }

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        baseScale = transform.localScale;

        if (crownVariations != null && crownVariations.Length > 0)
        {
            CrownVariation selectedVariation = crownVariations[Random.Range(0, crownVariations.Length)];
            
            foreach (SpriteRenderer sr in spriteRenderers)
            {
                if (sr != null)
                {
                    sr.sprite = selectedVariation.sprite;
                }
            }
        }

        hueOffset = Random.Range(0f, 1f);
        pulseOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (collected)
            return;

        float pulseFactor = (Mathf.Sin((Time.time + pulseOffset) * scaleSpeed) + 1f) * 0.5f;
        float currentMultiplier = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, pulseFactor);
        transform.localScale = baseScale * currentMultiplier;

        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            float currentHue = (Time.time * hueShiftSpeed + hueOffset) % 1f;
            Color newColor = Color.HSVToRGB(currentHue, saturation, 1f);

            foreach (SpriteRenderer sr in spriteRenderers)
            {
                if (sr != null)
                {
                    newColor.a = sr.color.a;
                    sr.color = newColor;
                }
            }
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