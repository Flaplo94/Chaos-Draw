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

        LoadSettings();
    }

    // --- Audio ---
    private void SetMasterVolume(float value)
    {
        lastMaster = value;
        if (!masterMuteToggle.isOn)
            ApplyChannel("Master", value, false);
        PlayerPrefs.SetFloat("MasterVol", value);
        SaveSettings();
    }

    private void ToggleMasterMute(bool mute)
    {
        ApplyChannel("Master", lastMaster, mute);
        PlayerPrefs.SetInt("MasterMute", mute ? 1 : 0);
        SaveSettings();
    }

    private void SetMusicVolume(float value)
    {
        lastMusic = value;
        if (!musicMuteToggle.isOn)
            ApplyChannel("Music", value, false);
        PlayerPrefs.SetFloat("MusicVol", value);
        SaveSettings();
    }

    private void ToggleMusicMute(bool mute)
    {
        ApplyChannel("Music", lastMusic, mute);
        PlayerPrefs.SetInt("MusicMute", mute ? 1 : 0);
        SaveSettings();
    }

    private void SetSFXVolume(float value)
    {
        lastSfx = value;
        if (!sfxMuteToggle.isOn)
            ApplyChannel("SFX", value, false);
        PlayerPrefs.SetFloat("SFXVol", value);
        SaveSettings();
    }

    private void ToggleSFXMute(bool mute)
    {
        ApplyChannel("SFX", lastSfx, mute);
        PlayerPrefs.SetInt("SFXMute", mute ? 1 : 0);
        SaveSettings();
    }

    private void ApplyChannel(string parameter, float value, bool mute)
    {
        if (mute)
            audioMixer.SetFloat(parameter, -80f);
        else
            audioMixer.SetFloat(parameter, Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
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

    // --- Save ---
    private void SaveSettings()
    {
        PlayerPrefs.Save();
    }

    // --- Load ---
    private void LoadSettings()
    {
        lastMaster = PlayerPrefs.GetFloat("MasterVol", 1f);
        masterSlider.value = lastMaster;
        masterMuteToggle.isOn = PlayerPrefs.GetInt("MasterMute", 0) == 1;
        ApplyChannel("Master", lastMaster, masterMuteToggle.isOn);

        lastMusic = PlayerPrefs.GetFloat("MusicVol", 1f);
        musicSlider.value = lastMusic;
        musicMuteToggle.isOn = PlayerPrefs.GetInt("MusicMute", 0) == 1;
        ApplyChannel("Music", lastMusic, musicMuteToggle.isOn);

        lastSfx = PlayerPrefs.GetFloat("SFXVol", 1f);
        sfxSlider.value = lastSfx;
        sfxMuteToggle.isOn = PlayerPrefs.GetInt("SFXMute", 0) == 1;
        ApplyChannel("SFX", lastSfx, sfxMuteToggle.isOn);

        fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

        int resIndex = PlayerPrefs.GetInt("ResolutionIndex", resolutionDropdown.value);
        if (resIndex >= 0 && resIndex < resolutions.Length)
        {
            resolutionDropdown.value = resIndex;
            resolutionDropdown.RefreshShownValue();
            Resolution res = resolutions[resIndex];
            Screen.SetResolution(res.width, res.height, fullscreenToggle.isOn);
        }

        mouseSensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
    }
}
