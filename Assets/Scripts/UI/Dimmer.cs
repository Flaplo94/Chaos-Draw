using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class Dimmer : MonoBehaviour
{
    [Range(0f, 1f)] public float targetAlpha = 0.7f;
    public float fadeDuration = 0.25f;
    public bool blockClicks = true;
    public bool startHidden = true;

    CanvasGroup cg;
    Coroutine current;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();

        // Sikre at basefarven er 100% sort (styr alpha via CanvasGroup)
        var img = GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 1f);

        if (startHidden)
        {
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
            gameObject.SetActive(false);
        }
    }

    public void Show()
    {
        Debug.Log("[Dimmer] Show()");
        gameObject.SetActive(true);
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(FadeTo(targetAlpha, true));
    }

    public void Hide()
    {
        Debug.Log("[Dimmer] Hide()");
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(FadeTo(0f, false));
    }

    public void InstantOn()
    {
        Debug.Log("[Dimmer] InstantOn()");
        gameObject.SetActive(true);
        cg.alpha = targetAlpha;
        cg.blocksRaycasts = blockClicks;
        cg.interactable = false;
    }

    public void InstantOff()
    {
        Debug.Log("[Dimmer] InstantOff()");
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
        gameObject.SetActive(false);
    }

    IEnumerator FadeTo(float a, bool enableBlock)
    {
        float start = cg.alpha;
        float t = 0f;

        if (enableBlock && blockClicks)
        {
            cg.blocksRaycasts = true;
            cg.interactable = false;
        }

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, a, t / fadeDuration);
            yield return null;
        }
        cg.alpha = a;

        if (a <= 0.001f)
        {
            cg.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
    }
}
