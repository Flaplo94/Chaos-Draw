using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ConstantHeightCamera : MonoBehaviour
{
    // Brug samme tal som Pixel Perfect Camera - Assets Pixels Per Unit
    public int pixelsPerUnit = 32;

    // Hvor høj “spillefladen” er i pixels, som vi vil fastholde
    public int referenceHeight = 1080;

    void OnValidate() => Apply();
    void Awake() => Apply();

    void Apply()
    {
        var cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = (referenceHeight * 0.5f) / Mathf.Max(1, pixelsPerUnit);
    }
}
