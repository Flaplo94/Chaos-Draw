// GameMusicManager.cs
using UnityEngine;

public class GameMusicManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip normalMusic;
    [SerializeField] private AudioClip boss1Music;
    [SerializeField] private AudioClip boss2Music;
    [SerializeField] private float fadeTime = 1f;

    private void Start()
    {
        PlayMusic(normalMusic);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        // If we're already playing this clip, don't restart it.
        if (audioSource != null && audioSource.clip == clip && audioSource.isPlaying)
        {
            audioSource.loop = loop; // still allow loop flag updates
            return;
        }

        StopAllCoroutines();
        StartCoroutine(FadeInMusic(clip, loop));
    }

    private System.Collections.IEnumerator FadeInMusic(AudioClip clip, bool loop)
    {
        // Only fade out if something is playing
        if (audioSource.isPlaying && audioSource.clip != null)
        {
            float startVol = audioSource.volume;
            float t = 0f;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
                yield return null;
            }
            audioSource.Stop();
        }

        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.Play();

        // Fade in to full volume
        float tIn = 0f;
        while (tIn < fadeTime)
        {
            tIn += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, 1f, tIn / fadeTime);
            yield return null;
        }
    }
}
