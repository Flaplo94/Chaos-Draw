using UnityEngine;
using UnityEngine.Audio;

public class AudioSettingsManager : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    void Start()
    {
        float musicNorm = PlayerPrefs.GetFloat("MusicVolNorm", 1f);
        float sfxNorm = PlayerPrefs.GetFloat("SFXVolNorm", 1f);

        float musicDb = (musicNorm > 0.0001f) ? Mathf.Log10(musicNorm) * 20f : -80f;
        float sfxDb = (sfxNorm > 0.0001f) ? Mathf.Log10(sfxNorm) * 20f : -80f;

        audioMixer.SetFloat("Music", musicDb);
        audioMixer.SetFloat("SFX", sfxDb);

        Debug.Log($"[AudioSettingsManager] Loaded Music={musicNorm} SFX={sfxNorm}");
        
    }
}
