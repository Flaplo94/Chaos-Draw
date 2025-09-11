using UnityEngine;
using UnityEngine.Audio;

public class AudioInitializer : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    void Awake()
    {
        LoadSettings();
    }

    void Start()
    {
        LoadSettings(); // double-check in case AudioSources weren’t ready
    }

    private void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat("MasterVol", 1f);
        bool masterMute = PlayerPrefs.GetInt("MasterMute", 0) == 1;
        ApplyChannel("Master", master, masterMute);

        float music = PlayerPrefs.GetFloat("MusicVol", 1f);
        bool musicMute = PlayerPrefs.GetInt("MusicMute", 0) == 1;
        ApplyChannel("Music", music, musicMute);

        float sfx = PlayerPrefs.GetFloat("SFXVol", 1f);
        bool sfxMute = PlayerPrefs.GetInt("SFXMute", 0) == 1;
        ApplyChannel("SFX", sfx, sfxMute);
    }

    private void ApplyChannel(string parameter, float value, bool mute)
    {
        if (mute)
            audioMixer.SetFloat(parameter, -80f);
        else
            audioMixer.SetFloat(parameter, Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
    }
}
