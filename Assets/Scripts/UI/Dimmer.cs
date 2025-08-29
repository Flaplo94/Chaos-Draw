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
        EnsureSetup();

        // Sikre at basefarven er 100% sort (styr alpha via CanvasGroup)
        var img = GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 1f);

        if (startHidden)
        {
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }
    }

    void EnsureSetup()
    {
        if (cg == null)
            cg = GetComponent<CanvasGroup>();
    }

    public void Show()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true); // auto-tænd GO

        EnsureSetup();
        Debug.Log("[Dimmer] Show()");
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(FadeTo(targetAlpha, true));
    }

    public void Hide()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true); // auto-tænd GO

        EnsureSetup();
        Debug.Log("[Dimmer] Hide()");
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(FadeTo(0f, false));
    }

    public void InstantOn()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        EnsureSetup();
        Debug.Log("[Dimmer] InstantOn()");
        cg.alpha = targetAlpha;
        cg.blocksRaycasts = blockClicks;
        cg.interactable = false;
    }

    public void InstantOff()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        EnsureSetup();
        Debug.Log("[Dimmer] InstantOff()");
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }

    IEnumerator FadeTo(float a, bool enableBlock)
    {
        EnsureSetup();
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
            //  ikke mere SetActive(false)
        }
    }
}
