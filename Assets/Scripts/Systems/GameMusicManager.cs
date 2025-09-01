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
        StopAllCoroutines();
        StartCoroutine(FadeInMusic(clip, loop));
    }

    private System.Collections.IEnumerator FadeInMusic(AudioClip clip, bool loop)
    {
        // fade out
        float startVol = audioSource.volume;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.Play();

        // fade in
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, 1f, t / fadeTime);
            yield return null;
        }
    }
}
