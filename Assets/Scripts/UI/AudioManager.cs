using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioMixer mainMixer;
    
    [Header("Music Settings")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Toggle musicMuteToggle;

    [Header("Sound Settings")]
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Toggle soundMuteToggle;

    private const string MUSIC_KEY = "MusicVolume";
    private const string SOUND_KEY = "SoundVolume";
    private const string MUSIC_MUTE_KEY = "MusicMute";
    private const string SOUND_MUTE_KEY = "SoundMute";

    private float preMuteMusicVolume = 0.75f;
    private float preMuteSoundVolume = 0.75f;

    void Start()
    {
        float savedMusic = PlayerPrefs.GetFloat(MUSIC_KEY, 0.75f);
        float savedSound = PlayerPrefs.GetFloat(SOUND_KEY, 0.75f);
        
        bool isMusicMuted = PlayerPrefs.GetInt(MUSIC_MUTE_KEY, 0) == 1;
        bool isSoundMuted = PlayerPrefs.GetInt(SOUND_MUTE_KEY, 0) == 1;

        // Falls gemutet war, sichern wir den echten Lautstärkewert für später
        preMuteMusicVolume = savedMusic > 0.0001f ? savedMusic : 0.75f;
        preMuteSoundVolume = savedSound > 0.0001f ? savedSound : 0.75f;

        if (musicSlider != null) musicSlider.value = savedMusic;
        if (soundSlider != null) soundSlider.value = savedSound;
        
        if (musicMuteToggle != null) musicMuteToggle.isOn = isMusicMuted;
        if (soundMuteToggle != null) soundMuteToggle.isOn = isSoundMuted;

        // Erzwingt die korrekten Mixer-Einstellungen direkt beim Szenenstart
        UpdateMusicState(isMusicMuted);
        UpdateSoundState(isSoundMuted);
    }

    public void SetMusicVolume(float value)
    {
        if (musicMuteToggle == null || !musicMuteToggle.isOn)
        {
            float db = (Mathf.Log10(value) * 20) + 10f;
            mainMixer.SetFloat("MusicVol", db);
            PlayerPrefs.SetFloat(MUSIC_KEY, value);
        }
    }

    public void OnMusicMuteToggled(bool isMuted)
    {
        PlayerPrefs.SetInt(MUSIC_MUTE_KEY, isMuted ? 1 : 0);
        UpdateMusicState(isMuted);
    }

    private void UpdateMusicState(bool isMuted)
    {
        if (isMuted)
        {
            if (musicSlider != null)
            {
                preMuteMusicVolume = musicSlider.value;
                musicSlider.value = 0.0001f;
                musicSlider.interactable = false;
            }
            mainMixer.SetFloat("MusicVol", -80f);
            
            if (musicMuteToggle != null && musicMuteToggle.targetGraphic != null)
                musicMuteToggle.targetGraphic.color = new Color(0.4f, 0.4f, 0.4f, 1f); 
        }
        else
        {
            if (musicSlider != null)
            {
                musicSlider.interactable = true;
                musicSlider.value = PlayerPrefs.GetFloat(MUSIC_KEY, preMuteMusicVolume);
            }
            SetMusicVolume(musicSlider != null ? musicSlider.value : preMuteMusicVolume);
            
            if (musicMuteToggle != null && musicMuteToggle.targetGraphic != null)
                musicMuteToggle.targetGraphic.color = Color.white;
        }
    }

    public void SetSoundVolume(float value)
    {
        if (soundMuteToggle == null || !soundMuteToggle.isOn)
        {
            float db = (Mathf.Log10(value) * 20) + 10f;
            mainMixer.SetFloat("SoundVol", db);
            PlayerPrefs.SetFloat(SOUND_KEY, value);
        }
    }

    public void OnSoundSoundToggled(bool isMuted)
    {
        PlayerPrefs.SetInt(SOUND_MUTE_KEY, isMuted ? 1 : 0);
        UpdateSoundState(isMuted);
    }

    private void UpdateSoundState(bool isMuted)
    {
        if (isMuted)
        {
            if (soundSlider != null)
            {
                preMuteSoundVolume = soundSlider.value;
                soundSlider.value = 0.0001f;
                soundSlider.interactable = false;
            }
            mainMixer.SetFloat("SoundVol", -80f);
            
            if (soundMuteToggle != null && soundMuteToggle.targetGraphic != null)
                soundMuteToggle.targetGraphic.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        }
        else
        {
            if (soundSlider != null)
            {
                soundSlider.interactable = true;
                soundSlider.value = PlayerPrefs.GetFloat(SOUND_KEY, preMuteSoundVolume);
            }
            SetSoundVolume(soundSlider != null ? soundSlider.value : preMuteSoundVolume);
            
            if (soundMuteToggle != null && soundMuteToggle.targetGraphic != null)
                soundMuteToggle.targetGraphic.color = Color.white;
        }
    }
}