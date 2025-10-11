using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class WorldPopupText : MonoBehaviour
{
    [Header("Lifetime & Motion")]
    public float lifetime = 1.2f;
    public float riseSpeed = 1.0f;          // units/sec (world) or pixels/sec (UI)
    [Range(0f, 1f)] public float fadePortion = 0.6f;

    [Header("2D Sorting (for world TMP)")]
    public string sortingLayerName = "VFX";
    public int sortingOrder = 20;

    // Detected mode
    private bool isUI;                      // true if under a non-world-space Canvas with TMP UGUI
    private TextMeshPro tmp;
    private TextMeshProUGUI tmpUGUI;
    private Canvas parentCanvas;
    private RectTransform rect;             // for UI

    private float t;

    void Awake()
    {
        // Find components on the same GameObject
        tmp = GetComponent<TextMeshPro>();
        tmpUGUI = GetComponent<TextMeshProUGUI>();
        rect = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        // If neither exists, auto-add world TMP so we never crash
        if (tmp == null && tmpUGUI == null)
        {
            tmp = gameObject.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 3f;
        }

        // Determine mode
        if (tmpUGUI != null && parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
        {
            isUI = true; // Screen Space Overlay or Screen Space Camera
        }
        else
        {
            isUI = false; // world text (TMP) or World-Space Canvas
        }

        // Sorting for world text
        if (!isUI && tmp != null)
        {
            var mr = tmp.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = sortingLayerName;
                mr.sortingOrder = sortingOrder;
            }
            tmp.alignment = TextAlignmentOptions.Center;
        }

        if (isUI && tmpUGUI != null)
        {
            tmpUGUI.alignment = TextAlignmentOptions.Center;
        }
    }

    /// <summary>
    /// Positions the popup either in world or under a screen-space canvas.
    /// For UI mode, pass screenPosOverride = Camera.WorldToScreenPoint(worldPos).
    /// </summary>
    public void InitializeAtWorldPosition(Vector3 worldPos, Camera cam, Vector3? screenPosOverride = null)
    {
        if (isUI)
        {
            // Place using screen position under canvas
            Vector3 screen = screenPosOverride ?? (cam != null ? cam.WorldToScreenPoint(worldPos) : worldPos);
            if (rect != null) rect.position = screen;
            else transform.position = screen;
        }
        else
        {
            // World-space
            transform.position = worldPos;
        }
    }

    public void SetText(string text)
    {
        if (tmpUGUI != null)
        {
            tmpUGUI.text = text;
            return;
        }
        if (tmp != null)
        {
            tmp.text = text;
            return;
        }

        // Last-ditch safety (shouldn't happen)
        tmp = gameObject.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = text;
    }

    void Update()
    {
        t += Time.deltaTime;

        // Float up
        if (isUI && rect != null)
        {
            // UI: move in screen space (pixels)
            rect.anchoredPosition += Vector2.up * (riseSpeed * Time.deltaTime);
        }
        else
        {
            // World: move in world units
            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
        }

        // Fade-out
        if (fadePortion > 0f && lifetime > 0f)
        {
            float fadeStart = lifetime * (1f - fadePortion);
            if (t > fadeStart)
            {
                float a = Mathf.InverseLerp(lifetime, fadeStart, t); 
                if (tmpUGUI != null)
                {
                    var c = tmpUGUI.color; c.a = a; tmpUGUI.color = c;
                }
                else if (tmp != null)
                {
                    var c = tmp.color; c.a = a; tmp.color = c;
                }
            }
        }

        if (t >= lifetime)
            Destroy(gameObject);
    }
}
