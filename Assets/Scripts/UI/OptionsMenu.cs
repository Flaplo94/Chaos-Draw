using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        // Load saved values, default to full volume
        float music = PlayerPrefs.GetFloat("MusicVolNorm", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolNorm", 1f);

        // Assign to sliders
        musicSlider.value = music;
        sfxSlider.value = sfx;

        // Apply immediately so mixer matches slider from the start
        ApplyMusicVolume(music);
        ApplySFXVolume(sfx);

        // Add listeners
        musicSlider.onValueChanged.AddListener(ApplyMusicVolume);
        sfxSlider.onValueChanged.AddListener(ApplySFXVolume);
    }

    private void ApplyMusicVolume(float value)
    {
        // Clamp between 0 and 1
        value = Mathf.Clamp01(value);

        float dB = (value <= 0f) ? -80f : Mathf.Log10(value) * 20f;

        audioMixer.SetFloat("MusicVol", dB);
        PlayerPrefs.SetFloat("MusicVolNorm", value);
        Debug.Log($"[OptionsMenu] Music slider={value} ? dB={dB}");
    }

    private void ApplySFXVolume(float value)
    {
        value = Mathf.Clamp01(value);

        float dB = (value <= 0f) ? -80f : Mathf.Log10(value) * 20f;

        audioMixer.SetFloat("SFXVol", dB);
        PlayerPrefs.SetFloat("SFXVolNorm", value);
        Debug.Log($"[OptionsMenu] SFX slider={value} ? dB={dB}");
    }
}
