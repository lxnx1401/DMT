using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuLerp : MonoBehaviour
{
    [Header("UI Elemente")]
    [SerializeField] private RectTransform menuPanel; 

    [Header("Audio Mixer & UI")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Toggle musicMuteToggle;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Toggle soundMuteToggle;

    [Header("Exposed Mixer Parameter Namen")]
    [SerializeField] private string musicParam = "MusicVol";
    [SerializeField] private string soundParam = "SoundVol";

    [Header("Abhängigkeiten")]
    [SerializeField] private LevelCompletedLerp levelCompletedScript; 

    [Header("Animationseinstellungen")]
    [SerializeField] private float animationDuration = 0.4f; 

    private const string MUSIC_KEY = "MusicVolume";
    private const string SOUND_KEY = "SoundVolume";
    private const string MUSIC_MUTE_KEY = "MusicMute";
    private const string SOUND_MUTE_KEY = "SoundMute";

    private float preMuteMusicVolume = 0.75f;
    private float preMuteSoundVolume = 0.75f;

    private float screenRightPos; 
    private float customOpenedPos; 

    private bool isPaused = false;
    private Coroutine activeAnimation;

    void Awake()
    {
        if (menuPanel != null)
        {
            customOpenedPos = menuPanel.anchoredPosition.x;
            screenRightPos = customOpenedPos + Screen.width + menuPanel.rect.width;
            
            Vector2 startPos = menuPanel.anchoredPosition;
            startPos.x = screenRightPos;
            menuPanel.anchoredPosition = startPos;
        }

        if (levelCompletedScript == null)
        {
            levelCompletedScript = FindAnyObjectByType<LevelCompletedLerp>();
        }
    }

    private IEnumerator Start()
    {
        // 1. Listeners programmgesteuert zuweisen
        SetupUIListeners();

        // 2. Wichtig für neue Level: 1 Frame warten, bis der AudioMixer der neuen Szene bereit ist
        yield return null;

        // 3. Exakte Audio-Settings aus den PlayerPrefs laden
        LoadAudioSettings();
    }

    void OnEnable()
    {
        Time.timeScale = 1f;
        isPaused = false;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && !isPaused)
        {
            if (levelCompletedScript != null && levelCompletedScript.IsLevelCompleted)
            {
                return; 
            }

            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;

        if (activeAnimation != null) StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(AnimateMenu(customOpenedPos));

        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;

        if (activeAnimation != null) StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(AnimateMenu(screenRightPos));

        Time.timeScale = 1f;
    }

    public void OnContinuePressed()
    {
        if (isPaused)
        {
            ResumeGame();
        }
    }

    public void OnBackToMenuPressed(string menuSceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    // --- SETUP UI LISTENERS ---

    private void SetupUIListeners()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (musicMuteToggle != null)
        {
            musicMuteToggle.onValueChanged.RemoveAllListeners();
            musicMuteToggle.onValueChanged.AddListener(OnMusicMuteToggled);
        }

        if (soundSlider != null)
        {
            soundSlider.onValueChanged.RemoveAllListeners();
            soundSlider.onValueChanged.AddListener(SetSoundVolume);
        }

        if (soundMuteToggle != null)
        {
            soundMuteToggle.onValueChanged.RemoveAllListeners();
            soundMuteToggle.onValueChanged.AddListener(OnSoundSoundToggled);
        }
    }

    // --- AUDIO LOGIK ---

    private void LoadAudioSettings()
    {
        float savedMusic = PlayerPrefs.GetFloat(MUSIC_KEY, 0.75f);
        float savedSound = PlayerPrefs.GetFloat(SOUND_KEY, 0.75f);
        
        bool isMusicMuted = PlayerPrefs.GetInt(MUSIC_MUTE_KEY, 0) == 1;
        bool isSoundMuted = PlayerPrefs.GetInt(SOUND_MUTE_KEY, 0) == 1;

        preMuteMusicVolume = savedMusic > 0.0001f ? savedMusic : 0.75f;
        preMuteSoundVolume = savedSound > 0.0001f ? savedSound : 0.75f;

        if (musicSlider != null) musicSlider.SetValueWithoutNotify(savedMusic);
        if (soundSlider != null) soundSlider.SetValueWithoutNotify(savedSound);
        
        if (musicMuteToggle != null) musicMuteToggle.SetIsOnWithoutNotify(isMusicMuted);
        if (soundMuteToggle != null) soundMuteToggle.SetIsOnWithoutNotify(isSoundMuted);

        UpdateMusicState(isMusicMuted);
        UpdateSoundState(isSoundMuted);
    }

    public void SetMusicVolume(float value)
    {
        if (musicMuteToggle == null || !musicMuteToggle.isOn)
        {
            float safeValue = Mathf.Max(value, 0.0001f);
            float db = (Mathf.Log10(safeValue) * 20f) + 10f;
            
            if (mainMixer != null) mainMixer.SetFloat(musicParam, db);
            PlayerPrefs.SetFloat(MUSIC_KEY, safeValue);
            PlayerPrefs.Save();
        }
    }

    public void OnMusicMuteToggled(bool isMuted)
    {
        PlayerPrefs.SetInt(MUSIC_MUTE_KEY, isMuted ? 1 : 0);
        PlayerPrefs.Save();
        UpdateMusicState(isMuted);
    }

    private void UpdateMusicState(bool isMuted)
    {
        if (isMuted)
        {
            if (musicSlider != null)
            {
                if (musicSlider.value > 0.0001f) preMuteMusicVolume = musicSlider.value;
                musicSlider.SetValueWithoutNotify(0.0001f);
                musicSlider.interactable = false;
            }
            if (mainMixer != null) mainMixer.SetFloat(musicParam, -80f);
            
            if (musicMuteToggle != null && musicMuteToggle.targetGraphic != null)
                musicMuteToggle.targetGraphic.color = new Color(0.4f, 0.4f, 0.4f, 1f); 
        }
        else
        {
            float restoredVol = PlayerPrefs.GetFloat(MUSIC_KEY, preMuteMusicVolume);
            if (musicSlider != null)
            {
                musicSlider.interactable = true;
                musicSlider.SetValueWithoutNotify(restoredVol);
            }
            SetMusicVolume(restoredVol);
            
            if (musicMuteToggle != null && musicMuteToggle.targetGraphic != null)
                musicMuteToggle.targetGraphic.color = Color.white;
        }
    }

    public void SetSoundVolume(float value)
    {
        if (soundMuteToggle == null || !soundMuteToggle.isOn)
        {
            float safeValue = Mathf.Max(value, 0.0001f);
            float db = (Mathf.Log10(safeValue) * 20f) + 10f;
            
            if (mainMixer != null) mainMixer.SetFloat(soundParam, db);
            PlayerPrefs.SetFloat(SOUND_KEY, safeValue);
            PlayerPrefs.Save();
        }
    }

    public void OnSoundSoundToggled(bool isMuted)
    {
        PlayerPrefs.SetInt(SOUND_MUTE_KEY, isMuted ? 1 : 0);
        PlayerPrefs.Save();
        UpdateSoundState(isMuted);
    }

    private void UpdateSoundState(bool isMuted)
    {
        if (isMuted)
        {
            if (soundSlider != null)
            {
                if (soundSlider.value > 0.0001f) preMuteSoundVolume = soundSlider.value;
                soundSlider.SetValueWithoutNotify(0.0001f);
                soundSlider.interactable = false;
            }
            if (mainMixer != null) mainMixer.SetFloat(soundParam, -80f);
            
            if (soundMuteToggle != null && soundMuteToggle.targetGraphic != null)
                soundMuteToggle.targetGraphic.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        }
        else
        {
            float restoredVol = PlayerPrefs.GetFloat(SOUND_KEY, preMuteSoundVolume);
            if (soundSlider != null)
            {
                soundSlider.interactable = true;
                soundSlider.SetValueWithoutNotify(restoredVol);
            }
            SetSoundVolume(restoredVol);
            
            if (soundMuteToggle != null && soundMuteToggle.targetGraphic != null)
                soundMuteToggle.targetGraphic.color = Color.white;
        }
    }

    private IEnumerator AnimateMenu(float targetXValue)
    {
        float elapsedTime = 0f;
        Vector2 startPosition = menuPanel.anchoredPosition;
        Vector2 targetPosition = new Vector2(targetXValue, startPosition.y);

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float percentageComplete = elapsedTime / animationDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, percentageComplete);

            menuPanel.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, smoothT);
            yield return null;
        }

        menuPanel.anchoredPosition = targetPosition;
    }
}