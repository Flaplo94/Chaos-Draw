using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class TopBarAutoSize : MonoBehaviour
{
    [Range(0.03f, 0.12f)] public float heightPercent = 0.07f; // 7% af skærmhøjde
    public int minPixels = 56;
    public int maxPixels = 88;

    void LateUpdate()
    {
        var rt = transform as RectTransform;
        if (!rt) return;

        float h = Mathf.RoundToInt(Mathf.Clamp(Screen.height * heightPercent, minPixels, maxPixels));
        var size = rt.sizeDelta;
        size.y = h; // kun højden – bredde styres af anchors
        rt.sizeDelta = size;

        // hold Y-position hel
        var pos = rt.anchoredPosition;
        pos.y = Mathf.Round(pos.y);
        rt.anchoredPosition = pos;
    }
}
