using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour  
{  
    public static MusicManager Instance { get; private set; }  

    private AudioSource audioSource;  

    private void Awake()  
    {  
        // Singleton-Muster: Verhindert, dass beim Szenenwechsel ein zweiter MusicManager entsteht
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
        // Startet die Musik, falls sie nicht schon läuft
        if (audioSource != null && !audioSource.isPlaying)  
        {  
            audioSource.Play();  
        }  
    }  

    private void OnDestroy()  
    {  
        // Wichtig: Event-Abmeldung, um Speicherlecks zu verhindern
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
        // Sucht nach allen AudioListenern in der neuen Szene
        AudioListener[] listeners = FindObjectsByType<AudioListener>(  
            FindObjectsInactive.Exclude,  
            FindObjectsSortMode.None);  

        if (listeners.Length <= 1)  
            return;  

        // Bevorzugt den AudioListener der Hauptkamera
        AudioListener preferred = Camera.main != null  
            ? Camera.main.GetComponent<AudioListener>()  
            : null;  
        preferred ??= listeners[0];  

        // Deaktiviert alle anderen Listener, damit Unity keine Fehler wirft
        foreach (AudioListener listener in listeners)  
            listener.enabled = listener == preferred;  
    }  
}