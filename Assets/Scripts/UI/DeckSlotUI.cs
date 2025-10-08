using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckSlotUI : MonoBehaviour
{
    public enum PlaceholderVisibility
    {
        OnlyWhenEmpty,  // vis kun placeholder når count == 0 (anbefalet)
        Never,          // vis aldrig placeholder
        Always          // vis altid placeholder (sjældent brugt)
    }

    [Header("References")]
    [SerializeField] private Image placeholderImage;            // DIT gamle BackImage (semi-transparent slot)
    [SerializeField] private TextMeshProUGUI countText;         // tæller overlay
    [SerializeField] private CanvasGroup cg;                    // valgfri fade/interaction

    [Header("Stack Layers (Card1..Card5)")]
    [Tooltip("Assign Card1..Card5 i rækkefølge. Viser kun de øverste N baseret på count.")]
    [SerializeField] private Image[] stackLayers;

    [Header("Behavior")]
    [SerializeField] private bool fadeWhenEmpty = true;
    [SerializeField] private float emptyAlpha = 0.35f;
    [SerializeField] private bool disableInteractionWhenEmpty = true;
    [SerializeField] private PlaceholderVisibility placeholderMode = PlaceholderVisibility.OnlyWhenEmpty;

    [Header("Back handling for layers")]
    [Tooltip("Når true, sættes deckets back-sprite på ALLE stackLayers (men ALDRIG på placeholderImage).")]
    [SerializeField] private bool propagateBackToLayers = true;

    private Sprite currentBackSprite; // rød/blå kortbagside til lagene

    /// <summary>
    /// Sæt deckets bagside (rød/blå) på LAGENE. Placeholder ændres ikke.
    /// Kald dette når deck-type skifter.
    /// </summary>
    public void SetBackSprite(Sprite backSprite)
    {
        currentBackSprite = backSprite;

        if (propagateBackToLayers && stackLayers != null)
        {
            for (int i = 0; i < stackLayers.Length; i++)
            {
                if (stackLayers[i] != null)
                    stackLayers[i].sprite = backSprite;
            }
        }
        // VIGTIGT: Ingen ændring af placeholderImage.sprite her!
    }

    /// <summary>
    /// Opdater visuel count + lag + placeholder. Kald når bunke-antal ændrer sig.
    /// </summary>
    public void SetCount(int count)
    {
        int clamped = Mathf.Max(0, count);
        bool empty = clamped == 0;

        // Counter
        if (countText != null)
            countText.text = clamped.ToString();

        // Fade/interaction
        if (cg != null)
        {
            cg.alpha = (fadeWhenEmpty && empty) ? emptyAlpha : 1f;
            if (disableInteractionWhenEmpty)
            {
                cg.blocksRaycasts = !empty;
                cg.interactable = !empty;
            }
        }

        // Lag: vis kun de øverste N
        if (stackLayers != null && stackLayers.Length > 0)
        {
            int show = Mathf.Clamp(clamped, 0, stackLayers.Length);
            for (int i = 0; i < stackLayers.Length; i++)
            {
                if (stackLayers[i] != null)
                    stackLayers[i].gameObject.SetActive(i < show);
            }
        }

        // Placeholder (det “tomme slot”)
        if (placeholderImage != null)
        {
            switch (placeholderMode)
            {
                case PlaceholderVisibility.OnlyWhenEmpty:
                    placeholderImage.enabled = empty; // viser KUN når bunken er tom
                    break;
                case PlaceholderVisibility.Never:
                    placeholderImage.enabled = false;
                    break;
                case PlaceholderVisibility.Always:
                    placeholderImage.enabled = true;
                    break;
            }
            // Tip: slå Raycast Target fra på placeholderImage i Inspector.
        }
    }

    /// <summary>
    /// Convenience hvis du tidligere kaldte Set(backSprite, count).
    /// </summary>
    public void Set(Sprite backSprite, int count)
    {
        if (backSprite != null && backSprite != currentBackSprite)
            SetBackSprite(backSprite);
        SetCount(count);
    }
}
