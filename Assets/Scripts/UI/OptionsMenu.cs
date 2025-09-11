using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Toggle masterMuteToggle;

    [SerializeField] private Slider musicSlider;
    [SerializeField] private Toggle musicMuteToggle;

    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle sfxMuteToggle;

    private float lastMaster = 1f;
    private float lastMusic = 1f;
    private float lastSfx = 1f;

    void OnEnable()
    {
        LoadSettings();
    }

    void Start()
    {
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        masterMuteToggle.onValueChanged.AddListener(ToggleMasterMute);

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        musicMuteToggle.onValueChanged.AddListener(ToggleMusicMute);

        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        sfxMuteToggle.onValueChanged.AddListener(ToggleSFXMute);
    }

    private void SetMasterVolume(float value)
    {
        lastMaster = value;
        if (!masterMuteToggle.isOn)
            ApplyChannel("Master", value, false);
        PlayerPrefs.SetFloat("MasterVol", value);
        PlayerPrefs.Save();
    }

    private void ToggleMasterMute(bool mute)
    {
        ApplyChannel("Master", lastMaster, mute);
        PlayerPrefs.SetInt("MasterMute", mute ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void SetMusicVolume(float value)
    {
        lastMusic = value;
        if (!musicMuteToggle.isOn)
            ApplyChannel("Music", value, false);
        PlayerPrefs.SetFloat("MusicVol", value);
        PlayerPrefs.Save();
    }

    private void ToggleMusicMute(bool mute)
    {
        ApplyChannel("Music", lastMusic, mute);
        PlayerPrefs.SetInt("MusicMute", mute ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void SetSFXVolume(float value)
    {
        lastSfx = value;
        if (!sfxMuteToggle.isOn)
            ApplyChannel("SFX", value, false);
        PlayerPrefs.SetFloat("SFXVol", value);
        PlayerPrefs.Save();
    }

    private void ToggleSFXMute(bool mute)
    {
        ApplyChannel("SFX", lastSfx, mute);
        PlayerPrefs.SetInt("SFXMute", mute ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyChannel(string parameter, float value, bool mute)
    {
        if (mute)
            audioMixer.SetFloat(parameter, -80f);
        else
            audioMixer.SetFloat(parameter, Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
    }

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
    }
}
