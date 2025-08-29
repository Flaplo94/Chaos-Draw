using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TMP_Text))]
public class FadeInText : MonoBehaviour
{
    public float fadeDuration = 1.0f;

    private TMP_Text tmpText;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        // Start med at være usynlig
        var c = tmpText.color;
        c.a = 0f;
        tmpText.color = c;
    }

    void OnEnable()
    {
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float timer = 0f;
        Color c = tmpText.color;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime; // unscaled så det også virker når Time.timeScale=0
            float alpha = Mathf.Clamp01(timer / fadeDuration);
            tmpText.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }
    }
}
