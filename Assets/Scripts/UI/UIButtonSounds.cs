using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;

public class UIButtonSounds : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerDownHandler
{
    [Header("Audio Quellen und Clips")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    [Header("Audio Mixer Group (Optional)")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    private void Awake()
    {
        SetupAudioSource();
    }

    private void OnEnable()
    {
        SetupAudioSource();
    }

    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Falls keine AudioSource existiert, lokal eine anlegen
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // WICHTIG: Spielt Sounds auch ab, wenn Time.timeScale = 0 (Pause) ist!
        audioSource.ignoreListenerPause = true;

        // Falls eine Mixer Group zugewiesen wurde, verbinden
        if (sfxMixerGroup != null && audioSource.outputAudioMixerGroup == null)
        {
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlaySound(hoverSound);
    }

    // OnPointerDown reagiert sofort beim Drücken (zuverlässiger bei TimeScale = 0)
    public void OnPointerDown(PointerEventData eventData)
    {
        PlaySound(clickSound);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Abgefangen durch OnPointerDown
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource == null)
        {
            SetupAudioSource();
        }

        if (audioSource != null && audioSource.enabled)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}