using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureSingleAudioListener();
    }


    private void Start()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (Instance == this)
            Instance = null;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSingleAudioListener();
    }

    private static void EnsureSingleAudioListener()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        if (listeners.Length <= 1)
            return;

        AudioListener preferred = Camera.main != null
            ? Camera.main.GetComponent<AudioListener>()
            : null;
        preferred ??= listeners[0];

        foreach (AudioListener listener in listeners)
            listener.enabled = listener == preferred;
    }
}
