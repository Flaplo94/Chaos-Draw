using UnityEngine;
using TMPro;

public class DamageNumberEffect : MonoBehaviour
{
    public float rise = 40f;      // hvor langt tallet flyver op (UI pixels)
    public float duration = 0.8f; // hvor hurtigt det forsvinder

    TMP_Text txt;
    RectTransform rect;
    Vector2 startPos;
    Color startColor;
    float t;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        txt = GetComponent<TMP_Text>();
        startColor = txt.color;
    }

    void OnEnable()
    {
        startPos = rect.anchoredPosition;
        t = 0f;
        var c = txt.color; c.a = 1f; txt.color = c; // reset alpha når den spawnes
    }

    void Update()
    {
        t += Time.deltaTime;
        float u = Mathf.Clamp01(t / duration);

        rect.anchoredPosition = startPos + Vector2.up * (rise * u);

        var c = txt.color;
        c.a = 1f - u;
        txt.color = c;

        if (u >= 1f) Destroy(gameObject);
    }
}
