using UnityEngine;

[ExecuteAlways]
public class UIScaleToScreen : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField, Range(0.05f, 0.4f)] private float heightPercent = 0.15f; // 15% of screen height

    void Update()
    {
        if (target == null) return;
        float targetHeight = Screen.height * heightPercent;
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
    }
}
