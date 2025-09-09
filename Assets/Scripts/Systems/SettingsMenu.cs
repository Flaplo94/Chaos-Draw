using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Toggle masterMuteToggle;

    [SerializeField] private Slider musicSlider;
    [SerializeField] private Toggle musicMuteToggle;

    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle sfxMuteToggle;

    [Header("Display")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Controls")]
    [SerializeField] private Slider mouseSensitivitySlider;

    private Resolution[] resolutions;

    private float lastMaster = 1f;
    private float lastMusic = 1f;
    private float lastSfx = 1f;

    void Start()
    {
        // Hook up listeners
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        masterMuteToggle.onValueChanged.AddListener(ToggleMasterMute);

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        musicMuteToggle.onValueChanged.AddListener(ToggleMusicMute);

        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        sfxMuteToggle.onValueChanged.AddListener(ToggleSFXMute);

        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<string>();
        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);

        mouseSensitivitySlider.onValueChanged.AddListener(v => { PlayerPrefs.SetFloat("MouseSensitivity", v); SaveSettings(); });

        // Load saved / current settings
        LoadSettings();
    }

    // --- Audio ---
    private void SetMasterVolume(float value)
    {
        lastMaster = value;
        if (!masterMuteToggle.isOn)
            audioMixer.SetFloat("Master", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);

        SaveSettings();
    }

    private void ToggleMasterMute(bool mute)
    {
        if (mute)
            audioMixer.SetFloat("Master", -80f);
        else
            SetMasterVolume(lastMaster);

        SaveSettings();
    }

    private void SetMusicVolume(float value)
    {
        lastMusic = value;
        if (!musicMuteToggle.isOn)
            audioMixer.SetFloat("Music", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);

        SaveSettings();
    }

    private void ToggleMusicMute(bool mute)
    {
        if (mute)
            audioMixer.SetFloat("Music", -80f);
        else
            SetMusicVolume(lastMusic);

        SaveSettings();
    }

    private void SetSFXVolume(float value)
    {
        lastSfx = value;
        if (!sfxMuteToggle.isOn)
            audioMixer.SetFloat("SFX", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);

        SaveSettings();
    }

    private void ToggleSFXMute(bool mute)
    {
        if (mute)
            audioMixer.SetFloat("SFX", -80f);
        else
            SetSFXVolume(lastSfx);

        SaveSettings();
    }

    // --- Display ---
    private void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);

        PlayerPrefs.SetInt("ResolutionIndex", index);
        SaveSettings();
    }

    private void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;

        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        SaveSettings();
    }

    // --- Save all settings ---
    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVol", lastMaster);
        PlayerPrefs.SetInt("MasterMute", masterMuteToggle.isOn ? 1 : 0);

        PlayerPrefs.SetFloat("MusicVol", lastMusic);
        PlayerPrefs.SetInt("MusicMute", musicMuteToggle.isOn ? 1 : 0);

        PlayerPrefs.SetFloat("SFXVol", lastSfx);
        PlayerPrefs.SetInt("SFXMute", sfxMuteToggle.isOn ? 1 : 0);

        PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivitySlider.value);

        PlayerPrefs.Save();
    }

    // --- Load saved / current values ---
    private void LoadSettings()
    {
        float value;

        // Master
        if (PlayerPrefs.HasKey("MasterVol"))
            lastMaster = PlayerPrefs.GetFloat("MasterVol", 1f);
        if (audioMixer.GetFloat("Master", out value))
        {
            bool muted = PlayerPrefs.GetInt("MasterMute", 0) == 1 || value <= -79f;
            masterMuteToggle.isOn = muted;
            if (!muted)
            {
                masterSlider.value = lastMaster;
                SetMasterVolume(lastMaster);
            }
        }

        // Music
        if (PlayerPrefs.HasKey("MusicVol"))
            lastMusic = PlayerPrefs.GetFloat("MusicVol", 1f);
        if (audioMixer.GetFloat("Music", out value))
        {
            bool muted = PlayerPrefs.GetInt("MusicMute", 0) == 1 || value <= -79f;
            musicMuteToggle.isOn = muted;
            if (!muted)
            {
                musicSlider.value = lastMusic;
                SetMusicVolume(lastMusic);
            }
        }

        // SFX
        if (PlayerPrefs.HasKey("SFXVol"))
            lastSfx = PlayerPrefs.GetFloat("SFXVol", 1f);
        if (audioMixer.GetFloat("SFX", out value))
        {
            bool muted = PlayerPrefs.GetInt("SFXMute", 0) == 1 || value <= -79f;
            sfxMuteToggle.isOn = muted;
            if (!muted)
            {
                sfxSlider.value = lastSfx;
                SetSFXVolume(lastSfx);
            }
        }

        // Fullscreen
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        fullscreenToggle.isOn = fullscreen;
        Screen.fullScreen = fullscreen;

        // Resolution
        int resIndex = PlayerPrefs.GetInt("ResolutionIndex", resolutionDropdown.value);
        if (resIndex >= 0 && resIndex < resolutions.Length)
        {
            resolutionDropdown.value = resIndex;
            resolutionDropdown.RefreshShownValue();
            Resolution res = resolutions[resIndex];
            Screen.SetResolution(res.width, res.height, fullscreen);
        }

        // Mouse Sensitivity
        mouseSensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
    }
}
