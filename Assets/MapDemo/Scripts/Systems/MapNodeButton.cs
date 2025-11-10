using UnityEngine;
using UnityEngine.UI;
using MapDemo.Settings;
using TMPro;

public class MapNodeButton : MonoBehaviour
{
    public int nodeId;

    [Header("Assign these in the NodePrefab")]
    public Button button;        // Button child
    public Image baseImage;      // "Base" image under Button
    public Image ringImage;      // "Ring" image under Button
    public Image iconImage;      // "Icon" image under Button
    public TMP_Text labelText;

    private Color baseColor = Color.white;

    private void Awake()
    {
        // Minimal safety: if you forget button, try to get it
        if (button == null)
            button = GetComponentInChildren<Button>(true);

        if (baseImage != null)
            baseColor = baseImage.color;
    }

    public void ApplyStyle(MapSettings settings, MapNodeType type)
    {
        if (settings == null) return;

        // Get per-type style if exists
        Sprite typeIcon = null;
        Color typeColor = Color.white;

        if (settings.TryGetStyle(type, out var style))
        {
            if (style.icon != null)
                typeIcon = style.icon;
            if (style.color.a > 0f) // only use if not fully transparent
                typeColor = style.color;
        }

        // --- BASE ---
        if (baseImage != null)
        {
            if (settings.nodeBaseSprite != null)
                baseImage.sprite = settings.nodeBaseSprite;

            baseColor = typeColor;                // tint base by style color
            baseImage.color = baseColor;
            baseImage.enabled = true;
        }

        // --- RING ---
        if (ringImage != null)
        {
            if (settings.nodeRingSprite != null)
            {
                ringImage.sprite = settings.nodeRingSprite;
                ringImage.enabled = true;
                ringImage.color = Color.white;
            }
            else
            {
                ringImage.enabled = false;
            }
        }

        // --- ICON ---
        if (iconImage != null)
        {
            if (typeIcon != null)
            {
                iconImage.sprite = typeIcon;
                iconImage.enabled = true;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.enabled = false;
            }
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;

        if (baseImage == null) return;

        if (interactable)
        {
            baseImage.color = baseColor;
        }
        else
        {
            var c = baseColor;
            c.r *= 0.6f;
            c.g *= 0.6f;
            c.b *= 0.6f;
            baseImage.color = c;
        }
    }

    public void SetSelected()
    {
        if (baseImage == null) return;

        var c = baseColor;
        c.g = 1f; // cheap highlight tweak
        baseImage.color = c;
    }

    public void SetLabel(string text)
    {
        if (labelText == null) return;

        labelText.text = text;
    }

    public void SetLabelVisible(bool visible)
    {
        if (labelText == null) return;

        labelText.gameObject.SetActive(visible && !string.IsNullOrEmpty(labelText.text));
    }

}
