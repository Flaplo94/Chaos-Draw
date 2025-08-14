using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckSlotUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backImage;               // Face-down back image
    [SerializeField] private TextMeshProUGUI countText;     // Count overlay
    [SerializeField] private CanvasGroup cg;                // Optional group control

    [Header("Behavior")]
    [SerializeField] private bool fadeWhenEmpty = true;           // Turn OFF on Discard pile
    [SerializeField] private float emptyAlpha = 0.35f;
    [SerializeField] private bool hideBackWhenEmpty = true;       // Turn ON to hide back at 0
    [SerializeField] private bool disableInteractionWhenEmpty = true;

    public void Set(Sprite backSprite, int count)
    {
        bool empty = count <= 0;

        // Back sprite + visibility
        if (backImage != null)
        {
            backImage.sprite = backSprite;
            // Hide the BACK when empty (counter still shows)
            backImage.enabled = !(hideBackWhenEmpty && empty);
        }

        // Counter always visible and crisp (never faded)
        if (countText != null)
        {
            countText.text = Mathf.Max(0, count).ToString();
            // Ensure counter isn't faded by us
            var c = countText.color;
            if (c.a < 1f) { c.a = 1f; countText.color = c; }
        }

        // Optional: fade the WHOLE group (useful for draw pile). 
        // For Discard pile, turn fadeWhenEmpty OFF in the Inspector.
        if (cg != null)
        {
            cg.alpha = (fadeWhenEmpty && empty) ? emptyAlpha : 1f;

            if (disableInteractionWhenEmpty)
            {
                cg.blocksRaycasts = !empty;
                cg.interactable = !empty;
            }
        }
    }
}
