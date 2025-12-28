using UnityEngine;
using UnityEngine.UI;
using MapDemo.Settings;
using TMPro;

// Dette script er lavet af Marc
/// <summary>
/// MapNodeButton
/// Ansvar: Wrapper / helper for node UI prefab.
/// Håndterer styling (sprite, icon, farve), interaktivitet og label-visning for en enkelt node-knap.
/// Denne klasse antager at prefab indeholder Button, Image-komponenter og eventuelt en TextMeshPro tekst.
/// </summary>
public class MapNodeButton : MonoBehaviour
{
    /// Node id som matcher MapGraph nodens id. Sættes af MapManager ved opbygning.
    public int nodeId;

    [Header("Assign these in the NodePrefab")]
    /// Reference til knappekomponenten i prefab (UI Button).
    public Button button;
    /// Image der repræsenterer node-basen (baggrund sprite).
    public Image baseImage;
    /// Image til ring/outline omkring noden (kan farves via MapSettings).
    public Image ringImage;
    /// Image der viser node-ikon (valgfrit, fra MapSettings NodeStyle.icon).
    public Image iconImage;
    /// Tekstfelt til node-label (TextMeshPro).
    public TMP_Text labelText;

    // Internt gemt basefarve — bruges til at skifte mellem aktiv/inaktiv/selected tilstande.
    private Color baseColor = Color.white;

    /// <summary>
    /// Awake: minimal sikkerhedskode.
    /// Hvis Button ikke er sat i Inspector, forsøger vi at finde en Button i children.
    /// Gemmer initial baseImage farve hvis tilgængelig.
    /// </summary>
    private void Awake()
    {
        // Hvis brugeren glemte at assign knappen i prefab, prøv at finde den i children.
        if (button == null)
            button = GetComponentInChildren<Button>(true);

        if (baseImage != null)
            baseColor = baseImage.color;
    }

    /// <summary>
    /// ApplyStyle
    /// Anvender styling fra MapSettings for en given EncounterType.
    /// - Sætter sprites for base og ring fra settings.
    /// - Sætter ikon og farve fra NodeStyle hvis tilgængelig.
    /// </summary>
    /// <param name="settings">MapSettings instans med style data.</param>
    /// <param name="type">EncounterType der bestemmer hvilken style der anvendes.</param>
    public void ApplyStyle(MapSettings settings, EncounterType type)
    {
        if (settings == null) return;

        Sprite typeIcon = null;
        Color typeColor = Color.white;

        // Hent NodeStyle hvis den findes
        if (settings.TryGetStyle(type, out var style))
        {
            if (style.icon != null)
                typeIcon = style.icon;
            // Kun anvend farve hvis den har synlig alfa
            if (style.color.a > 0f)
                typeColor = style.color;
        }

        // Base image: sæt sprite fra settings og farv efter type
        if (baseImage != null)
        {
            if (settings.nodeBaseSprite != null)
                baseImage.sprite = settings.nodeBaseSprite;

            baseColor = typeColor;
            baseImage.color = baseColor;
            baseImage.enabled = true;
        }

        // Ring image: sæt sprite fra settings hvis tilgængelig, ellers hide
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

        // Icon image: sæt ikon hvis style indeholder et ikon, ellers hide
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

    /// <summary>
    /// SetInteractable
    /// Aktiverer eller deaktiverer knappen visuelt og funktionelt.
    /// Når ikke-interactable dæmpes basefarven for at give visuel feedback.
    /// </summary>
    /// <param name="interactable">True for aktiveret, false for deaktiveret.</param>
    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;

        if (baseImage == null) return;

        if (interactable)
        {
            // Gendan original farve
            baseImage.color = baseColor;
        }
        else
        {
            // Dæmp farven et par procent for "disabled" effekt
            var c = baseColor;
            c.r *= 0.6f;
            c.g *= 0.6f;
            c.b *= 0.6f;
            baseImage.color = c;
        }
    }

    /// <summary>
    /// SetSelected
    /// Marker node som valgt ved at justere basefarvens grønne komponent.
    /// Enkel, billig highlight-effekt.
    /// </summary>
    public void SetSelected()
    {
        if (baseImage == null) return;

        var c = baseColor;
        c.g = 1f; 
        baseImage.color = c;
    }

    /// <summary>
    /// SetLabel
    /// Sætter label-teksten for noden.
    /// </summary>
    /// <param name="text">Tekst der skal vises.</param>
    public void SetLabel(string text)
    {
        if (labelText == null) return;

        labelText.text = text;
    }

    /// <summary>
    /// SetLabelVisible
    /// Viser eller skjuler label-UI baseret på 'visible' og om der er tekst til stede.
    /// Bruges til at spare plads hvis labels er deaktiveret.
    /// </summary>
    /// <param name="visible">True for synlig, false for skjult.</param>
    public void SetLabelVisible(bool visible)
    {
        if (labelText == null) return;

        labelText.gameObject.SetActive(visible && !string.IsNullOrEmpty(labelText.text));
    }
}
