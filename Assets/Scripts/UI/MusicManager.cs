using UnityEngine;

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

        audioSource = GetComponentInChildren<AudioSource>();  
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
        if (Instance == this)  
        {  
            Instance = null;  
        }  
    }  
}